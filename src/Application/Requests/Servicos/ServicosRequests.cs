namespace LojaApi.Application.Servicos;

public record AgendarPublicoRequest(Guid ServicoId, string NomeCliente, string Telefone, DateTime DataHora);

public record SalvarAgendamentoRequest(
    Guid ServicoId,
    Guid? ClienteId,
    string? NomeCliente,
    decimal Preco,
    DateTime DataHora,
    int DuracaoMin,
    string? Observacao,
    Guid? ProfissionalId = null
);

public record StatusAgendamentoRequest(string Status);

public record SalvarServicoRequest(string Nome, string Categoria, decimal Preco, int DuracaoMin, bool Ativo);

public record SalvarPlanoRequest(string Nome, decimal Valor, string? ServicosIds, int? LimiteUsosMes = null, List<int>? DiasSemana = null);

public record VincularPlanoRequest(Guid ClienteId, Guid PlanoId, int DiaVencimento, bool IniciarProximoMes = false);

public record AtualizarAssinaturaRequest(Guid? PlanoId, int? DiaVencimento);

public record MarcarPagamentoPlanoRequest(bool Pago);

public record PagarMesItemRequest(Guid PagamentoId, decimal? ValorPago);

public record PagarMesesRequest(List<PagarMesItemRequest> Meses);
