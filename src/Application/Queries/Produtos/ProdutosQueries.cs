namespace LojaApi.Application.Produtos;

// Consultas de Produtos: só leem dados (cada uma tem o seu handler em Application/Handlers/Produtos).

public record GerarCodigosQuery(Guid? LojaId, int Quantidade, string? Ignorar);
public record ListarProdutosQuery(Guid? LojaId, string? Busca, string? Categoria, bool? Ativo, Guid? MarcaId);
public record BuscarProdutoQuery(Guid? LojaId, Guid Id);
public record ListarVariacoesQuery(Guid? LojaId, Guid ProdutoId);

public record ListarMovimentosEstoqueQuery(Guid? LojaId, Guid? ProdutoId, string? Tipo, DateTime? De, DateTime? Ate);
public record ListarAlertasEstoqueQuery(Guid? LojaId);

public record ListarCategoriasQuery(Guid? LojaId);
public record ListarMarcasQuery(Guid? LojaId);

public record ListarFornecedoresQuery(Guid? LojaId, string? Busca);
public record ListarFornecedoresAtivosQuery(Guid? LojaId);
public record BuscarFornecedorQuery(Guid? LojaId, Guid Id);
