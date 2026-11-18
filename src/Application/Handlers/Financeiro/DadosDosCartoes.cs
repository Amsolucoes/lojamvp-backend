using LojaApi.Domain.Financeiro;

namespace LojaApi.Application.Financeiro;

// Dados dos cartões já carregados (compras, faturas e adiantamentos) para os relatórios rodarem as regras em memória.
// Antes, cada relatório consultava o banco dentro de laços (cartão × mês); agora tudo vem em poucas consultas.
internal sealed class DadosDosCartoes
{
    // Fim em aberto das janelas de leitura (compras futuras e faturas futuras entram, sem limite superior)
    public static readonly DateTime SemLimite = new(9000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly Dictionary<Guid, List<CompraLeveDto>> _comprasPorCartao = [];
    private readonly List<FaturaDoRelatorioDto> _faturas = [];
    private readonly Dictionary<Guid, decimal> _antecipadoPorFatura = [];

    public IReadOnlyList<CompraLeveDto> Compras(Guid cartaoId) => _comprasPorCartao.TryGetValue(cartaoId, out var lista) ? lista : [];

    // Total das compras do cartão com data entre início e fim (inclusive)
    public decimal SomaCompras(Guid cartaoId, DateTime inicio, DateTime fim) =>
        Compras(cartaoId).Where(c => c.DataCompra.Date >= inicio.Date && c.DataCompra.Date <= fim.Date).Sum(c => c.Valor);

    public FaturaDoRelatorioDto? Fatura(Guid cartaoId, int ano, int mes) =>
        _faturas.FirstOrDefault(f => f.CartaoCreditoId == cartaoId && f.MesReferencia.Year == ano && f.MesReferencia.Month == mes);

    public decimal Antecipado(FaturaDoRelatorioDto? fatura) => fatura is not null && _antecipadoPorFatura.TryGetValue(fatura.Id, out var v) ? v : 0;

    public RelatoriosRegras.FaturaRegistrada? FaturaRegistrada(Guid cartaoId, int ano, int mes) =>
        Fatura(cartaoId, ano, mes) is { } f ? new RelatoriosRegras.FaturaRegistrada(f.Status, f.Total, Antecipado(f)) : null;

    // Contribuição do cartão no mês (ver RelatoriosRegras.ContribuicaoDoCartaoNoMes)
    public RelatoriosRegras.Contribuicao ContribuicaoNoMes(CartaoDoRelatorioDto cartao, int ano, int mes, DateTime hoje, IEnumerable<RelatoriosRegras.ParcelaDoMes> parcelasDoMes) =>
        RelatoriosRegras.ContribuicaoDoCartaoNoMes(cartao.DiaFechamento, cartao.DiaVencimento, ano, mes, hoje,
            (inicio, fim) => SomaCompras(cartao.Id, inicio, fim), FaturaRegistrada(cartao.Id, ano, mes), parcelasDoMes);

    // Compras de comprasDesde até comprasAte (exclusive) e faturas de faturasDesde até faturasAte (exclusive, pelo mês de referência)
    public static async Task<DadosDosCartoes> CarregarAsync(IRelatoriosConsultas consultas, IContasAPagarConsultas contasAPagar, List<Guid> cartaoIds,
        DateTime comprasDesde, DateTime comprasAte, DateTime faturasDesde, DateTime faturasAte)
    {
        var dados = new DadosDosCartoes();
        if (cartaoIds.Count == 0) return dados;

        foreach (var grupo in (await consultas.ListarComprasLevesAsync(cartaoIds, comprasDesde, comprasAte)).GroupBy(c => c.CartaoId))
            dados._comprasPorCartao[grupo.Key] = grupo.ToList();

        dados._faturas.AddRange(await consultas.ListarFaturasAsync(cartaoIds, faturasDesde, faturasAte));
        if (dados._faturas.Count > 0)
            foreach (var (faturaId, total) in await contasAPagar.SomarAntecipadosPorFaturaAsync(dados._faturas.Select(f => f.Id).ToList()))
                dados._antecipadoPorFatura[faturaId] = total;

        return dados;
    }
}
