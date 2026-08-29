using LojaApi.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Resend;

namespace LojaApi.src.Services;

public class OrdemServicoNotificacaoService(AppDbContext db, IResend resend, IHttpClientFactory httpClientFactory, ILogger<OrdemServicoNotificacaoService> logger)
{
    public record ResultadoEnvio(bool Enviado, string? Erro);

    public async Task<ResultadoEnvio> EnviarPorEmailAsync(Guid orcamentoId, Guid lojaId)
    {
        var orcamento = await db.OrcamentosServico
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == orcamentoId && o.LojaId == lojaId);
        if (orcamento is null) return new ResultadoEnvio(false, "Orçamento não encontrado.");

        var cliente = await db.Clientes.FindAsync(orcamento.ClienteId);
        if (cliente is null || string.IsNullOrWhiteSpace(cliente.Email))
            return new ResultadoEnvio(false, "Cliente não tem e-mail cadastrado.");

        var loja = await db.Lojas.FindAsync(lojaId);
        if (loja is null) return new ResultadoEnvio(false, "Loja não encontrada.");

        var linhas = string.Join("", orcamento.Itens.Select(i =>
            $"<tr><td style='padding:6px 10px;border-bottom:1px solid #eee'>{i.Descricao} ({i.Quantidade}x)</td>" +
            $"<td style='padding:6px 10px;border-bottom:1px solid #eee;text-align:right'>R$ {i.ValorTotal:N2}</td></tr>"));

        var veiculo = string.Join(" · ", new[] { orcamento.VeiculoDescricao, orcamento.Placa }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var logoHtml = string.IsNullOrWhiteSpace(loja.LogoUrl)
            ? $"<h2 style='color:#2f7d4f'>{loja.Nome}</h2>"
            : $"<img src='{loja.LogoUrl}' alt='{loja.Nome}' style='max-height:60px;max-width:220px;margin-bottom:8px' />";

        var contatoLinhas = string.Join("", new[]
        {
            !string.IsNullOrWhiteSpace(loja.Endereco) ? $"<div>{loja.Endereco}</div>" : "",
            !string.IsNullOrWhiteSpace(loja.Telefone) ? $"<div>Tel/WhatsApp: {FormatarTelefone(loja.Telefone)}</div>" : "",
        });

        var html = $@"
            <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                <div style='text-align:center;margin-bottom:12px'>{logoHtml}</div>
                <h3 style='color:#2f7d4f;margin-bottom:4px'>Orçamento</h3>
                <p>Olá, {cliente.Nome}! Segue o orçamento{(string.IsNullOrEmpty(veiculo) ? "" : $" referente ao veículo <strong>{veiculo}</strong>")}.</p>
                <table style='width:100%;border-collapse:collapse;margin-top:12px'>{linhas}</table>
                <table style='width:100%;border-collapse:collapse;margin-top:8px'>
                    <tr><td style='padding:6px 10px;font-weight:600'>Total</td><td style='padding:6px 10px;text-align:right;font-weight:600'>R$ {orcamento.ValorTotal:N2}</td></tr>
                </table>
                {(string.IsNullOrWhiteSpace(orcamento.Observacoes) ? "" : $"<p style='margin-top:16px;font-size:13px;color:#666'><strong>Observações:</strong> {orcamento.Observacoes}</p>")}
                <div style='margin-top:20px;padding-top:12px;border-top:1px solid #eee;font-size:13px;color:#888;text-align:center'>
                    <div style='font-weight:600;color:#555'>{loja.Nome}</div>
                    {contatoLinhas}
                </div>
            </div>";

        try
        {
            var msg = new EmailMessage
            {
                From = "AldevSoftware <ordemservico@aldevsoftware.com.br>",
                Subject = $"Orçamento — {loja.Nome}" + (string.IsNullOrEmpty(veiculo) ? "" : $" ({veiculo})"),
                HtmlBody = html,
            };
            msg.To.Add(cliente.Email);
            await resend.EmailSendAsync(msg);
            logger.LogInformation("Orçamento {OrcamentoId} enviado por e-mail para {Email}.", orcamento.Id, cliente.Email);
            return new ResultadoEnvio(true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar orçamento {OrcamentoId} por e-mail.", orcamento.Id);
            return new ResultadoEnvio(false, "Falha ao enviar o e-mail. Tente novamente.");
        }
    }

    // ── PDF do orçamento, formato A4 — usado pelo botão "Baixar PDF" ──
    // Obs: não embute logo (evita depender de download de imagem externa dentro
    // da geração do PDF); usa o nome da loja como cabeçalho, igual o e-mail faz
    // quando não há logo cadastrada.
    public async Task<byte[]?> GerarPdfAsync(Guid orcamentoId, Guid lojaId)
    {
        var orcamento = await db.OrcamentosServico
            .Include(o => o.Itens)
            .Include(o => o.Mecanicos).ThenInclude(m => m.Profissional)
            .Include(o => o.ChecklistRespostas).ThenInclude(r => r.ChecklistItem)
            .FirstOrDefaultAsync(o => o.Id == orcamentoId && o.LojaId == lojaId);
        if (orcamento is null) return null;

        var cliente = await db.Clientes.FindAsync(orcamento.ClienteId);
        var loja = await db.Lojas.FindAsync(lojaId);
        if (loja is null) return null;

        // Baixa a logo da loja pra embutir no cabeçalho. Se falhar (rede, URL
        // inválida etc.), segue sem logo em vez de quebrar a geração do PDF.
        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(loja.LogoUrl))
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                logoBytes = await client.GetByteArrayAsync(loja.LogoUrl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Não foi possível baixar a logo da loja {LojaId} para o PDF.", lojaId);
            }
        }

