namespace LojaApi.Application.Acessorios;

// Comandos da loja de acessórios: o que muda dados (cada um tem o seu handler em Application/Handlers/Acessorios).
// Há duas famílias de rotas de pedidos (/api/loja-acessorios e /api/loja-acessorios/produtos) que se comportam de forma um pouco
// diferente; as diferenças estão nos comandos "...Simples" e "...ComReativacao" abaixo.

// Pedidos
public record CriarPedidoPixCommand(CriarPedidoRequest Req);
public record CriarPedidoCartaoCommand(CriarPedidoCartaoRequest Req);
public record ConsultarStatusPedidoCommand(Guid Id);                      // confere o pagamento; só olha quem está aguardando
public record ConsultarStatusPedidoComReativacaoCommand(Guid Id);         // também reativa o pedido vencido cujo Pix caiu depois do prazo
public record AtualizarStatusPedidoCommand(Guid Id, AtualizarStatusPedidoRequest Req);            // devolve o estoque ao cancelar
public record AtualizarStatusPedidoSimplesCommand(Guid Id, AtualizarStatusPedidoRequest Req);     // não mexe no estoque

// Produtos
public record CriarProdutoAcessorioCommand(SalvarProdutoAcessorioRequest Req);
public record AtualizarProdutoAcessorioCommand(Guid Id, SalvarProdutoAcessorioRequest Req);
public record ExcluirProdutoAcessorioCommand(Guid Id);

// Categorias
public record CriarCategoriaAcessorioCommand(SalvarCategoriaAcessorioRequest Req);
public record AtualizarCategoriaAcessorioCommand(Guid Id, SalvarCategoriaAcessorioRequest Req);
public record ExcluirCategoriaAcessorioCommand(Guid Id);

// Avaliações
public record CriarAvaliacaoAcessorioCommand(CriarAvaliacaoRequest Req);
