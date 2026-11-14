namespace LojaApi.Domain.OrdemServico;

// Item de uma ordem, no que importa para calcular a comissão
public record ItemComissionavel(string Tipo, Guid? ServicoId, decimal ValorTotal);

// Resultado da comissão de um mecânico: base de cálculo, percentual efetivo (média do que foi aplicado) e valor
public record ComissaoCalculada(decimal ValorBase, decimal PercentualEfetivo, decimal Valor);

// Regras puras de OrdemServico (sem banco): estados da ordem, itens, pagamento, comissão dos mecânicos e patrimônio.
public static class OrdemServicoRegras
{
    public const string Pendente = "pendente";
    public const string EmAndamento = "em_andamento";
    public const string Concluido = "concluido";
    public const string Cancelado = "cancelado";

    public const string TipoPeca = "peca";
    public const string TipoServico = "servico";

    // ── Itens ──

    public static bool TipoDeItemValido(string tipo) => tipo == TipoPeca || tipo == TipoServico;

    public static decimal SubtotalDoItem(int quantidade, decimal valorUnitario) => quantidade * valorUnitario;

    // Placa em branco vira nula; o resto fica sem espaços e em maiúsculas (para a busca)
    public static string? NormalizarPlaca(string? placa) => string.IsNullOrWhiteSpace(placa) ? null : placa.Trim().ToUpperInvariant();

    // Se o mecânico veio sem percentual, vale o padrão do cadastro dele
    public static decimal PercentualDoMecanico(decimal informado, decimal? padraoDoCadastro) => informado > 0 ? informado : (padraoDoCadastro ?? 0);

    // ── Estados ──

    public static bool PodeEditar(string status) => status == Pendente || status == EmAndamento;
    public static bool PodeAprovar(string status) => status == Pendente;
    public static bool PodeCancelar(string status) => status != Concluido;
    public static bool PodeReabrir(string status) => status == Cancelado;
    public static bool PodeConcluir(string status) => status == EmAndamento;
    public static bool PodeDesfazerConclusao(string status) => status == Concluido;
    public static bool PodeExcluir(string status) => status == Pendente;
    public static bool PodeAjustarDatas(string status) => status != Pendente && status != Cancelado;

    // ── Datas ──

    // Datas de entrada e saída informadas viram UTC; o que não veio fica como estava. A saída não pode ser antes da entrada.
    public static (DateTime? AprovadoEm, DateTime? ConcluidoEm, string? Erro) AjustarDatas(DateTime? aprovadoAtual, DateTime? concluidoAtual, DateTime? aprovadoNovo, DateTime? concluidoNovo)
    {
        DateTime? aprovadoEm = aprovadoNovo.HasValue ? DateTime.SpecifyKind(aprovadoNovo.Value, DateTimeKind.Utc) : aprovadoAtual;
        DateTime? concluidoEm = concluidoNovo.HasValue ? DateTime.SpecifyKind(concluidoNovo.Value, DateTimeKind.Utc) : concluidoAtual;

        if (aprovadoEm.HasValue && concluidoEm.HasValue && concluidoEm.Value < aprovadoEm.Value)
            return (null, null, "A data/hora de saída não pode ser antes da entrada.");

        return (aprovadoEm, concluidoEm, null);
    }

    // ── Financeiro da conclusão ──

    // Vencimento escolhido fica ao meio-dia UTC (não "escorrega" de dia); sem vencimento, vale agora
    public static DateTime VencimentoDoLancamento(DateTime? vencimento, DateTime agoraUtc) =>
        vencimento.HasValue ? DateTime.SpecifyKind(vencimento.Value.Date, DateTimeKind.Utc).AddHours(12) : agoraUtc;

    // O lançamento não tem campo de forma de pagamento: ela vai como observação ("Pago via pix", "Pago via cartao em 3x")
    public static string? ObservacaoDoPagamento(bool jaPago, string? forma, int? parcelas) =>
        jaPago ? $"Pago via {forma}" + (parcelas is > 1 ? $" em {parcelas}x" : "") : null;

    public static string DescricaoDoLancamento(string? veiculo, Guid orcamentoId) => $"Ordem de Serviço — {veiculo ?? orcamentoId.ToString()[..8]}";

    // ── Comissão ──

    // A base é "total" (peças + serviços) ou "servico" (só mão de obra). Dentro dela, um serviço do catálogo pode ter uma
    // exceção de percentual para o mecânico (ex.: 0%), que vale só naquele item. O percentual registrado é a média ponderada
    // do que foi de fato aplicado. Sem percentual na ordem, ou com comissão zero, não há comissão (devolve nulo).
    public static ComissaoCalculada? CalcularComissao(IEnumerable<ItemComissionavel> itens, string? baseCalculo, decimal percentualDaOrdem, Func<Guid, decimal?> excecaoDoServico)
    {
        if (percentualDaOrdem <= 0) return null;

        var itensBase = baseCalculo == "servico" ? itens.Where(i => i.Tipo == TipoServico) : itens;

        decimal valorBase = 0;
        decimal valorComissao = 0;

        foreach (var item in itensBase)
        {
            var excecao = item.Tipo == TipoServico && item.ServicoId.HasValue ? excecaoDoServico(item.ServicoId.Value) : null;
            var percentualItem = excecao ?? percentualDaOrdem;

            valorBase += item.ValorTotal;
            valorComissao += item.ValorTotal * (percentualItem / 100m);
        }

        valorComissao = Math.Round(valorComissao, 2);
        if (valorComissao <= 0) return null;

        var percentualEfetivo = valorBase > 0 ? Math.Round((valorComissao / valorBase) * 100m, 2) : percentualDaOrdem;
        return new ComissaoCalculada(valorBase, percentualEfetivo, valorComissao);
    }

    // ── Patrimônio ──

    public static string? NormalizarCategoriaDePatrimonio(string? categoria) => string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();

    public static decimal ValorTotalDoPatrimonio(int quantidadeEsperada, decimal valorUnitario) => quantidadeEsperada * valorUnitario;

    // Contado menos esperado: negativo = faltando, positivo = sobrando
    public static int DiferencaDaContagem(int quantidadeContada, int quantidadeEsperada) => quantidadeContada - quantidadeEsperada;

    public static DateTime DataDaContagem(DateTime? data, DateTime agoraUtc) => data.HasValue ? DateTime.SpecifyKind(data.Value, DateTimeKind.Utc) : agoraUtc;
}
