namespace LojaApi.Domain.Corretora;

public static class EtapaOportunidade
{
    public const string Lead = "lead";
    public const string Ganho = "ganho";
    public const string Perdido = "perdido";
}

public static class StatusApolice
{
    public const string Ativa = "ativa";
    public const string Cancelada = "cancelada";
}

// Uma linha do funil (para o dashboard): etapa, vidas e valor estimado da oportunidade
public readonly record struct LinhaDoFunil(string Etapa, int? Vidas, decimal? Valor);
public readonly record struct TotaisDoFunil(int Propostas, int Vidas, decimal ValorTotal);
public readonly record struct ResumoDoFunil(TotaisDoFunil EmAndamento, TotaisDoFunil Implantadas);

// Regras da corretora de seguros (funil de vendas e apólices) — sem banco.
public static class CorretoraRegras
{
    private static readonly string[] Etapas = ["lead", "contato", "proposta", "negociacao", "ganho", "perdido"];
    private static readonly string[] StatusDeApolice = ["ativa", "vencida", "renovada", "cancelada"];

    public static bool EtapaValida(string? etapa) => etapa is not null && Etapas.Contains(etapa);
    public static bool StatusApoliceValido(string? status) => status is not null && StatusDeApolice.Contains(status);

    // Oportunidade em andamento = ainda não foi ganha nem perdida
    public static bool EmAndamento(string etapa) => etapa != EtapaOportunidade.Ganho && etapa != EtapaOportunidade.Perdido;

    // As datas de vigência são guardadas ao meio-dia (UTC) para não "virarem de dia" por causa de fuso
    public static DateTime NormalizarVigencia(DateTime data) => DateTime.SpecifyKind(data.Date, DateTimeKind.Utc).AddHours(12);

    public static int DiasParaVencer(DateTime vigenciaFim, DateTime hoje) => (vigenciaFim.Date - hoje).Days;

    // Só apólice ativa que vence de hoje até daqui a 30 dias
    public static bool VenceEmAte30Dias(string status, DateTime vigenciaFim, DateTime hoje)
    {
        var dias = DiasParaVencer(vigenciaFim, hoje);
        return status == StatusApolice.Ativa && dias <= 30 && dias >= 0;
    }

    public static string DescricaoDaComissao(string plano, string? nomeCliente) => $"Comissão — {plano} ({nomeCliente})";

    // Comissão lançada ao ganhar: o valor informado, senão o estimado, senão zero
    public static decimal ValorDaComissao(decimal? informado, decimal? estimado) => informado ?? estimado ?? 0;

    public static ResumoDoFunil Resumir(IEnumerable<LinhaDoFunil> linhas)
    {
        var lista = linhas.ToList();
        var andamento = lista.Where(l => EmAndamento(l.Etapa)).ToList();
        var ganhas = lista.Where(l => l.Etapa == EtapaOportunidade.Ganho).ToList();
        return new(Totais(andamento), Totais(ganhas));
    }

    private static TotaisDoFunil Totais(List<LinhaDoFunil> l) => new(l.Count, l.Sum(x => x.Vidas ?? 0), l.Sum(x => x.Valor ?? 0));
}
