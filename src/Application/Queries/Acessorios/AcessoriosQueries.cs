namespace LojaApi.Application.Acessorios;

// Consultas da loja de acessórios: só leem dados (cada uma tem o seu handler em Application/Handlers/Acessorios).
// A vitrine e o detalhe do produto também liberam, antes, o estoque de pedidos vencidos (não precisa de rotina em segundo plano).

public record CalcularFreteQuery(string Uf);                         // rota /api/loja-acessorios/frete
public record ConsultarFreteQuery(string Uf, decimal Subtotal);      // rota /api/loja-acessorios/produtos/frete

public record ListarPedidosResumoQuery(string? Status);              // resumo do pedido (rota /api/loja-acessorios/pedidos)
public record ListarPedidosCompletosQuery(string? Status);           // o pedido inteiro (rota /api/loja-acessorios/produtos/pedidos)

public record ListarVitrineQuery(string? Categoria);
public record BuscarProdutoAcessorioQuery(Guid Id);
public record ListarTodosProdutosAcessorioQuery;
public record ListarCategoriasAcessorioQuery;
public record ListarAvaliacoesAcessorioQuery(Guid ProdutoId);
