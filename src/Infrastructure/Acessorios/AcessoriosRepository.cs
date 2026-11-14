using LojaApi.Application.Acessorios;
using LojaApi.Data;
using LojaApi.Domain.Acessorios;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Acessorios;

// Acesso aos dados da loja de acessórios (EF Core). Só lê e grava; as regras ficam nos handlers.
public class AcessoriosRepository(AppDbContext db) : IAcessoriosRepository
{
    // ── Pedidos ──

    public Task<List<PedidoAcessorio>> ListarPedidosVencidosAsync(DateTime agora) =>
        db.PedidosAcessorio
            .Include(p => p.Itens)
            .Where(p => p.Status == StatusPedidoAcessorio.AguardandoPagamento && p.ExpiraEm != null && p.ExpiraEm < agora)
            .ToListAsync();

    public Task<PedidoAcessorio?> ObterPedidoAsync(Guid id) =>
        db.PedidosAcessorio.FirstOrDefaultAsync(p => p.Id == id);

    public Task<PedidoAcessorio?> ObterPedidoComItensAsync(Guid id) =>
        db.PedidosAcessorio.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PedidoAcessorio?> ObterPedidoPelaChaveAsync(Guid id) => await db.PedidosAcessorio.FindAsync(id);

    public async Task<List<PedidoAcessorioResumoDto>> ListarPedidosResumoAsync(string? status)
    {
        var q = db.PedidosAcessorio.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);

        // Projeta para um tipo anônimo no banco e monta os records em memória (o EF não ordena/traduz bem construtor de record aninhado)
        var lista = await q.OrderByDescending(p => p.CriadoEm)
            .Select(p => new
            {
                p.Id,
                p.ClienteNome,
                p.ClienteEmail,
                p.ClienteTelefone,
                p.ClienteCpfCnpj,
                p.Cep,
                p.Endereco,
                p.Numero,
                p.Complemento,
                p.Bairro,
                p.Cidade,
                p.Uf,
                p.Subtotal,
                p.ValorFrete,
                p.Total,
                p.Status,
                p.CodigoRastreio,
                p.CriadoEm,
                p.PagoEm,
                p.EnviadoEm,
                Itens = p.Itens.Select(i => new
                {
                    i.NomeProduto,
                    i.Quantidade,
                    i.PrecoUnitario,
                    i.Subtotal,
                }),
            })
            .ToListAsync();

        return lista.Select(p => new PedidoAcessorioResumoDto(
            p.Id, p.ClienteNome, p.ClienteEmail, p.ClienteTelefone, p.ClienteCpfCnpj, p.Cep, p.Endereco, p.Numero, p.Complemento,
            p.Bairro, p.Cidade, p.Uf, p.Subtotal, p.ValorFrete, p.Total, p.Status, p.CodigoRastreio, p.CriadoEm, p.PagoEm, p.EnviadoEm,
            p.Itens.Select(i => new PedidoAcessorioItemResumoDto(i.NomeProduto, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList())).ToList();
    }

    public Task<List<PedidoAcessorio>> ListarPedidosComItensAsync(string? status)
    {
        var q = db.PedidosAcessorio.Include(p => p.Itens).AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);

        return q.OrderByDescending(p => p.CriadoEm).ToListAsync();
    }

    public Task<PedidoAcessorio?> ObterUltimoPedidoPagoComProdutoAsync(string emailEmMinusculas, Guid produtoId) =>
        db.PedidosAcessorio
            .Include(p => p.Itens)
            .Where(p => p.Status == StatusPedidoAcessorio.Pago
                     && p.ClienteEmail.ToLower() == emailEmMinusculas
                     && p.Itens.Any(i => i.ProdutoId == produtoId))
            .OrderByDescending(p => p.PagoEm)
            .FirstOrDefaultAsync();

    public void Adicionar(PedidoAcessorio pedido) => db.PedidosAcessorio.Add(pedido);
    public void Remover(PedidoAcessorio pedido) => db.PedidosAcessorio.Remove(pedido);

    // ── Produtos ──

    public async Task<ProdutoAcessorio?> ObterProdutoAsync(Guid id) => await db.ProdutosAcessorio.FindAsync(id);

