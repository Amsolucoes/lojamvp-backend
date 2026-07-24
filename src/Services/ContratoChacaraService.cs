using LojaApi.Models;
using LojaApi.src.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace LojaApi.src.Services;

public static class ContratoChacaraService
{
    public static byte[] GerarContratoPdf(Reserva reserva, Loja loja, InfoChacara? info, ConfiguracaoPrecoChacara cfg)
    {
        var dias = (int)Math.Round((reserva.DataFim - reserva.DataInicio).TotalDays) + 1;
        var valorDiario = dias > 0 ? Math.Round(reserva.Valor / dias, 2) : reserva.Valor;
        var saldo = reserva.Valor - reserva.ValorPago;
        var pagoIntegral = saldo <= 0;

        var locadorNome = info?.LocadorNome ?? "(nome do locador não cadastrado)";
        var locadorRg = info?.LocadorRg ?? "(não informado)";
        var locadorCpf = info?.LocadorCpf ?? "(não informado)";
        var locadorEndereco = info?.LocadorEndereco ?? "(endereço não informado)";
        var locadorTelefone = info?.LocadorTelefone ?? "(não informado)";
        var cidadeAssinatura = info?.CidadeAssinatura ?? "(cidade não informada)";
        var enderecoImovel = info?.Endereco ?? "(endereço não cadastrado)";

        string formaPagamento;
        if (pagoIntegral)
        {
            formaPagamento = $"Pagamento integral de R$ {reserva.Valor:N2} recebido em " +
                $"{(reserva.DataConfirmacao ?? DateTime.UtcNow):dd/MM/yyyy}.";
        }
        else
        {
            formaPagamento = $"Entrada de R$ {reserva.ValorPago:N2} paga em " +
                $"{(reserva.DataConfirmacao ?? DateTime.UtcNow):dd/MM/yyyy} e o restante de R$ {saldo:N2} na entrega das " +
                "chaves ao Locatário, sendo que o valor dado como antecipação não será devolvido em caso de desistência " +
                "após a assinatura do contrato.";
        }

        var itensChacara = new List<string>();
        if (!string.IsNullOrWhiteSpace(info?.ComodidadesExtras))
        {
            itensChacara.AddRange(info!.ComodidadesExtras!
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0));
        }

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10.5f));

                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    col.Item().PaddingBottom(6).Text("CONTRATO DE LOCAÇÃO DE IMÓVEL PARA FINS DE TEMPORADA")
                        .FontSize(14).Bold().AlignCenter();

                    col.Item().Text(t =>
                    {
                        t.Span("LOCADOR: ").Bold();
                        t.Span($"{locadorNome}, RG: {locadorRg}, CPF: {locadorCpf}, reside à {locadorEndereco} – Telefone: {locadorTelefone}");
                    });

                    col.Item().Text(t =>
                    {
                        t.Span("LOCATÁRIO: ").Bold();
                        t.Span($"{reserva.ClienteNome}, CPF: {reserva.ClienteDocumento ?? "(não informado)"}, " +
                            $"CEP: {reserva.ClienteCep ?? "(não informado)"}, reside à {reserva.ClienteEndereco ?? "(não informado)"}. " +
                            $"Telefone: {(string.IsNullOrWhiteSpace(reserva.ClienteTelefone) ? "não informado" : reserva.ClienteTelefone)}");
                    });

                    col.Item().Text(t => { t.Span("IMÓVEL: ").Bold(); t.Span($"CHÁCARA {loja.Nome.ToUpper()}"); });
                    col.Item().Text(t => { t.Span("FINALIDADE: ").Bold(); t.Span("Temporada"); });
                    col.Item().Text(t => { t.Span("PRAZO DA LOCAÇÃO: ").Bold(); t.Span($"{dias} dia(s)"); });
                    col.Item().Text(t =>
                    {
                        t.Span("INÍCIO: ").Bold();
                        t.Span($"{reserva.DataInicio:dd/MM/yyyy} às 8h   ");
                        t.Span("TÉRMINO: ").Bold();
                        t.Span($"{reserva.DataFim:dd/MM/yyyy} às 20h");
                    });
                    col.Item().Text(t => { t.Span("VALOR DIÁRIO DA LOCAÇÃO: ").Bold(); t.Span($"R$ {valorDiario:N2}"); });
                    col.Item().Text(t => { t.Span("VALOR TOTAL DA LOCAÇÃO: ").Bold(); t.Span($"R$ {reserva.Valor:N2}"); });
                    col.Item().Text(t => { t.Span("FORMA DE PAGAMENTO: ").Bold(); t.Span(formaPagamento); });

                    col.Item().PaddingTop(8).Text(
                        "Locador e Locatário resolvem firmar o presente CONTRATO DE LOCAÇÃO POR TEMPORADA, mediante as " +
                        "cláusulas e condições seguintes:");

                    col.Item().PaddingTop(6).Text(t =>
                    {
                        t.Span("CLÁUSULA ÚNICA – Da devolução – ").Bold();
                        t.Span("Em caso de desistência, até 15 dias antes do dia reservado, será devolvido 50% da entrada do valor antecipado.");
                    });

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA PRIMEIRA – ").Bold();
                        t.Span($"O locador se obriga, neste ato, a dar em locação ao Locatário o imóvel de sua propriedade, " +
                            $"denominado Chácara {loja.Nome}, localizado em {enderecoImovel}.");
                    });
                    col.Item().PaddingLeft(14).Text(
                        "1.1. O Locatário nesta oportunidade se declara ciente das regras que regem o imóvel, comprometendo-se a observá-las e cumpri-las.");
                    col.Item().PaddingLeft(14).Text(
                        "1.2. Juntamente com o imóvel são dados em locação os bens móveis e utensílios que o guarnecem, conforme relação em anexo, sendo parte integrante deste contrato.");

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA SEGUNDA – ").Bold();
                        t.Span($"O prazo do presente contrato de locação é de {dias} dia(s), iniciando-se em " +
                            $"{reserva.DataInicio:dd/MM/yyyy}, às 8h, e encerrando-se em {reserva.DataFim:dd/MM/yyyy}, às 20h, " +
                            "quando o LOCATÁRIO se obriga a restituir o imóvel locado no estado de conservação em que o recebeu.");
                    });

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA TERCEIRA – ").Bold();
                        t.Span("A presente locação destina-se a fins exclusivamente de lazer, por tempo determinado, ficando " +
                            "proibida qualquer alteração da referida destinação, salvo mediante concordância prévia e expressa do LOCADOR.");
                    });

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA QUARTA – ").Bold();
                        t.Span("O LOCADOR, ou seu representante, expedirá recibo discriminado no qual constará o valor pago na presente data.");
                    });
                    col.Item().PaddingLeft(14).Text(
                        "Parágrafo Único – O LOCATÁRIO, no curso da locação, obriga-se a satisfazer todas as exigências do Poder Público, sob pena de rescisão deste contrato.");

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA QUINTA – ").Bold();
                        t.Span("O LOCATÁRIO deve manter o imóvel, as instalações sanitárias e elétricas, fechos, vidros, torneiras, " +
                            "ralos, pisos e calçadas, bem como os demais acessórios, móveis e utensílios, em perfeito estado de " +
                            "conservação e higiene, para restituí-los quando findo ou rescindido este contrato.");
                    });

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA SEXTA – ").Bold();
                        t.Span("Não será permitida a transferência deste contrato, nem a sublocação, cessão ou empréstimo total " +
                            "ou parcial do imóvel locado, sem prévia autorização por escrito do LOCADOR.");
                    });

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA SÉTIMA – ").Bold();
                        t.Span("O LOCATÁRIO faculta ao LOCADOR, ou seu representante, o exame e vistoria do imóvel locado, " +
                            "em dia e hora previamente acordados, a fim de verificar o estado de conservação.");
                    });

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA OITAVA – ").Bold();
                        t.Span("O LOCATÁRIO se responsabiliza por qualquer dano que venha a causar ao imóvel ou aos bens que " +
                            "o guarnecem, devendo restituí-los nas mesmas condições em que os recebeu.");
                    });
                    col.Item().PaddingLeft(14).Text(
                        "9.1. Havendo qualquer dano, o LOCATÁRIO deverá repará-lo por sua conta enquanto durar a locação.");

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA NONA – ").Bold();
                        t.Span("Fica estipulada multa no valor de 20% (vinte por cento) incidente sobre o valor total do " +
                            "contrato, por qualquer infração às cláusulas estabelecidas neste instrumento.");
                    });

                    col.Item().Text(t =>
                    {
                        t.Span("CLÁUSULA DÉCIMA – ").Bold();
                        t.Span($"As partes elegem o foro da comarca de {cidadeAssinatura}, que é o da situação do imóvel, " +
                            "para dirimir as questões resultantes da execução do presente contrato, obrigando-se a parte " +
                            "vencida a pagar à vencedora, além das custas e despesas processuais, honorários advocatícios " +
                            "fixados em 20% (vinte por cento) sobre o valor da causa.");
                    });

                    col.Item().PaddingTop(6).Text(
                        "E assim, por estarem justas e contratadas, assinam as partes o presente instrumento particular de " +
                        "CONTRATO DE LOCAÇÃO POR TEMPORADA, em 2 (duas) vias de igual teor e forma, na presença da testemunha abaixo.");

                    if (itensChacara.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("A Chácara contém:").Bold();
                        foreach (var item in itensChacara)
                            col.Item().PaddingLeft(10).Text($"– {item}");
                    }

                    col.Item().PaddingTop(16).Text($"{cidadeAssinatura}, {DateTime.UtcNow.AddHours(-4):dd/MM/yyyy}.");

                    col.Item().PaddingTop(20).Text($"LOCADOR: {locadorNome}: ______________________________________");
                    col.Item().PaddingTop(14).Text(
                        $"LOCATÁRIO: {reserva.ClienteNome}, CPF: {reserva.ClienteDocumento ?? "(não informado)"}, " +
                        $"CEP: {reserva.ClienteCep ?? "(não informado)"}, residente à {reserva.ClienteEndereco ?? "(não informado)"}. " +
                        $"Telefone: {(string.IsNullOrWhiteSpace(reserva.ClienteTelefone) ? "não informado" : reserva.ClienteTelefone)}: ______________________________________");
                    col.Item().PaddingTop(14).Text("TESTEMUNHA: ______________________________________");
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Gerado automaticamente em ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(DateTime.UtcNow.AddHours(-4).ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return documento.GeneratePdf();
    }
}