using System.Globalization;
using System.Text.Json;

namespace LojaApi.Domain.Carteira;

// Regras da carteira do cliente. Métodos devolvem a mensagem de erro, ou null quando está tudo certo.
public static class RegrasCarteira
{
    public const string FormaCarteira = "carteira";

    // Como o cliente pode pagar o que deve (a própria carteira, claro, não está na lista)
    public static readonly string[] FormasDeRecebimento = ["dinheiro", "pix", "credito", "debito"];

    private static readonly CultureInfo PtBr = new("pt-BR");
    public static string Dinheiro(decimal valor) => string.Format(PtBr, "R$ {0:N2}", valor);

    // Uma venda só pode ir para a carteira se o cliente não estourar o limite (sem limite = sem teto).
    public static string? ValidarCompra(decimal saldoAtual, decimal valor, decimal? limite)
    {
        if (valor <= 0) return "Informe um valor maior que zero.";
        if (limite.HasValue && saldoAtual + valor > limite.Value)
        {
            var disponivel = Math.Max(0, limite.Value - saldoAtual);
            return $"Limite da carteira excedido. Limite: {Dinheiro(limite.Value)}, já deve: {Dinheiro(saldoAtual)}, disponível: {Dinheiro(disponivel)}.";
        }
        return null;
    }

    public static string? ValidarLimite(decimal? limite) =>
        limite is < 0 ? "O limite não pode ser negativo." : null;

    // O cliente só paga o que deve, por uma forma conhecida, numa data que não seja futura.
    public static string? ValidarRecebimento(decimal saldo, decimal valor, string? forma, DateTime data, DateTime hoje)
    {
        if (valor <= 0) return "Informe um valor maior que zero.";
        if (!FormasDeRecebimento.Contains(forma ?? "")) return "Forma de pagamento inválida.";
        if (data.Date > hoje.Date) return "A data não pode ser no futuro.";
        if (valor > saldo) return $"O cliente deve apenas {Dinheiro(saldo)} na carteira.";
        return null;
    }

    // Quanto da venda foi para a carteira. O caixa manda as formas em JSON: [{ "forma": "pix", "valor": 50 }, { "forma": "carteira", "valor": 70 }].
    // Sem JSON legível, vale a forma principal: "carteira" = a venda inteira.
    public static decimal ValorNaCarteira(string? formasJson, string formaPrincipal, decimal valorDevido)
    {
        if (!string.IsNullOrWhiteSpace(formasJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(formasJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    decimal soma = 0;
                    foreach (var f in doc.RootElement.EnumerateArray())
                    {
                        if (f.ValueKind != JsonValueKind.Object) continue;
                        if (!f.TryGetProperty("forma", out var forma) || forma.GetString() != FormaCarteira) continue;
                        if (f.TryGetProperty("valor", out var valor) && valor.TryGetDecimal(out var v)) soma += v;
                    }
                    return Math.Round(soma, 2);
                }
            }
            catch (JsonException) { /* cai para a forma principal */ }
        }
        return formaPrincipal == FormaCarteira ? valorDevido : 0;
    }

    // Distribui os pagamentos nas compras mais antigas primeiro (o que sobra de cada compra é o que está "em aberto").
    public static List<(Guid VendaId, DateTime Data, decimal Valor, decimal EmAberto)> ComprasEmAberto(
        IEnumerable<(Guid VendaId, DateTime Data, decimal Valor)> compras, decimal totalPago)
    {
        var restantePago = totalPago;
        var resultado = new List<(Guid, DateTime, decimal, decimal)>();
        foreach (var c in compras.OrderBy(c => c.Data))
        {
            var abatido = Math.Min(c.Valor, Math.Max(0, restantePago));
            restantePago -= abatido;
            var emAberto = c.Valor - abatido;
            if (emAberto > 0) resultado.Add((c.VendaId, c.Data, c.Valor, emAberto));
        }
        return resultado;
    }

    // ── Quem pagou o quê ─────────────────────────────────────────────────────
    // Cada pagamento quita as compras mais antigas primeiro (como um extrato). Daqui sai, para cada venda na carteira,
    // quanto ainda está em aberto e por qual forma/dia foi sendo paga — o fluxo de caixa mostra isso como "Carteira → Pix".

    public record CompraParaAlocar(Guid VendaId, DateTime Data, DateTime CriadoEm, decimal Valor);
    public record PagamentoParaAlocar(Guid Id, DateTime Data, DateTime CriadoEm, decimal Valor, string Forma);
    public record PartePaga(Guid PagamentoId, DateTime Data, string Forma, decimal Valor);
    public record CompraAlocada(Guid VendaId, decimal Valor, decimal EmAberto, List<PartePaga> Pagamentos);
    public record PagamentoAlocado(Guid PagamentoId, List<(Guid VendaId, decimal Valor)> Abateu);

    public static (List<CompraAlocada> Compras, List<PagamentoAlocado> Pagamentos) AlocarPagamentos(
        IEnumerable<CompraParaAlocar> compras, IEnumerable<PagamentoParaAlocar> pagamentos)
    {
        var fila = compras
            .OrderBy(c => c.Data.Date).ThenBy(c => c.CriadoEm)
            .Select(c => (Compra: c, Restante: c.Valor, Partes: new List<PartePaga>()))
            .ToList();
        var pagamentosAlocados = new List<PagamentoAlocado>();

        foreach (var pg in pagamentos.OrderBy(p => p.Data.Date).ThenBy(p => p.CriadoEm))
        {
            var sobra = pg.Valor;
            var abateu = new List<(Guid, decimal)>();
            for (var i = 0; i < fila.Count && sobra > 0; i++)
            {
                if (fila[i].Restante <= 0) continue;
                var parte = Math.Min(sobra, fila[i].Restante);
                fila[i] = (fila[i].Compra, fila[i].Restante - parte, fila[i].Partes);
                fila[i].Partes.Add(new PartePaga(pg.Id, pg.Data, pg.Forma, parte));
                abateu.Add((fila[i].Compra.VendaId, parte));
                sobra -= parte;
            }
            pagamentosAlocados.Add(new PagamentoAlocado(pg.Id, abateu));
        }

        var comprasAlocadas = fila.Select(f => new CompraAlocada(f.Compra.VendaId, f.Compra.Valor, f.Restante, f.Partes)).ToList();
        return (comprasAlocadas, pagamentosAlocados);
    }
}
