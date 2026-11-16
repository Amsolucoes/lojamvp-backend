using LojaApi.Application.Financeiro;
using LojaApi.Data;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Financeiro;

// Acesso aos dados dos cartões, das compras e dos fixos do cartão (EF Core). Só lê e grava; as regras ficam nos handlers.
public class CartoesRepository(AppDbContext db) : ICartoesRepository
{
    public Task<List<CartaoDto>> ListarCartoesAsync(Guid lojaId) =>
        db.CartoesCredito.Where(c => c.LojaId == lojaId)
            .Select(c => new CartaoDto(c.Id, c.Nome, c.Limite, c.DiaFechamento, c.DiaVencimento, c.ContaBancariaId, c.Ativo, c.TaxaJurosMensal, c.Bandeira))
            .ToListAsync();

    public Task<List<CartaoFixoDto>> ListarFixosDoCartaoAsync(Guid cartaoId, Guid lojaId) =>
        db.CartaoLancamentosFixos
            .Where(f => f.CartaoCreditoId == cartaoId && f.CartaoCredito!.LojaId == lojaId)
            .Select(f => new CartaoFixoDto(f.Id, f.Descricao, f.Valor, f.Ativo))
            .ToListAsync();

    public Task<List<string>> ListarDescricoesRecentesAsync(Guid lojaId) =>
        db.LancamentosCartao
            .Where(l => l.LojaId == lojaId)
            .OrderByDescending(l => l.CriadoEm)
            .Select(l => l.Descricao)
            .Distinct()
            .Take(50)
            .ToListAsync();

    public Task<CartaoCredito?> ObterCartaoAsync(Guid id, Guid lojaId) => db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
    public void Adicionar(CartaoCredito cartao) => db.CartoesCredito.Add(cartao);

    public Task<LancamentoCartao?> ObterCompraAsync(Guid id, Guid lojaId) =>
        db.LancamentosCartao.Include(l => l.CartaoCredito).FirstOrDefaultAsync(l => l.Id == id && l.CartaoCredito!.LojaId == lojaId);

    public Task<List<LancamentoCartao>> ListarGrupoDeComprasAsync(Guid grupoId, Guid lojaId) =>
        db.LancamentosCartao.Where(l => l.GrupoParcelamentoId == grupoId && l.LojaId == lojaId).ToListAsync();

    public Task<List<LancamentoCartao>> ListarComprasDoFixoAPartirDeAsync(Guid fixoId, Guid lojaId, DateTime desde) =>
        db.LancamentosCartao.Where(l => l.CartaoFixoId == fixoId && l.LojaId == lojaId && l.DataCompra >= desde).ToListAsync();

    public async Task<HashSet<(int Ano, int Mes)>> ListarMesesDeFaturasPagasAsync(Guid cartaoId) =>
        (await db.FaturasCartao.Where(f => f.CartaoCreditoId == cartaoId && f.Status == "pago").ToListAsync())
            .Select(f => (f.MesReferencia.Year, f.MesReferencia.Month))
            .ToHashSet();

    public void Adicionar(LancamentoCartao compra) => db.LancamentosCartao.Add(compra);
    public void Remover(LancamentoCartao compra) => db.LancamentosCartao.Remove(compra);
    public void Remover(IEnumerable<LancamentoCartao> compras) => db.LancamentosCartao.RemoveRange(compras);

    public Task<CartaoLancamentoFixo?> ObterFixoAsync(Guid id, Guid lojaId) =>
        db.CartaoLancamentosFixos.Include(f => f.CartaoCredito).FirstOrDefaultAsync(f => f.Id == id && f.CartaoCredito!.LojaId == lojaId);

    public Task<CartaoLancamentoFixo?> ObterFixoPorLojaAsync(Guid id, Guid lojaId) =>
        db.CartaoLancamentosFixos.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);

    public void Adicionar(CartaoLancamentoFixo fixo) => db.CartaoLancamentosFixos.Add(fixo);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