    public Task<ProdutoAcessorio?> ObterProdutoAtivoAsync(Guid id) =>
        db.ProdutosAcessorio.FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

    public Task<List<ProdutoAcessorio>> ListarProdutosAsync() =>
        db.ProdutosAcessorio.OrderBy(p => p.Ordem).ThenBy(p => p.Nome).ToListAsync();

    public async Task<List<ProdutoVitrineDto>> ListarVitrineAsync(string? categoria, DateTime limiteNovo)
    {
        var q = db.ProdutosAcessorio.Where(p => p.Ativo);
        if (!string.IsNullOrEmpty(categoria)) q = q.Where(p => p.Categoria == categoria);

        var lista = await q.OrderBy(p => p.Ordem).ThenBy(p => p.Nome)
            .Select(p => new
            {
                p.Id,
                p.Nome,
                p.Descricao,
                p.Preco,
                p.PrecoPromocional,
                p.Categoria,
                p.ImagensUrls,
                Disponivel = p.Estoque > 0,
                p.Destaque,
                Novo = p.CriadoEm >= limiteNovo,
                MediaAvaliacoes = db.AvaliacoesAcessorio.Where(a => a.ProdutoId == p.Id).Average(a => (double?)a.Nota) ?? 0,
                TotalAvaliacoes = db.AvaliacoesAcessorio.Count(a => a.ProdutoId == p.Id),
            })
            .ToListAsync();

        return lista.Select(p => new ProdutoVitrineDto(
            p.Id, p.Nome, p.Descricao, p.Preco, p.PrecoPromocional, p.Categoria, p.ImagensUrls, p.Disponivel, p.Destaque, p.Novo,
            p.MediaAvaliacoes, p.TotalAvaliacoes)).ToList();
    }

    public Task<bool> ProdutoTemPedidosAsync(Guid id) => db.ItensPedidoAcessorio.AnyAsync(i => i.ProdutoId == id);

    public void Adicionar(ProdutoAcessorio produto) => db.ProdutosAcessorio.Add(produto);
    public void Remover(ProdutoAcessorio produto) => db.ProdutosAcessorio.Remove(produto);

    // ── Categorias ──

    public Task<List<CategoriaAcessorio>> ListarCategoriasAtivasAsync() =>
        db.CategoriasAcessorio
            .Where(c => c.Ativa)
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .ToListAsync();

    public async Task<CategoriaAcessorio?> ObterCategoriaAsync(Guid id) => await db.CategoriasAcessorio.FindAsync(id);

    public Task<bool> CategoriaComChaveExisteAsync(string chave) => db.CategoriasAcessorio.AnyAsync(c => c.Chave == chave);

    public async Task<int> MaiorOrdemDeCategoriaAsync() => await db.CategoriasAcessorio.MaxAsync(c => (int?)c.Ordem) ?? -1;

    public Task<bool> CategoriaEmUsoAsync(string chave) => db.ProdutosAcessorio.AnyAsync(p => p.Categoria == chave);

    public void Adicionar(CategoriaAcessorio categoria) => db.CategoriasAcessorio.Add(categoria);
    public void Remover(CategoriaAcessorio categoria) => db.CategoriasAcessorio.Remove(categoria);

    // ── Avaliações ──

    public Task<bool> JaAvaliouAsync(Guid pedidoId, Guid produtoId) =>
        db.AvaliacoesAcessorio.AnyAsync(a => a.PedidoId == pedidoId && a.ProdutoId == produtoId);

    public async Task<List<AvaliacaoPublicaDto>> ListarAvaliacoesAsync(Guid produtoId)
    {
        var lista = await db.AvaliacoesAcessorio
            .Where(a => a.ProdutoId == produtoId)
            .OrderByDescending(a => a.CriadoEm)
            .Select(a => new { a.Id, a.ClienteNome, a.Nota, a.Comentario, a.CriadoEm })
            .ToListAsync();

        return lista.Select(a => new AvaliacaoPublicaDto(a.Id, a.ClienteNome, a.Nota, a.Comentario, a.CriadoEm)).ToList();
    }

    public void Adicionar(AvaliacaoAcessorio avaliacao) => db.AvaliacoesAcessorio.Add(avaliacao);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
