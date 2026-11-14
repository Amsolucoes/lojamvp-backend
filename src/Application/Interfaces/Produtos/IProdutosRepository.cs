using LojaApi.Models;
using LojaApi.src.Models;
using LojaApi.src.Models.Etiquetas;

namespace LojaApi.Application.Produtos;

// Acesso aos dados de Produtos (cadastro, variações, estoque, categorias, marcas, fornecedores e etiquetas).
// Os handlers decidem as regras; aqui só se lê e grava. Implementado em Infrastructure/Produtos (EF Core).
// Nas buscas "(id, lojaId)": com loja, só devolve o que é dela; sem loja (usuário sem vínculo), não filtra.
public interface IProdutosRepository
{
    // ── Produtos ──
    Task<List<string>> ListarCodigosInternosAsync(Guid? lojaId);                                   // códigos de barras que começam com "2", do produto e das variações
    Task<List<Produto>> ListarProdutosAsync(Guid? lojaId, string? busca, string? categoria, bool? ativo, Guid? marcaId);   // com variações, marca e fornecedor; por nome
    Task<Produto?> ObterProdutoCompletoAsync(Guid id);                                              // com variações, marca e fornecedor (qualquer loja)
    Task<Produto?> ObterProdutoAsync(Guid id);                                                      // qualquer loja
    Task<bool> CodigoDeBarrasDeProdutoEmUsoAsync(string codigo, Guid? lojaId, Guid? ignorarProdutoId);
    Task<List<Produto>> ListarProdutosDaLojaPorIdsAsync(IEnumerable<Guid> ids, Guid? lojaId);
    Task<int> AtualizarStatusEmLoteAsync(IEnumerable<Guid> ids, bool ativo, Guid? lojaId);         // grava na hora
    Task<bool> ProdutoTemVendasAsync(Guid id);
    Task ExcluirProdutoComDependenciasAsync(Guid id);                                              // variações, movimentos e o produto; grava na hora
    void Adicionar(Produto produto);

    // ── Variações ──
    Task<List<ProdutoVariacaoDto>> ListarVariacoesAsync(Guid produtoId);                           // por tamanho e cor
    Task<ProdutoVariacao?> ObterVariacaoDoProdutoAsync(Guid produtoId, Guid variacaoId, Guid? lojaId);   // com o produto
    Task<bool> CodigoDeBarrasDeVariacaoEmUsoAsync(string codigo, Guid? lojaId, Guid? ignorarVariacaoId);
    void Adicionar(ProdutoVariacao variacao);

    // ── Marca e fornecedor do produto ──
    Task<Marca?> ObterMarcaAsync(Guid id, Guid? lojaId);
    Task<Fornecedor?> ObterFornecedorAsync(Guid id, Guid? lojaId);

    // ── Estoque ──
    Task<List<MovimentoDto>> ListarMovimentosAsync(Guid? lojaId, Guid? produtoId, string? tipo, DateTime? de, DateTime? ate);
    Task<List<AlertaEstoqueDto>> ListarAlertasAsync(Guid? lojaId);
    void Adicionar(MovimentoEstoque movimento);

    // ── Categorias ──
    Task<List<CategoriaProdutoDto>> ListarCategoriasAsync(Guid lojaId);                            // ativas, por ordem e nome
    Task<CategoriaLoja?> ObterCategoriaAsync(Guid id, Guid lojaId);
    Task<bool> CategoriaComNomeExisteAsync(Guid lojaId, string nome, Guid? ignorarId);            // entre as ativas, sem diferenciar maiúscula
    Task<int> ProximaOrdemDeCategoriaAsync(Guid lojaId);
    Task<int> ContarProdutosAtivosDaCategoriaAsync(Guid lojaId, string nomeCategoria);
    Task RenomearCategoriaDosProdutosAsync(Guid lojaId, string nomeAntigo, string nomeNovo);       // grava na hora
    void Adicionar(CategoriaLoja categoria);

    // ── Marcas ──
    Task<List<MarcaDto>> ListarMarcasAsync(Guid lojaId);                                           // ativas, por nome
    Task<bool> MarcaComNomeExisteAsync(Guid lojaId, string nome, Guid? ignorarId);                // entre as ativas, sem diferenciar maiúscula
    Task<int> ContarProdutosAtivosDaMarcaAsync(Guid marcaId);
    void Adicionar(Marca marca);

    // ── Fornecedores ──
    Task<List<FornecedorDto>> ListarFornecedoresAsync(Guid? lojaId, string? busca);
    Task<List<FornecedorResumoDto>> ListarFornecedoresAtivosAsync(Guid lojaId);
    Task<Fornecedor?> ObterFornecedorComProdutosAsync(Guid id);                                     // qualquer loja
    Task<int> ContarProdutosAtivosDoFornecedorAsync(Guid fornecedorId);
    Task<int> ContarProdutosDoFornecedorAsync(Guid fornecedorId);
    void Adicionar(Fornecedor fornecedor);
    void Remover(Fornecedor fornecedor);

    // ── Modelos de etiqueta ──
    Task<List<ConfiguracaoEtiqueta>> ListarModelosAsync(Guid lojaId);                              // o padrão primeiro, depois por nome
    Task<ConfiguracaoEtiqueta?> ObterModeloAsync(Guid id, Guid? lojaId);
    Task<int> ContarModelosAsync(Guid? lojaId);
    Task<ConfiguracaoEtiqueta?> ObterQualquerModeloAsync(Guid? lojaId);
    void Adicionar(ConfiguracaoEtiqueta modelo);
    void Remover(ConfiguracaoEtiqueta modelo);

    Task SalvarAsync();
}
