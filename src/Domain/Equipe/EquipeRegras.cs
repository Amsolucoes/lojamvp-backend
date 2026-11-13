namespace LojaApi.Domain.Equipe;

public static class TipoRemuneracao
{
    public const string Comissao = "comissao";
    public const string SalarioFixo = "salario_fixo";
    public const string Diaria = "diaria";
}

public static class StatusComissao
{
    public const string Pendente = "pendente";
    public const string Pago = "pago";
}

public static class OrigemComissao
{
    public const string Agendamento = "agendamento";
    public const string OrdemServico = "ordem_servico";
    public const string Venda = "venda";
}

// Regras da equipe (funcionários, remuneração e fechamento de comissões) — sem banco.
public static class EquipeRegras
{
    public const string CategoriaSalarios = "Salários de Funcionários";
    public const string IconeSalarios = "💰";
    public const string CategoriaComissoes = "Comissões de Funcionários";
    public const string IconeComissoes = "👤";

    // Dia do mês em que o salário vence quando ninguém informou um dia válido
    public const int DiaPadraoDoSalario = 5;

    // Mensagem de erro dos valores exigidos por cada tipo de remuneração (null = tudo certo)
    public static string? ValidarRemuneracao(string tipo, decimal? salarioFixo, decimal? valorDiaria, bool comissaoVendasAtiva, decimal? comissaoVendasPercentual)
    {
        if (tipo == TipoRemuneracao.SalarioFixo && (!salarioFixo.HasValue || salarioFixo <= 0))
            return "Informe o valor do salário fixo.";
        if (tipo == TipoRemuneracao.Diaria && (!valorDiaria.HasValue || valorDiaria <= 0))
            return "Informe o valor da diária.";
        if (comissaoVendasAtiva && (!comissaoVendasPercentual.HasValue || comissaoVendasPercentual <= 0))
            return "Informe o percentual de comissão em vendas.";
        return null;
    }

    // Só vale o que é do tipo escolhido: salário só no salário fixo, diária só na diária, % de vendas só com vendas ligada
    public static decimal? SalarioAplicavel(string tipo, decimal? salarioFixo) => tipo == TipoRemuneracao.SalarioFixo ? salarioFixo : null;
    public static decimal? DiariaAplicavel(string tipo, decimal? valorDiaria) => tipo == TipoRemuneracao.Diaria ? valorDiaria : null;
    public static decimal? PercentualVendasAplicavel(bool ativa, decimal? percentual) => ativa ? percentual : null;

    // "servico" (só mão de obra) ou "total" (peça + serviço); qualquer outra coisa é "total"
    public static string BaseDeCalculo(string? baseCalculo) => baseCalculo == "servico" ? "servico" : "total";

    // Dia de pagamento: de 1 a 28, senão nenhum
    public static int? DiaValido(int? dia) => dia is >= 1 and <= 28 ? dia : null;
    public static int DiaDoLancamentoFixo(int? dia) => DiaValido(dia) ?? DiaPadraoDoSalario;

    // Texto do endereço: sem espaços nas pontas, vazio vira nulo e corta no tamanho máximo da coluna
    public static string? Limpar(string? valor, int tamanhoMaximo)
    {
        var t = valor?.Trim();
        if (string.IsNullOrEmpty(t)) return null;
        return t.Length > tamanhoMaximo ? t[..tamanhoMaximo] : t;
    }

    public static string? Uf(string? valor) => Limpar(valor, 2)?.ToUpperInvariant();

    // ── Fechamento de comissões ──────────────────────────────────────────────

    public static string? ValidarFechamento(int qtdDiarias, decimal? valorDiariaDoProfissional)
    {
        if (qtdDiarias < 0) return "Quantidade de diárias inválida.";
        if (qtdDiarias > 0 && (!valorDiariaDoProfissional.HasValue || valorDiariaDoProfissional <= 0))
            return "Este profissional não tem valor de diária cadastrado.";
        return null;
    }

    public static decimal ValorDasDiarias(int qtdDiarias, decimal? valorDiaria) => qtdDiarias * (valorDiaria ?? 0);

    // O período vai do começo do primeiro dia até o fim do último (limite superior exclusivo), em UTC
    public static DateTime InicioDoPeriodo(DateTime inicio) => DateTime.SpecifyKind(inicio.Date, DateTimeKind.Utc);
    public static DateTime FimDoPeriodoExclusivo(DateTime fim) => DateTime.SpecifyKind(fim.Date, DateTimeKind.Utc).AddDays(1);
    public static DateTime UltimoDiaDoPeriodo(DateTime fim) => DateTime.SpecifyKind(fim.Date, DateTimeKind.Utc);

    // Vencimento da conta a pagar: o informado, ou 5 dias depois do fim do período — sempre ao meio-dia (UTC)
    public static DateTime VencimentoDoFechamento(DateTime? vencimento, DateTime periodoFim) => vencimento.HasValue
        ? DateTime.SpecifyKind(vencimento.Value.Date, DateTimeKind.Utc).AddHours(12)
        : DateTime.SpecifyKind(periodoFim.Date, DateTimeKind.Utc).AddDays(5).AddHours(12);

    public static string DescricaoDoFechamento(string nomeProfissional, int qtdDiarias, DateTime periodoInicio, DateTime periodoFim)
    {
        var diarias = qtdDiarias > 0 ? $" + {qtdDiarias} diária(s)" : "";
        return $"Comissão{diarias} — {nomeProfissional} ({periodoInicio:dd/MM} a {periodoFim:dd/MM})";
    }

    // Título da comissão de uma ordem de serviço: "Ordem de Serviço — Gol 2015 · ABC1D23"
    public static string NomeDaOrdemDeServico(string? veiculo, string? placa)
    {
        var detalhe = string.Join(" · ", new[] { veiculo, placa }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrEmpty(detalhe) ? "Ordem de Serviço" : $"Ordem de Serviço — {detalhe}";
    }
}
