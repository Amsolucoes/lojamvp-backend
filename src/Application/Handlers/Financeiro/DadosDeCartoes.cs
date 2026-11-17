namespace LojaApi.Application.Financeiro;

// Dados de cartões já carregados (itens, faturas e antecipados) para as regras de Contas a Pagar rodarem em memória.
internal sealed class DadosDeCartoes
{
    public List<FaturaLeveDto> Faturas { get; init; } = [];
    public Dictionary<Guid, decimal> AntecipadoPorFatura { get; init; } = [];

    public FaturaLeveDto? Fatura(Guid cartaoId, int ano, int mes) =>
        Faturas.FirstOrDefault(f => f.CartaoCreditoId == cartaoId && f.MesReferencia.Year == ano && f.MesReferencia.Month == mes);

    public decimal Antecipado(FaturaLeveDto? fatura) =>
        fatura is not null && AntecipadoPorFatura.TryGetValue(fatura.Id, out var v) ? v : 0;

    // Carrega as faturas dos cartões no intervalo [desde, ate) e o total antecipado de cada uma
    public static async Task<DadosDeCartoes> CarregarAsync(IContasAPagarConsultas consultas, List<Guid> cartaoIds, DateTime desde, DateTime ate)
    {
        var faturas = await consultas.ListarFaturasAsync(cartaoIds, desde, ate);
        var antecipados = faturas.Count > 0 ? await consultas.SomarAntecipadosPorFaturaAsync(faturas.Select(f => f.Id).ToList()) : [];
        return new DadosDeCartoes { Faturas = faturas, AntecipadoPorFatura = antecipados };
    }
}
