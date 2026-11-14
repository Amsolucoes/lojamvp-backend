using LojaApi.Application.Produtos;
using LojaApi.Models;
using LojaApi.src.Models;
using LojaApi.src.Models.Etiquetas;

namespace LojaApi.Tests.Suporte.Falsos;

// Repositório de Produtos em memória: permite testar os HANDLERS (regras) sem banco.
// "Salvamentos" conta as gravações pedidas pelo handler (zero = nada foi gravado).
public sealed class RepositorioProdutosFalso : IProdutosRepository
{
    public List<Produto> Produtos { get; } = [];
    public List<ProdutoVariacao> Variacoes { get; } = [];
    public List<MovimentoEstoque> Movimentos { get; } = [];
    public List<CategoriaLoja> Categorias { get; } = [];
    public List<Marca> Marcas { get; } = [];
    public List<Fornecedor> Fornecedores { get; } = [];
    public List<ConfiguracaoEtiqueta> Modelos { get; } = [];
    public HashSet<Guid> ProdutosVendidos { get; } = [];
    public int Salvamentos { get; private set; }
    public int CategoriasRenomeadas { get; private set; }

    // ── Produtos ──
    public Task<List<string>> ListarCodigosInternosAsync(Guid? lojaId) => Task.FromResult(
        Produtos.Where(p => p.LojaId == lojaId && p.CodigoBarras != null && p.CodigoBarras.StartsWith("2")).Select(p => p.CodigoBarras!)
            .Concat(Variacoes.Where(v => Produtos.Any(p => p.Id == v.ProdutoId && p.LojaId == lojaId) && v.CodigoBarras != null && v.CodigoBarras.StartsWith("2")).Select(v => v.CodigoBarras!)).ToList());
    public Task<List<Produto>> ListarProdutosAsync(Guid? lojaId, string? busca, string? categoria, bool? ativo, Guid? marcaId) => Task.FromResult(
        Produtos.Where(p => (lojaId == null || p.LojaId == lojaId) && (categoria == null || p.Categoria == categoria) && (ativo == null || p.Ativo == ativo) && (marcaId == null || p.MarcaId == marcaId))
            .OrderBy(p => p.Nome).Select(ComVariacoes).ToList());
    public Task<Produto?> ObterProdutoCompletoAsync(Guid id) => Task.FromResult(Produtos.Where(p => p.Id == id).Select(ComVariacoes).FirstOrDefault());
    public Task<Produto?> ObterProdutoAsync(Guid id) => Task.FromResult(Produtos.FirstOrDefault(p => p.Id == id));
    public Task<bool> CodigoDeBarrasDeProdutoEmUsoAsync(string codigo, Guid? lojaId, Guid? ignorarProdutoId) =>
        Task.FromResult(Produtos.Any(p => p.CodigoBarras == codigo && p.LojaId == lojaId && (ignorarProdutoId == null || p.Id != ignorarProdutoId)));
    public Task<List<Produto>> ListarProdutosDaLojaPorIdsAsync(IEnumerable<Guid> ids, Guid? lojaId) =>
        Task.FromResult(Produtos.Where(p => ids.Contains(p.Id) && (lojaId == null || p.LojaId == lojaId)).ToList());
    public Task<int> AtualizarStatusEmLoteAsync(IEnumerable<Guid> ids, bool ativo, Guid? lojaId)
    {
        var alvo = Produtos.Where(p => ids.Contains(p.Id) && (lojaId == null || p.LojaId == lojaId)).ToList();
        foreach (var p in alvo) p.Ativo = ativo;
        return Task.FromResult(alvo.Count);
    }
    public Task<bool> ProdutoTemVendasAsync(Guid id) => Task.FromResult(ProdutosVendidos.Contains(id));
    public Task ExcluirProdutoComDependenciasAsync(Guid id)
    {
        Variacoes.RemoveAll(v => v.ProdutoId == id);
        Movimentos.RemoveAll(m => m.ProdutoId == id);
        Produtos.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }
    public void Adicionar(Produto produto) => Produtos.Add(produto);

