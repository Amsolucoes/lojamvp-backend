using LojaApi.Application.OrdemServico;
using LojaApi.Domain.OrdemServico;
using LojaApi.src.Models.OrdemServico;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LojaApi.Infrastructure.OrdemServico;

// Adaptador da porta IRelatorioPatrimonioPdf: desenha os PDFs do patrimônio (QuestPDF).
public class RelatorioPatrimonioPdf : IRelatorioPatrimonioPdf
{
    // PDF do cadastro atual (lista de itens + valor total de patrimônio)
    public byte[] GerarCadastro(string? nomeLoja, IReadOnlyList<ItemPatrimonio> itens)
    {
        var valorTotal = itens.Sum(i => i.QuantidadeEsperada * i.ValorUnitario);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Text($"Patrimônio — {nomeLoja ?? ""}").FontSize(16).Bold();

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); // nome
                        c.RelativeColumn(2); // categoria
                        c.RelativeColumn(1); // qtd
                        c.RelativeColumn(1.5f); // valor unit
                        c.RelativeColumn(1.5f); // total
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Item").Bold();
                        h.Cell().Text("Categoria").Bold();
                        h.Cell().Text("Qtd").Bold();
                        h.Cell().Text("Valor unit.").Bold();
                        h.Cell().Text("Total").Bold();
                    });

                    foreach (var i in itens)
                    {
                        table.Cell().Text(i.Nome);
                        table.Cell().Text(i.Categoria ?? "—");
                        table.Cell().Text(i.QuantidadeEsperada.ToString());
                        table.Cell().Text($"R$ {i.ValorUnitario:N2}");
                        table.Cell().Text($"R$ {(i.QuantidadeEsperada * i.ValorUnitario):N2}");
                    }
                });

                page.Footer().AlignRight().Text($"Valor total do patrimônio: R$ {valorTotal:N2}").Bold();
            });
        }).GeneratePdf();
    }

    // PDF de uma contagem específica, com comparativo esperado x contado
    public byte[] GerarContagem(string? nomeLoja, ContagemPatrimonio contagem)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text($"Contagem de Patrimônio — {nomeLoja ?? ""}").FontSize(16).Bold();
                    col.Item().Text($"Data: {contagem.DataContagem:dd/MM/yyyy HH:mm}" +
                        (string.IsNullOrEmpty(contagem.Responsavel) ? "" : $" · Responsável: {contagem.Responsavel}"))
                        .FontSize(10);
                });

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); // nome
                        c.RelativeColumn(1.2f); // esperado
                        c.RelativeColumn(1.2f); // contado
                        c.RelativeColumn(1.2f); // diferença
                        c.RelativeColumn(2.5f); // observação
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Item").Bold();
                        h.Cell().Text("Esperado").Bold();
                        h.Cell().Text("Contado").Bold();
                        h.Cell().Text("Diferença").Bold();
                        h.Cell().Text("Observação").Bold();
                    });

                    foreach (var i in contagem.Itens.OrderBy(i => i.ItemPatrimonio!.Nome))
                    {
                        var diferenca = OrdemServicoRegras.DiferencaDaContagem(i.QuantidadeContada, i.QuantidadeEsperadaNoMomento);
                        table.Cell().Text(i.ItemPatrimonio!.Nome);
                        table.Cell().Text(i.QuantidadeEsperadaNoMomento.ToString());
                        table.Cell().Text(i.QuantidadeContada.ToString());
                        table.Cell().Text(diferenca == 0 ? "OK" : (diferenca > 0 ? $"+{diferenca}" : diferenca.ToString()))
                            .FontColor(diferenca == 0 ? Colors.Black : Colors.Red.Medium);
                        table.Cell().Text(i.Observacao ?? "—");
                    }
                });

                var totalDivergencias = contagem.Itens.Count(i => i.QuantidadeContada != i.QuantidadeEsperadaNoMomento);
                page.Footer().AlignRight().Text($"{totalDivergencias} item(ns) com divergência de {contagem.Itens.Count} contado(s).").Bold();
            });
        }).GeneratePdf();
    }
}
