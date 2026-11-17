namespace LojaApi.Domain.Financeiro;

// Regras puras do Planejamento (sem banco): percentual a guardar, receita planejada, tipo do teto por categoria e valores das metas.
public static class PlanejamentoRegras
{
    public const string IconePadraoDaMeta = "🎯";

    public static bool PercentualValido(decimal percentual) => percentual is >= 0 and <= 100;

    // Receita planejada informada e negativa é recusada; ausente (nula) vale
    public static bool ReceitaPlanejadaNegativa(decimal? receita) => receita < 0;

    // Só receita maior que zero é gravada; zero ou ausente volta a "sem receita planejada" (o sistema usa a prevista)
    public static decimal? ReceitaParaGravar(decimal? receita) => receita is > 0 ? receita : null;

    // Teto de "receber" ou de "pagar" (qualquer outro texto vale "pagar")
    public static string TipoDoTeto(string? tipo) => tipo == "receber" ? "receber" : "pagar";

    public static bool ValorDoTetoValido(decimal valor) => valor >= 0;

    public static bool ValorAlvoValido(decimal valorAlvo) => valorAlvo > 0;

    public static string IconeDaMeta(string? icone) => string.IsNullOrWhiteSpace(icone) ? IconePadraoDaMeta : icone.Trim();

    // O que já foi guardado nunca fica negativo
    public static decimal ValorGuardadoValido(decimal valor) => Math.Max(0, valor);

    // Soma (ou, com valor negativo, retira) o aporte, sem deixar o guardado negativo
    public static decimal NovoValorGuardado(decimal atual, decimal aporte) => Math.Max(0, atual + aporte);

    // Data alvo da meta: só o dia, em UTC
    public static DateTime? DataAlvoEmUtc(DateTime? data) => data.HasValue ? DateTime.SpecifyKind(data.Value.Date, DateTimeKind.Utc) : null;
}
