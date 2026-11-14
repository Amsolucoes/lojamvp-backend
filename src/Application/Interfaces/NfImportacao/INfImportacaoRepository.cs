using LojaApi.Models;
using LojaApi.src.Models;

namespace LojaApi.Application.NfImportacao;

// Acesso aos dados da importação de nota fiscal (XML e lançamento manual). Os handlers decidem as regras;
// aqui só se lê e grava. Implementado em Infrastructure/NfImportacao (EF Core).
public interface INfImportacaoRepository
{
    // ── Notas ──
    Task<NfImportada?> ObterNotaAtivaPorChaveAsync(Guid lojaId, string chaveAcesso);              // só as não desfeitas
    Task<NfImportada?> ObterNotaAsync(Guid id, Guid lojaId);
    Task<bool> NotaDoFornecedorExisteAsync(Guid lojaId, Guid fornecedorId, string numeroNf, Guid? ignorarId);   // não desfeitas; sem diferenciar maiúscula
    Task<List<NfHistoricoDto>> ListarHistoricoAsync(Guid lojaId);                                  // da mais nova para a mais antiga
    void Adicionar(NfImportada nota);

    // ── Fornecedor ──
    Task<Fornecedor?> ObterFornecedorAsync(Guid id, Guid lojaId);

    // ── Produtos e variações ──
    Task<List<Produto>> ListarProdutosAtivosComVariacoesAsync(Guid lojaId);
    Task<Produto?> ObterProdutoComVariacoesAsync(Guid id);                                         // qualquer loja
    Task<bool> ProdutoTemVendasAsync(Guid id);
    Task ExcluirProdutoComDependenciasAsync(Guid id);                                              // variações, movimentos e o produto; grava na hora
    void Adicionar(Produto produto);
    void Adicionar(ProdutoVariacao variacao);
    void Remover(ProdutoVariacao variacao);

    // ── Categorias ──
    Task<List<CategoriaLoja>> ListarCategoriasAtivasAsync(Guid lojaId);
    Task<CategoriaLoja?> ObterCategoriaAtivaPorNomeAsync(Guid lojaId, string nome);               // sem diferenciar maiúscula
    Task<CategoriaLoja?> ObterCategoriaAsync(Guid id);
    Task<int> ProximaOrdemDeCategoriaAsync(Guid lojaId);
    Task<bool> CategoriaTemProdutoAtivoAsync(Guid lojaId, string nomeCategoria);
    void Adicionar(CategoriaLoja categoria);

    // ── Mapeamento código do fornecedor → produto ──
    Task<List<NfProdutoMapeamento>> ListarMapeamentosAsync(Guid lojaId, string cnpjFornecedor);
    Task<NfProdutoMapeamento?> ObterMapeamentoAsync(Guid lojaId, string cnpjFornecedor, string codigoFornecedor);
    void Adicionar(NfProdutoMapeamento mapeamento);

    // ── Movimentos de estoque da nota (achados pela observação) ──
    Task<List<MovimentoEstoque>> ListarMovimentosPorObservacaoAsync(Guid lojaId, string observacao);
    Task ExcluirMovimentosPorObservacaoAsync(Guid lojaId, string observacao);                     // grava na hora
    void Adicionar(MovimentoEstoque movimento);

    Task SalvarAsync();
}
