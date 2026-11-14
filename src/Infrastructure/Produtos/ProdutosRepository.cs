using LojaApi.Application.Produtos;
using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models;
using LojaApi.src.Models.Etiquetas;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Produtos;

// Acesso aos dados de Produtos (EF Core). Só lê e grava; as regras ficam nos handlers.
// Nas buscas "(id, lojaId)": com loja, só devolve o que é dela; sem loja (usuário sem vínculo), não filtra — igual ao comportamento antigo.
public class ProdutosRepository(AppDbContext db) : IProdutosRepository
{
    // ── Produtos ──

    public async Task<List<string>> ListarCodigosInternosAsync(Guid? lojaId)
    {
        var doProduto = await db.Produtos
            .Where(p => p.LojaId == lojaId && p.CodigoBarras != null && p.CodigoBarras.StartsWith("2"))
            .Select(p => p.CodigoBarras!).ToListAsync();
        var daVariacao = await db.ProdutoVariacoes
            .Where(v => v.Produto!.LojaId == lojaId && v.CodigoBarras != null && v.CodigoBarras.StartsWith("2"))
            .Select(v => v.CodigoBarras!).ToListAsync();
        return [.. doProduto, .. daVariacao];
    }

    public Task<List<Produto>> ListarProdutosAsync(Guid? lojaId, string? busca, string? categoria, bool? ativo, Guid? marcaId)
    {
        var q = db.Produtos.Include(p => p.Variacoes).Include(p => p.Marca).Include(p => p.Fornecedor).AsQueryable();

        if (lojaId.HasValue)
            q = q.Where(p => p.LojaId == lojaId);

        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(p => p.Nome.ToLower().Contains(busca.ToLower()) ||
                              (p.CodigoBarras != null && p.CodigoBarras.Contains(busca)));
        if (!string.IsNullOrWhiteSpace(categoria))
            q = q.Where(p => p.Categoria == categoria);
        if (ativo.HasValue)
            q = q.Where(p => p.Ativo == ativo.Value);
        if (marcaId.HasValue)
            q = q.Where(p => p.MarcaId == marcaId.Value);

        return q.OrderBy(p => p.Nome).ToListAsync();
    }

    public Task<Produto?> ObterProdutoCompletoAsync(Guid id) =>
        db.Produtos.Include(x => x.Variacoes).Include(x => x.Marca).Include(x => x.Fornecedor).FirstOrDefaultAsync(x => x.Id == id);

    public Task<Produto?> ObterProdutoAsync(Guid id) => db.Produtos.FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> CodigoDeBarrasDeProdutoEmUsoAsync(string codigo, Guid? lojaId, Guid? ignorarProdutoId) =>
        db.Produtos.AnyAsync(p => p.CodigoBarras == codigo && p.LojaId == lojaId && (ignorarProdutoId == null || p.Id != ignorarProdutoId));

    public Task<List<Produto>> ListarProdutosDaLojaPorIdsAsync(IEnumerable<Guid> ids, Guid? lojaId)
    {
        var lista = ids.ToList();
        return db.Produtos.Where(p => lista.Contains(p.Id) && (!lojaId.HasValue || p.LojaId == lojaId)).ToListAsync();
    }

    public Task<int> AtualizarStatusEmLoteAsync(IEnumerable<Guid> ids, bool ativo, Guid? lojaId)
    {
        var lista = ids.ToList();
        return db.Produtos
            .Where(p => lista.Contains(p.Id) && (!lojaId.HasValue || p.LojaId == lojaId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Ativo, ativo)
                .SetProperty(p => p.AtualizadoEm, DateTime.UtcNow));
    }

    public Task<bool> ProdutoTemVendasAsync(Guid id) => db.ItensVenda.AnyAsync(i => i.ProdutoId == id);

    public async Task ExcluirProdutoComDependenciasAsync(Guid id)
    {
        await db.ProdutoVariacoes.Where(v => v.ProdutoId == id).ExecuteDeleteAsync();
        await db.Movimentos.Where(m => m.ProdutoId == id).ExecuteDeleteAsync();
        await db.Produtos.Where(p => p.Id == id).ExecuteDeleteAsync();
    }

    public void Adicionar(Produto produto) => db.Produtos.Add(produto);

    // ── Variações ──

    public Task<List<ProdutoVariacaoDto>> ListarVariacoesAsync(Guid produtoId) =>
        db.ProdutoVariacoes
            .Where(v => v.ProdutoId == produtoId)
            .OrderBy(v => v.Tamanho).ThenBy(v => v.Cor)
            .Select(v => new ProdutoVariacaoDto(
                v.Id, v.Tamanho, v.Cor, v.OutroCampo,
                v.CodigoBarras, v.Estoque, v.EstoqueMinimo, v.Ativo))
            .ToListAsync();

