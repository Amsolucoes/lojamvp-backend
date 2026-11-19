namespace LojaApi.Application.Bonificacoes;

// O que a API de Bonificações DEVOLVE

public record BonificacaoDto(Guid Id, Guid? ClienteId, string NomeCliente, Guid? ProdutoId, Guid? VariacaoId, string NomeProduto, decimal Quantidade,
    decimal CustoUnitario, decimal CustoTotal, string? Observacao, Guid? VendaId, Guid? UsuarioId, DateTime CriadoEm, DateTime? EstornadaEm);

public record BonificacaoPorProdutoDto(Guid? ProdutoId, string NomeProduto, decimal Quantidade, decimal Custo);

public record BonificacaoPorClienteDto(Guid? ClienteId, string NomeCliente, int Bonificacoes, decimal Quantidade, decimal Custo);

// Quanto a loja gastou em brindes no período (não conta as estornadas)
public record ResumoBonificacoesDto(int TotalBonificacoes, decimal TotalQuantidade, decimal TotalCusto,
    List<BonificacaoPorProdutoDto> PorProduto, List<BonificacaoPorClienteDto> PorCliente);