        var veiculo = string.Join(" · ", new[] { orcamento.VeiculoDescricao, orcamento.Placa }.Where(s => !string.IsNullOrWhiteSpace(s)));

        // Enquanto está "pendente" ainda é um orçamento; a partir do momento que
        // é aprovado (em_andamento/concluído/cancelado) já virou ordem de serviço —
        // é o mesmo critério que você descreveu no fluxo do sistema.
        var ehOrcamento = orcamento.Status == "pendente";

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Background(Colors.Grey.Darken4).Padding(16).Row(row =>
                {
                    if (logoBytes != null)
                    {
                        row.ConstantItem(70).Height(50).Image(logoBytes).FitArea();
                        row.ConstantItem(12);
                    }
                    row.RelativeItem().AlignMiddle().Text(loja.Nome).FontColor(Colors.White).FontSize(20).Bold();
                    row.AutoItem().Column(col =>
                    {
                        if (!string.IsNullOrWhiteSpace(loja.Telefone))
                            col.Item().AlignRight().Text($"Tel/WhatsApp: {FormatarTelefone(loja.Telefone!)}").FontColor(Colors.White).FontSize(9);
                        if (!string.IsNullOrWhiteSpace(loja.Endereco))
                            col.Item().AlignRight().Text(loja.Endereco!).FontColor(Colors.White).FontSize(9);
                    });
                });

                page.Content().Padding(20).Column(col =>
                {
                    col.Item().PaddingBottom(12).Row(row =>
                    {
                        row.AutoItem().Width(12).Height(12).Border(1).BorderColor(Colors.Black)
                            .Background(ehOrcamento ? Colors.Black : Colors.White);
                        row.AutoItem().PaddingLeft(4).PaddingRight(16).AlignMiddle().Text("Orçamento").FontSize(10);
                        row.AutoItem().Width(12).Height(12).Border(1).BorderColor(Colors.Black)
                            .Background(!ehOrcamento ? Colors.Black : Colors.White);
                        row.AutoItem().PaddingLeft(4).AlignMiddle().Text("Ordem de Serviço").FontSize(10).Bold();
                        row.RelativeItem();
                        row.AutoItem().AlignMiddle().Text($"Data: {orcamento.CriadoEm:dd/MM/yyyy}").FontSize(10);
                    });

                    col.Item().PaddingBottom(4).Text($"Nome: {cliente?.Nome ?? "—"}").FontSize(11);
                    if (cliente != null && !string.IsNullOrWhiteSpace(cliente.Telefone))
                        col.Item().PaddingBottom(4).Text($"Telefone: {cliente.Telefone}").FontSize(11);
                    if (!string.IsNullOrEmpty(veiculo))
                        col.Item().PaddingBottom(4).Text($"Veículo: {veiculo}").FontSize(11);

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(5);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Orange.Medium).Padding(6).Text("Quant.").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Orange.Medium).Padding(6).Text("Descrição de peças/serviços").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Orange.Medium).Padding(6).AlignRight().Text("Total").FontColor(Colors.White).Bold();
                        });

                        foreach (var item in orcamento.Itens)
                        {
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(item.Quantidade.ToString());
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(item.Descricao);
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight().Text($"R$ {item.ValorTotal:N2}");
                        }

                        // Linhas em branco no fim, pra lembrar o modelo em papel —
                        // espaço pra anotação manual se precisar.
                        for (int i = 0; i < 3; i++)
                        {
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(" ");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(" ");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(" ");
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Border(1).BorderColor(Colors.Black)
                        .Padding(8).Text($"TOTAL R$ {orcamento.ValorTotal:N2}").FontSize(14).Bold();

                    if (orcamento.Mecanicos.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Mecânico(s)").Bold().FontSize(11);
                        foreach (var m in orcamento.Mecanicos)
                            col.Item().Text(m.Profissional?.Nome ?? "—").FontSize(10);
                    }

                    if (orcamento.ChecklistRespostas.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Checklist").Bold().FontSize(11);
                        foreach (var r in orcamento.ChecklistRespostas)
                            col.Item().Text($"{r.ChecklistItem?.Nome}: {r.Estado}").FontSize(10);
                    }

                    if (!string.IsNullOrWhiteSpace(orcamento.Observacoes))
                    {
                        col.Item().PaddingTop(16).Text("Observações").Bold().FontSize(11);
                        col.Item().Text(orcamento.Observacoes).FontSize(10);
                    }

                    col.Item().PaddingTop(40).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(4).LineHorizontal(1).LineColor(Colors.Black);
                            c.Item().AlignCenter().Text("Técnico").FontSize(10);
                        });
                        row.ConstantItem(30);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(4).LineHorizontal(1).LineColor(Colors.Black);
                            c.Item().AlignCenter().Text("Cliente").FontSize(10);
                        });
                    });
                });

                page.Footer().AlignCenter().PaddingVertical(6).Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }

    // Formata telefone só para exibição — não altera o valor salvo no banco.
    // Aceita 10 dígitos (fixo, com DDD) ou 11 dígitos (celular, com DDD).
    private static string FormatarTelefone(string telefone)
    {
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        return digitos.Length switch
        {
            11 => $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}",
            10 => $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}",
            _ => telefone, // formato inesperado — mostra como veio, sem quebrar
        };
    }
}