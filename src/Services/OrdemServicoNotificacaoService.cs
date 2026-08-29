using LojaApi.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Resend;

namespace LojaApi.src.Services;

public class OrdemServicoNotificacaoService(AppDbContext db, IResend resend, ILogger<OrdemServicoNotificacaoService> logger)
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
            .FirstOrDefaultAsync(o => o.Id == orcamentoId && o.LojaId == lojaId);
        if (orcamento is null) return null;

        var cliente = await db.Clientes.FindAsync(orcamento.ClienteId);
        var loja = await db.Lojas.FindAsync(lojaId);
        if (loja is null) return null;

        var veiculo = string.Join(" · ", new[] { orcamento.VeiculoDescricao, orcamento.Placa }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(loja.Nome).FontSize(18).Bold();
                    if (!string.IsNullOrWhiteSpace(loja.Endereco))
                        col.Item().Text(loja.Endereco!).FontSize(9).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(loja.Telefone))
                        col.Item().Text($"Tel/WhatsApp: {FormatarTelefone(loja.Telefone!)}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(14).Column(col =>
                {
                    col.Item().Text("Orçamento").FontSize(16).Bold();
                    col.Item().PaddingTop(4).Text($"Cliente: {cliente?.Nome ?? "—"}");
                    if (cliente != null && !string.IsNullOrWhiteSpace(cliente.Telefone))
                        col.Item().Text($"Telefone: {cliente.Telefone}");
                    if (!string.IsNullOrEmpty(veiculo))
                        col.Item().Text($"Veículo: {veiculo}");
                    col.Item().Text($"Data: {orcamento.CriadoEm:dd/MM/yyyy}");

                    col.Item().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Descrição").Bold();
                            header.Cell().Text("Qtd").Bold();
                            header.Cell().Text("Valor unit.").Bold();
                            header.Cell().Text("Total").Bold();
                            header.Cell().ColumnSpan(4).PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        });

                        foreach (var item in orcamento.Itens)
                        {
                            table.Cell().PaddingVertical(4).Text(item.Descricao);
                            table.Cell().PaddingVertical(4).Text(item.Quantidade.ToString());
                            table.Cell().PaddingVertical(4).Text($"R$ {item.ValorUnitario:N2}");
                            table.Cell().PaddingVertical(4).Text($"R$ {item.ValorTotal:N2}");
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Text($"Total: R$ {orcamento.ValorTotal:N2}").FontSize(14).Bold();

                    if (!string.IsNullOrWhiteSpace(orcamento.Observacoes))
                    {
                        col.Item().PaddingTop(16).Text("Observações").Bold();
                        col.Item().Text(orcamento.Observacoes);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
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