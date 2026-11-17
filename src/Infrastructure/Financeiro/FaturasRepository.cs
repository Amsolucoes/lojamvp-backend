using LojaApi.Application.Financeiro;
using LojaApi.Data;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Financeiro;

// Acesso aos dados das faturas do cartão, dos pagamentos antecipados e das parcelas de financiamento (EF Core).
// Só lê e grava; as regras ficam nos handlers.
public class FaturasRepository(AppDbContext db) : IFaturasRepository
{
    public Task<List<FaturaItemDto>> ListarItensDoCicloAsync(Guid cartaoId, DateTime inicio, DateTime fim) =>
        db.LancamentosCartao
            .Where(l => l.CartaoCreditoId == cartaoId && l.DataCompra.Date >= inicio.Date && l.DataCompra.Date <= fim.Date)
            .OrderBy(l => l.DataCompra)
            .Select(l => new FaturaItemDto(l.Id, l.Descricao, l.Valor, l.DataCompra, l.Categoria != null ? l.Categoria.Nome : null, l.CategoriaId, l.Modo, l.Observacao))
            .ToListAsync();

    public async Task<decimal> SomarComprasDoCicloAsync(Guid cartaoId, DateTime inicio, DateTime fim) =>
        await db.LancamentosCartao
            .Where(l => l.CartaoCreditoId == cartaoId && l.DataCompra.Date >= inicio.Date && l.DataCompra.Date <= fim.Date)
            .SumAsync(l => (decimal?)l.Valor) ?? 0;

    public Task<FaturaCartao?> ObterFaturaDoMesAsync(Guid cartaoId, int ano, int mes) =>
        db.FaturasCartao.FirstOrDefaultAsync(f => f.CartaoCreditoId == cartaoId && f.MesReferencia.Year == ano && f.MesReferencia.Month == mes);

    public Task<FaturaCartao?> ObterFaturaPorReferenciaAsync(Guid cartaoId, DateTime mesReferencia) =>
        db.FaturasCartao.FirstOrDefaultAsync(f => f.CartaoCreditoId == cartaoId && f.MesReferencia == mesReferencia);

    public Task<List<FaturaCartao>> ListarFaturasPorIdsAsync(List<Guid> ids) => db.FaturasCartao.Where(f => ids.Contains(f.Id)).ToListAsync();

    public void Adicionar(FaturaCartao fatura) => db.FaturasCartao.Add(fatura);

    public Task<List<AntecipadoDto>> ListarAntecipadosAsync(Guid faturaId) =>
        db.PagamentosAntecipadosFatura
            .Where(p => p.FaturaCartaoId == faturaId)
            .OrderBy(p => p.Data)
            .Select(p => new AntecipadoDto(p.Id, p.Valor, p.Data, p.ContaBancariaId, p.Observacao))
            .ToListAsync();

    public async Task<decimal> SomarAntecipadosAsync(Guid faturaId) =>
        await db.PagamentosAntecipadosFatura.Where(p => p.FaturaCartaoId == faturaId).SumAsync(p => (decimal?)p.Valor) ?? 0;

    public Task<PagamentoAntecipadoFatura?> ObterAntecipadoAsync(Guid id, Guid lojaId) =>
        db.PagamentosAntecipadosFatura.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);

    public void Adicionar(PagamentoAntecipadoFatura antecipado) => db.PagamentosAntecipadosFatura.Add(antecipado);
    public void Remover(PagamentoAntecipadoFatura antecipado) => db.PagamentosAntecipadosFatura.Remove(antecipado);

    public Task<List<LancamentoFinanceiro>> ListarParcelasDoMesAsync(Guid cartaoId, int ano, int mes) =>
        db.LancamentosFinanceiros
            .Where(l => l.CartaoOrigemId == cartaoId && l.Vencimento.Year == ano && l.Vencimento.Month == mes)
            .OrderBy(l => l.NumeroParcela)
            .ToListAsync();

    public Task<List<LancamentoFinanceiro>> ListarParcelasPendentesDoMesAsync(Guid cartaoId, int ano, int mes) =>
        db.LancamentosFinanceiros
            .Where(l => l.CartaoOrigemId == cartaoId && l.Status == "pendente" && l.Vencimento.Year == ano && l.Vencimento.Month == mes)
            .ToListAsync();

    public Task<List<LancamentoFinanceiro>> ListarParcelasPagasDoMesAsync(Guid cartaoId, int ano, int mes) =>
        db.LancamentosFinanceiros
            .Where(l => l.CartaoOrigemId == cartaoId && l.Status == "pago" && l.Vencimento.Year == ano && l.Vencimento.Month == mes)
            .ToListAsync();

    public Task<List<LancamentoFinanceiro>> ListarParcelasPendentesDaFaturaAsync(Guid faturaId) =>
        db.LancamentosFinanceiros.Where(l => l.FaturaCartaoId == faturaId && l.Status == "pendente").ToListAsync();

    public Task<List<FaturaParcialDto>> ListarFaturasParciaisAsync(Guid lojaId) =>
        db.FaturasCartao.AsNoTracking()
            .Where(f => f.LojaId == lojaId && f.Status == "parcial")
            .Select(f => new FaturaParcialDto(f.CartaoCreditoId, f.MesReferencia, f.PagoEm))
            .ToListAsync();

    public Task<List<LancamentoFinanceiro>> ListarPendentesDeCartoesAsync(Guid lojaId, List<Guid?> cartaoIds) =>
        db.LancamentosFinanceiros.Where(l => l.LojaId == lojaId && l.Status == "pendente" && cartaoIds.Contains(l.CartaoOrigemId)).ToListAsync();

    public Task<List<Guid?>> ListarGruposDeFinanciamentoPendentesAsync(Guid lojaId) =>
        db.LancamentosFinanceiros
            .Where(l => l.LojaId == lojaId && l.CartaoOrigemId != null && l.Status == "pendente" && l.GrupoParcelamentoId != null)
            .Select(l => l.GrupoParcelamentoId)
            .Distinct()
            .ToListAsync();

    public Task<List<LancamentoFinanceiro>> ListarPendentesDosGruposAsync(List<Guid?> grupoIds) =>
        db.LancamentosFinanceiros.Where(l => grupoIds.Contains(l.GrupoParcelamentoId) && l.Status == "pendente").ToListAsync();

    public async Task<Dictionary<Guid, CartaoCredito>> ObterCartoesPorIdsAsync(List<Guid> ids) =>
        (await db.CartoesCredito.Where(c => ids.Contains(c.Id)).ToListAsync()).ToDictionary(c => c.Id);

    public async Task SalvarSeHouverAlteracoesAsync()
    {
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync();
    }

    public Task SalvarAsync() => db.SaveChangesAsync();
}