    private Produto ComVariacoes(Produto p) { p.Variacoes = Variacoes.Where(v => v.ProdutoId == p.Id).ToList(); return p; }

    // ── Variações ──
    public Task<List<ProdutoVariacaoDto>> ListarVariacoesAsync(Guid produtoId) => Task.FromResult(
        Variacoes.Where(v => v.ProdutoId == produtoId).OrderBy(v => v.Tamanho).ThenBy(v => v.Cor)
            .Select(v => new ProdutoVariacaoDto(v.Id, v.Tamanho, v.Cor, v.OutroCampo, v.CodigoBarras, v.Estoque, v.EstoqueMinimo, v.Ativo)).ToList());
    public Task<ProdutoVariacao?> ObterVariacaoDoProdutoAsync(Guid produtoId, Guid variacaoId, Guid? lojaId) => Task.FromResult(
        Variacoes.FirstOrDefault(v => v.Id == variacaoId && v.ProdutoId == produtoId && (lojaId == null || Produtos.Any(p => p.Id == produtoId && p.LojaId == lojaId))));
    public Task<bool> CodigoDeBarrasDeVariacaoEmUsoAsync(string codigo, Guid? lojaId, Guid? ignorarVariacaoId) => Task.FromResult(
        Variacoes.Any(v => v.CodigoBarras == codigo && Produtos.Any(p => p.Id == v.ProdutoId && p.LojaId == lojaId) && (ignorarVariacaoId == null || v.Id != ignorarVariacaoId)));
    public void Adicionar(ProdutoVariacao variacao) => Variacoes.Add(variacao);

    // ── Marca e fornecedor ──
    public Task<Marca?> ObterMarcaAsync(Guid id, Guid? lojaId) => Task.FromResult(Marcas.FirstOrDefault(m => m.Id == id && m.LojaId == lojaId));
    public Task<Fornecedor?> ObterFornecedorAsync(Guid id, Guid? lojaId) => Task.FromResult(Fornecedores.FirstOrDefault(f => f.Id == id && (lojaId == null || f.LojaId == lojaId)));

    // ── Estoque ──
    public Task<List<MovimentoDto>> ListarMovimentosAsync(Guid? lojaId, Guid? produtoId, string? tipo, DateTime? de, DateTime? ate) => Task.FromResult(new List<MovimentoDto>());
    public Task<List<AlertaEstoqueDto>> ListarAlertasAsync(Guid? lojaId) => Task.FromResult(
        Produtos.Where(p => p.Ativo && p.Estoque <= p.EstoqueMinimo && (lojaId == null || p.LojaId == lojaId))
            .Select(p => new AlertaEstoqueDto(p.Id, p.Nome, p.Categoria, p.Estoque, p.EstoqueMinimo, p.Estoque == 0)).ToList());
    public void Adicionar(MovimentoEstoque movimento) => Movimentos.Add(movimento);

    // ── Categorias ──
    public Task<List<CategoriaProdutoDto>> ListarCategoriasAsync(Guid lojaId) => Task.FromResult(
        Categorias.Where(c => c.LojaId == lojaId && c.Ativo).OrderBy(c => c.Ordem).Select(c => new CategoriaProdutoDto(c.Id, c.Nome, c.TipoTamanho, c.UsaTamanho, c.UsaCor, c.TamanhosPersonalizados)).ToList());
    public Task<CategoriaLoja?> ObterCategoriaAsync(Guid id, Guid lojaId) => Task.FromResult(Categorias.FirstOrDefault(c => c.Id == id && c.LojaId == lojaId));
    public Task<bool> CategoriaComNomeExisteAsync(Guid lojaId, string nome, Guid? ignorarId) => Task.FromResult(
        Categorias.Any(c => c.LojaId == lojaId && (ignorarId == null || c.Id != ignorarId) && c.Nome.ToLower() == nome.ToLower() && c.Ativo));
    public Task<int> ProximaOrdemDeCategoriaAsync(Guid lojaId) => Task.FromResult(Categorias.Where(c => c.LojaId == lojaId).Select(c => c.Ordem + 1).DefaultIfEmpty(0).Max());
    public Task<int> ContarProdutosAtivosDaCategoriaAsync(Guid lojaId, string nomeCategoria) => Task.FromResult(Produtos.Count(p => p.LojaId == lojaId && p.Categoria == nomeCategoria && p.Ativo));
    public Task RenomearCategoriaDosProdutosAsync(Guid lojaId, string nomeAntigo, string nomeNovo)
    {
        foreach (var p in Produtos.Where(p => p.LojaId == lojaId && p.Categoria == nomeAntigo)) p.Categoria = nomeNovo;
        CategoriasRenomeadas++;
        return Task.CompletedTask;
    }
    public void Adicionar(CategoriaLoja categoria) => Categorias.Add(categoria);

