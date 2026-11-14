namespace LojaApi.Application.OrdemServico;

// O que OrdemServico RECEBE no corpo da requisição

// Checklist
public record SalvarChecklistCategoriaRequest(string Nome, int Ordem, bool Ativa);
public record SalvarChecklistItemRequest(Guid CategoriaId, string Nome, int Ordem, bool Ativo);

// Orçamento / ordem
public record ItemOrcamentoRequest(string Tipo, Guid? ProdutoId, Guid? ServicoId, string Descricao, int Quantidade, decimal ValorUnitario);
public record MecanicoOrcamentoRequest(Guid ProfissionalId, decimal ComissaoPercentual);
public record ChecklistRespostaRequest(Guid ChecklistItemId, string Estado, string? Observacao);

public record CriarOrcamentoRequest(
    Guid ClienteId,
    string? VeiculoDescricao,
    string? Placa,
    string? Observacoes,
    List<ItemOrcamentoRequest> Itens,
    List<MecanicoOrcamentoRequest> Mecanicos,
    List<ChecklistRespostaRequest>? ChecklistRespostas
);

public record ConcluirOrcamentoRequest(Guid ContaBancariaId, DateTime? Vencimento, bool JaPago = false, string? FormaPagamento = null, int? Parcelas = null);
public record AjustarDatasRequest(DateTime? AprovadoEm, DateTime? ConcluidoEm);

// Patrimônio
public record SalvarItemPatrimonioRequest(string Nome, string? Categoria, int QuantidadeEsperada, decimal ValorUnitario, string? Observacao, bool Ativo);
public record ItemContagemRequest(Guid ItemPatrimonioId, int QuantidadeContada, string? Observacao);
public record CriarContagemRequest(DateTime? DataContagem, string? Responsavel, string? Observacao, List<ItemContagemRequest> Itens);
