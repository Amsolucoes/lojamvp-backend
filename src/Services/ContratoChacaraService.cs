using LojaApi.Models;
using LojaApi.src.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LojaApi.src.Services;

public static class ContratoChacaraService
{
    public static byte[] GerarContratoPdf(Reserva reserva, Loja loja, InfoChacara? info, ConfiguracaoPrecoChacara cfg)
    {
        var endereco = info?.Endereco ?? "(endereço não cadastrado)";
        var dias = (int)Math.Round((reserva.DataFim - reserva.DataInicio).TotalDays) + 1;

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Contrato de Locação por Temporada").FontSize(18).Bold();
                    col.Item().PaddingTop(4).Text($"Reserva #{reserva.Id}").FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Text(text =>
                    {
                        text.Span("LOCADOR(A): ").Bold();
                        text.Span(loja.Nome);
                    });

                    col.Item().Text(text =>
                    {
                        text.Span("LOCATÁRIO(A): ").Bold();
                        text.Span(reserva.ClienteNome);
                    });
                    col.Item().Text(text =>
                    {
                        text.Span("E-mail: ").Bold();
                        text.Span(reserva.ClienteEmail);
                    });
                    col.Item().Text(text =>
                    {
                        text.Span("Telefone: ").Bold();
                        text.Span(reserva.ClienteTelefone);
                    });
                    if (!string.IsNullOrWhiteSpace(reserva.ClienteDocumento))
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("CPF: ").Bold();
                            text.Span(reserva.ClienteDocumento);
                        });
                    }

                    col.Item().PaddingTop(8).Text("OBJETO DO CONTRATO").Bold();
                    col.Item().Text($"Locação do imóvel de temporada situado em: {endereco}.");

                    col.Item().PaddingTop(8).Text("PERÍODO E VALOR").Bold();
                    col.Item().Text($"Check-in: {reserva.DataInicio:dd/MM/yyyy}");
                    col.Item().Text($"Check-out: {reserva.DataFim:dd/MM/yyyy}");
                    col.Item().Text($"Total de dias: {dias}");
                    col.Item().Text($"Número de pessoas: {reserva.Pessoas}");
                    col.Item().Text($"Valor total: R$ {reserva.Valor:N2}").Bold();

                    if (reserva.Pessoas > cfg.LimitePessoasPacotePequeno)
                    {
                        col.Item().Text($"Este valor já inclui a taxa de limpeza de R$ {cfg.ValorTaxaLimpeza:N2}, aplicável a eventos acima de {cfg.LimitePessoasPacotePequeno} pessoas.");
                    }

                    col.Item().PaddingTop(8).Text("CONDIÇÕES DE USO E SAÍDA").Bold();
                    col.Item().Text(
                        "O(A) locatário(a) compromete-se a deixar o imóvel em condições adequadas de limpeza e organização " +
                        "ao final do período contratado. Em caso de descumprimento, poderá ser cobrada multa de " +
                        $"R$ {cfg.ValorMultaNaoLimpeza:N2} referente aos custos de limpeza e reparo.");

                    col.Item().Text(
                        "O(A) locatário(a) é responsável por quaisquer danos causados ao imóvel e seus pertences durante o " +
                        "período de locação, comprometendo-se a ressarcir o(a) locador(a) pelos custos de reparo ou reposição.");

                    col.Item().PaddingTop(8).Text("DISPOSIÇÕES GERAIS").Bold();
                    col.Item().Text(
                        "Este contrato é gerado automaticamente com base nos dados informados no momento da reserva online " +
                        "e representa o acordo entre as partes para o período e valor acima especificados.");

                    col.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1);
                            c.Item().PaddingTop(4).Text(loja.Nome).FontSize(9);
                            c.Item().Text("Locador(a)").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1);
                            c.Item().PaddingTop(4).Text(reserva.ClienteNome).FontSize(9);
                            c.Item().Text("Locatário(a)").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Gerado automaticamente em ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.Span(DateTime.UtcNow.AddHours(-4).ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return documento.GeneratePdf();
    }
}