    // ── Marcas ──
    public Task<List<MarcaDto>> ListarMarcasAsync(Guid lojaId) => Task.FromResult(Marcas.Where(m => m.LojaId == lojaId && m.Ativo).OrderBy(m => m.Nome).Select(m => new MarcaDto(m.Id, m.Nome)).ToList());
    public Task<bool> MarcaComNomeExisteAsync(Guid lojaId, string nome, Guid? ignorarId) => Task.FromResult(
        Marcas.Any(m => m.LojaId == lojaId && (ignorarId == null || m.Id != ignorarId) && m.Nome.ToLower() == nome.ToLower() && m.Ativo));
    public Task<int> ContarProdutosAtivosDaMarcaAsync(Guid marcaId) => Task.FromResult(Produtos.Count(p => p.MarcaId == marcaId && p.Ativo));
    public void Adicionar(Marca marca) => Marcas.Add(marca);

    // ── Fornecedores ──
    public Task<List<FornecedorDto>> ListarFornecedoresAsync(Guid? lojaId, string? busca) => Task.FromResult(new List<FornecedorDto>());
    public Task<List<FornecedorResumoDto>> ListarFornecedoresAtivosAsync(Guid lojaId) => Task.FromResult(
        Fornecedores.Where(f => f.LojaId == lojaId && f.Ativo).OrderBy(f => f.Nome).Select(f => new FornecedorResumoDto(f.Id, f.Nome)).ToList());
    public Task<Fornecedor?> ObterFornecedorComProdutosAsync(Guid id) => Task.FromResult(Fornecedores.Where(f => f.Id == id).Select(f => { f.Produtos = Produtos.Where(p => p.FornecedorId == f.Id).ToList(); return f; }).FirstOrDefault());
    public Task<int> ContarProdutosAtivosDoFornecedorAsync(Guid fornecedorId) => Task.FromResult(Produtos.Count(p => p.FornecedorId == fornecedorId && p.Ativo));
    public Task<int> ContarProdutosDoFornecedorAsync(Guid fornecedorId) => Task.FromResult(Produtos.Count(p => p.FornecedorId == fornecedorId));
    public void Adicionar(Fornecedor fornecedor) => Fornecedores.Add(fornecedor);
    public void Remover(Fornecedor fornecedor) => Fornecedores.Remove(fornecedor);

    // ── Modelos de etiqueta ──
    public Task<List<ConfiguracaoEtiqueta>> ListarModelosAsync(Guid lojaId) => Task.FromResult(Modelos.Where(c => c.LojaId == lojaId).OrderByDescending(c => c.Padrao).ThenBy(c => c.Nome).ToList());
    public Task<ConfiguracaoEtiqueta?> ObterModeloAsync(Guid id, Guid? lojaId) => Task.FromResult(Modelos.FirstOrDefault(c => c.Id == id && c.LojaId == lojaId));
    public Task<int> ContarModelosAsync(Guid? lojaId) => Task.FromResult(Modelos.Count(c => c.LojaId == lojaId));
    public Task<ConfiguracaoEtiqueta?> ObterQualquerModeloAsync(Guid? lojaId) => Task.FromResult(Modelos.FirstOrDefault(c => c.LojaId == lojaId));
    public void Adicionar(ConfiguracaoEtiqueta modelo) => Modelos.Add(modelo);
    public void Remover(ConfiguracaoEtiqueta modelo) => Modelos.Remove(modelo);

    public Task SalvarAsync() { Salvamentos++; return Task.CompletedTask; }
}
