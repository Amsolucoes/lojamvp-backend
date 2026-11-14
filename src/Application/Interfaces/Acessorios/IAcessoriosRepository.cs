using LojaApi.src.Models;

namespace LojaApi.Application.Acessorios;

// Acesso aos dados da loja de acessórios. Os handlers decidem as regras; aqui só se lê e grava.
// Implementado em Infrastructure/Acessorios (EF Core).
public interface IAcessoriosRepository
{
    // Pedidos
    Task<List<PedidoAcessorio>> ListarPedidosVencidosAsync(DateTime agora);        // aguardando pagamento e já vencidos, com os itens
    Task<PedidoAcessorio?> ObterPedidoAsync(Guid id);                              // sem os itens
    Task<PedidoAcessorio?> ObterPedidoComItensAsync(Guid id);
    Task<PedidoAcessorio?> ObterPedidoPelaChaveAsync(Guid id);                     // busca direta pela chave (sem consulta extra se já estiver carregado)
    Task<List<PedidoAcessorioResumoDto>> ListarPedidosResumoAsync(string? status);
    Task<List<PedidoAcessorio>> ListarPedidosComItensAsync(string? status);
    Task<PedidoAcessorio?> ObterUltimoPedidoPagoComProdutoAsync(string emailEmMinusculas, Guid produtoId);
    void Adicionar(PedidoAcessorio pedido);
    void Remover(PedidoAcessorio pedido);

    // Produtos
    Task<ProdutoAcessorio?> ObterProdutoAsync(Guid id);
    Task<ProdutoAcessorio?> ObterProdutoAtivoAsync(Guid id);
    Task<List<ProdutoAcessorio>> ListarProdutosAsync();
    Task<List<ProdutoVitrineDto>> ListarVitrineAsync(string? categoria, DateTime limiteNovo);
    Task<bool> ProdutoTemPedidosAsync(Guid id);
    void Adicionar(ProdutoAcessorio produto);
    void Remover(ProdutoAcessorio produto);

    // Categorias
    Task<List<CategoriaAcessorio>> ListarCategoriasAtivasAsync();
    Task<CategoriaAcessorio?> ObterCategoriaAsync(Guid id);
    Task<bool> CategoriaComChaveExisteAsync(string chave);
    Task<int> MaiorOrdemDeCategoriaAsync();                                        // -1 quando ainda não há nenhuma
    Task<bool> CategoriaEmUsoAsync(string chave);
    void Adicionar(CategoriaAcessorio categoria);
    void Remover(CategoriaAcessorio categoria);

    // Avaliações
    Task<bool> JaAvaliouAsync(Guid pedidoId, Guid produtoId);
    Task<List<AvaliacaoPublicaDto>> ListarAvaliacoesAsync(Guid produtoId);
    void Adicionar(AvaliacaoAcessorio avaliacao);

    Task SalvarAsync();
}
