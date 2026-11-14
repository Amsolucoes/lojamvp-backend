using LojaApi.Application.NfImportacao;
using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.NfImportacao;

// Acesso aos dados da importação de nota fiscal (EF Core). Só lê e grava; as regras ficam nos handlers.
public class NfImportacaoRepository(AppDbContext db) : INfImportacaoRepository
{
    // ── Notas ──

    public Task<NfImportada?> ObterNotaAtivaPorChaveAsync(Guid lojaId, string chaveAcesso) =>
        db.NfsImportadas.FirstOrDefaultAsync(n => n.LojaId == lojaId && n.ChaveAcesso == chaveAcesso && !n.Desfeita);

    public Task<NfImportada?> ObterNotaAsync(Guid id, Guid lojaId) =>
        db.NfsImportadas.FirstOrDefaultAsync(n => n.Id == id && n.LojaId == lojaId);

    public Task<bool> NotaDoFornecedorExisteAsync(Guid lojaId, Guid fornecedorId, string numeroNf, Guid? ignorarId) =>
        db.NfsImportadas.AnyAsync(n =>
            n.LojaId == lojaId && n.FornecedorId == fornecedorId && !n.Desfeita
            && (ignorarId == null || n.Id != ignorarId)
            && n.NumeroNf.ToLower() == numeroNf.ToLower());

    public Task<List<NfHistoricoDto>> ListarHistoricoAsync(Guid lojaId) =>
        db.NfsImportadas
            .Where(n => n.LojaId == lojaId)
            .OrderByDescending(n => n.ImportadoEm)
            .Select(n => new NfHistoricoDto(n.Id, n.NumeroNf, n.NomeFornecedor, n.FornecedorId, n.Origem, n.DataEmissao,
                n.ValorTotal, n.ValorCustoTotal, n.ValorVendaTotal, n.QuantidadeTotal, n.QtdItens, n.ImportadoEm, n.Desfeita))
            .ToListAsync();

    public void Adicionar(NfImportada nota) => db.NfsImportadas.Add(nota);

    // ── Fornecedor ──

    public Task<Fornecedor?> ObterFornecedorAsync(Guid id, Guid lojaId) =>
        db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);

    // ── Produtos e variações ──

    public Task<List<Produto>> ListarProdutosAtivosComVariacoesAsync(Guid lojaId) =>
        db.Produtos.Include(p => p.Variacoes).Where(p => p.LojaId == lojaId && p.Ativo).ToListAsync();

    public Task<Produto?> ObterProdutoComVariacoesAsync(Guid id) =>
        db.Produtos.Include(p => p.Variacoes).FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> ProdutoTemVendasAsync(Guid id) => db.ItensVenda.AnyAsync(iv => iv.ProdutoId == id);

    public async Task ExcluirProdutoComDependenciasAsync(Guid id)
    {
        await db.ProdutoVariacoes.Where(v => v.ProdutoId == id).ExecuteDeleteAsync();
        await db.Movimentos.Where(m => m.ProdutoId == id).ExecuteDeleteAsync();
        await db.Produtos.Where(p => p.Id == id).ExecuteDeleteAsync();
    }

    public void Adicionar(Produto produto) => db.Produtos.Add(produto);
    public void Adicionar(ProdutoVariacao variacao) => db.ProdutoVariacoes.Add(variacao);
    public void Remover(ProdutoVariacao variacao) => db.ProdutoVariacoes.Remove(variacao);

    // ── Categorias ──

    public Task<List<CategoriaLoja>> ListarCategoriasAtivasAsync(Guid lojaId) =>
        db.CategoriasLoja.Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();

    public Task<CategoriaLoja?> ObterCategoriaAtivaPorNomeAsync(Guid lojaId, string nome) =>
        db.CategoriasLoja.FirstOrDefaultAsync(c => c.LojaId == lojaId && c.Ativo && c.Nome.ToLower() == nome.ToLower());

    public async Task<CategoriaLoja?> ObterCategoriaAsync(Guid id) => await db.CategoriasLoja.FindAsync(id);

    public async Task<int> ProximaOrdemDeCategoriaAsync(Guid lojaId) =>
        (await db.CategoriasLoja.Where(c => c.LojaId == lojaId).Select(c => (int?)c.Ordem).MaxAsync() ?? -1) + 1;

    public Task<bool> CategoriaTemProdutoAtivoAsync(Guid lojaId, string nomeCategoria) =>
        db.Produtos.AnyAsync(p => p.LojaId == lojaId && p.Categoria == nomeCategoria && p.Ativo);

    public void Adicionar(CategoriaLoja categoria) => db.CategoriasLoja.Add(categoria);

    // ── Mapeamento ──

    public Task<List<NfProdutoMapeamento>> ListarMapeamentosAsync(Guid lojaId, string cnpjFornecedor) =>
        db.NfProdutoMapeamentos.Where(m => m.LojaId == lojaId && m.CnpjFornecedor == cnpjFornecedor).ToListAsync();

    public Task<NfProdutoMapeamento?> ObterMapeamentoAsync(Guid lojaId, string cnpjFornecedor, string codigoFornecedor) =>
        db.NfProdutoMapeamentos.FirstOrDefaultAsync(m =>
            m.LojaId == lojaId && m.CnpjFornecedor == cnpjFornecedor && m.CodigoFornecedor == codigoFornecedor);

    public void Adicionar(NfProdutoMapeamento mapeamento) => db.NfProdutoMapeamentos.Add(mapeamento);

    // ── Movimentos ──

    public Task<List<MovimentoEstoque>> ListarMovimentosPorObservacaoAsync(Guid lojaId, string observacao) =>
        db.Movimentos.Where(m => m.LojaId == lojaId && m.Observacao == observacao).ToListAsync();

    public Task ExcluirMovimentosPorObservacaoAsync(Guid lojaId, string observacao) =>
        db.Movimentos.Where(m => m.LojaId == lojaId && m.Observacao == observacao).ExecuteDeleteAsync();

    public void Adicionar(MovimentoEstoque movimento) => db.Movimentos.Add(movimento);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