    public Task<ProdutoVariacao?> ObterVariacaoDoProdutoAsync(Guid produtoId, Guid variacaoId, Guid? lojaId) =>
        db.ProdutoVariacoes.FirstOrDefaultAsync(v => v.Id == variacaoId && v.ProdutoId == produtoId && (!lojaId.HasValue || v.Produto!.LojaId == lojaId));

    public Task<bool> CodigoDeBarrasDeVariacaoEmUsoAsync(string codigo, Guid? lojaId, Guid? ignorarVariacaoId) =>
        db.ProdutoVariacoes.AnyAsync(v => v.CodigoBarras == codigo && v.Produto!.LojaId == lojaId && (ignorarVariacaoId == null || v.Id != ignorarVariacaoId));

    public void Adicionar(ProdutoVariacao variacao) => db.ProdutoVariacoes.Add(variacao);

    // ── Marca e fornecedor do produto ──

    public Task<Marca?> ObterMarcaAsync(Guid id, Guid? lojaId) => db.Marcas.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);

    public Task<Fornecedor?> ObterFornecedorAsync(Guid id, Guid? lojaId) =>
        db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id && (!lojaId.HasValue || f.LojaId == lojaId));

    // ── Estoque ──

    public Task<List<MovimentoDto>> ListarMovimentosAsync(Guid? lojaId, Guid? produtoId, string? tipo, DateTime? de, DateTime? ate)
    {
        var q = db.Movimentos.AsQueryable();

        if (lojaId.HasValue) q = q.Where(m => m.LojaId == lojaId);
        if (produtoId.HasValue) q = q.Where(m => m.ProdutoId == produtoId);
        if (!string.IsNullOrEmpty(tipo)) q = q.Where(m => m.Tipo == tipo);
        if (de.HasValue) q = q.Where(m => m.CriadoEm >= de.Value);
        if (ate.HasValue) q = q.Where(m => m.CriadoEm <= ate.Value.AddDays(1));

        return q.OrderByDescending(m => m.CriadoEm)
            .Select(m => new MovimentoDto(m.Id, m.ProdutoId, m.Produto.Nome, m.Tipo, m.Quantidade, m.Observacao, m.CriadoEm))
            .ToListAsync();
    }

    public Task<List<AlertaEstoqueDto>> ListarAlertasAsync(Guid? lojaId)
    {
        var q = db.Produtos.Where(p => p.Ativo && p.Estoque <= p.EstoqueMinimo);
        if (lojaId.HasValue) q = q.Where(p => p.LojaId == lojaId);

        return q.Select(p => new AlertaEstoqueDto(p.Id, p.Nome, p.Categoria, p.Estoque, p.EstoqueMinimo, p.Estoque == 0)).ToListAsync();
    }

    public void Adicionar(MovimentoEstoque movimento) => db.Movimentos.Add(movimento);

    // ── Categorias ──

    public Task<List<CategoriaProdutoDto>> ListarCategoriasAsync(Guid lojaId) =>
        db.CategoriasLoja
            .Where(c => c.LojaId == lojaId && c.Ativo)
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .Select(c => new CategoriaProdutoDto(c.Id, c.Nome, c.TipoTamanho, c.UsaTamanho, c.UsaCor, c.TamanhosPersonalizados))
            .ToListAsync();

    public Task<CategoriaLoja?> ObterCategoriaAsync(Guid id, Guid lojaId) => db.CategoriasLoja.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public Task<bool> CategoriaComNomeExisteAsync(Guid lojaId, string nome, Guid? ignorarId) =>
        db.CategoriasLoja.AnyAsync(c => c.LojaId == lojaId && (ignorarId == null || c.Id != ignorarId) && c.Nome.ToLower() == nome.ToLower() && c.Ativo);

    public async Task<int> ProximaOrdemDeCategoriaAsync(Guid lojaId) =>
        (await db.CategoriasLoja.Where(c => c.LojaId == lojaId).Select(c => (int?)c.Ordem).MaxAsync() ?? -1) + 1;

    public Task<int> ContarProdutosAtivosDaCategoriaAsync(Guid lojaId, string nomeCategoria) =>
        db.Produtos.CountAsync(p => p.LojaId == lojaId && p.Categoria == nomeCategoria && p.Ativo);

    public Task RenomearCategoriaDosProdutosAsync(Guid lojaId, string nomeAntigo, string nomeNovo) =>
        db.Produtos
            .Where(p => p.LojaId == lojaId && p.Categoria == nomeAntigo)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Categoria, nomeNovo));

    public void Adicionar(CategoriaLoja categoria) => db.CategoriasLoja.Add(categoria);

    // ── Marcas ──

    public Task<List<MarcaDto>> ListarMarcasAsync(Guid lojaId) =>
        db.Marcas
            .Where(m => m.LojaId == lojaId && m.Ativo)
            .OrderBy(m => m.Nome)
            .Select(m => new MarcaDto(m.Id, m.Nome))
            .ToListAsync();

    public Task<bool> MarcaComNomeExisteAsync(Guid lojaId, string nome, Guid? ignorarId) =>
        db.Marcas.AnyAsync(m => m.LojaId == lojaId && (ignorarId == null || m.Id != ignorarId) && m.Nome.ToLower() == nome.ToLower() && m.Ativo);

    public Task<int> ContarProdutosAtivosDaMarcaAsync(Guid marcaId) => db.Produtos.CountAsync(p => p.MarcaId == marcaId && p.Ativo);

    public void Adicionar(Marca marca) => db.Marcas.Add(marca);

    // ── Fornecedores ──

    public Task<List<FornecedorDto>> ListarFornecedoresAsync(Guid? lojaId, string? busca)
    {
        var q = db.Fornecedores.AsQueryable();

        if (lojaId.HasValue)
            q = q.Where(f => f.LojaId == lojaId);

        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(f =>
                f.Nome.ToLower().Contains(busca.ToLower()) ||
                (f.CnpjCpf != null && f.CnpjCpf.Contains(busca)) ||
                (f.Telefone != null && f.Telefone.Contains(busca)));

        return q.OrderBy(f => f.Nome)
            .Select(f => new FornecedorDto(
                f.Id, f.Nome, f.CnpjCpf, f.Telefone, f.Email,
                f.Endereco, f.Observacoes, f.Ativo, f.CriadoEm,
                f.Produtos.Count(p => p.Ativo),
                f.Cep, f.Rua, f.Numero, f.Bairro, f.Cidade, f.Uf
            )).ToListAsync();
    }

    public Task<List<FornecedorResumoDto>> ListarFornecedoresAtivosAsync(Guid lojaId) =>
        db.Fornecedores
            .Where(f => f.LojaId == lojaId && f.Ativo)
            .OrderBy(f => f.Nome)
            .Select(f => new FornecedorResumoDto(f.Id, f.Nome))
            .ToListAsync();

    public Task<Fornecedor?> ObterFornecedorComProdutosAsync(Guid id) =>
        db.Fornecedores.Include(x => x.Produtos).FirstOrDefaultAsync(x => x.Id == id);

    public Task<int> ContarProdutosAtivosDoFornecedorAsync(Guid fornecedorId) => db.Produtos.CountAsync(p => p.FornecedorId == fornecedorId && p.Ativo);

    public Task<int> ContarProdutosDoFornecedorAsync(Guid fornecedorId) => db.Produtos.CountAsync(p => p.FornecedorId == fornecedorId);

    public void Adicionar(Fornecedor fornecedor) => db.Fornecedores.Add(fornecedor);
    public void Remover(Fornecedor fornecedor) => db.Fornecedores.Remove(fornecedor);

    // ── Modelos de etiqueta ──

    public Task<List<ConfiguracaoEtiqueta>> ListarModelosAsync(Guid lojaId) =>
        db.ConfiguracoesEtiqueta
            .Where(c => c.LojaId == lojaId)
            .OrderByDescending(c => c.Padrao).ThenBy(c => c.Nome)
            .ToListAsync();

    public Task<ConfiguracaoEtiqueta?> ObterModeloAsync(Guid id, Guid? lojaId) =>
        db.ConfiguracoesEtiqueta.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public Task<int> ContarModelosAsync(Guid? lojaId) => db.ConfiguracoesEtiqueta.CountAsync(c => c.LojaId == lojaId);

    public Task<ConfiguracaoEtiqueta?> ObterQualquerModeloAsync(Guid? lojaId) => db.ConfiguracoesEtiqueta.FirstOrDefaultAsync(c => c.LojaId == lojaId);

    public void Adicionar(ConfiguracaoEtiqueta modelo) => db.ConfiguracoesEtiqueta.Add(modelo);
    public void Remover(ConfiguracaoEtiqueta modelo) => db.ConfiguracoesEtiqueta.Remove(modelo);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
