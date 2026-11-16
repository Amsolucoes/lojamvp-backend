using LojaApi.Application.Financeiro;
using LojaApi.Data;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Financeiro;

// Acesso aos dados do Financeiro (EF Core). Só lê e grava; as regras ficam nos handlers.
public class FinanceiroRepository(AppDbContext db) : IFinanceiroRepository
{
    // ── Contas bancárias ──

    public Task<List<ContaBancaria>> ListarContasAsync(Guid lojaId) => db.ContasBancarias.AsNoTracking().Where(c => c.LojaId == lojaId).ToListAsync();

    public Task<ContaBancaria?> ObterContaAsync(Guid id, Guid lojaId) => db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    // Saldo de todas as contas em poucas consultas agrupadas.
    public async Task<Dictionary<Guid, decimal>> CalcularSaldosAsync(Guid lojaId, List<ContaBancaria> contas)
    {
        var resultado = contas.ToDictionary(c => c.Id, c => c.SaldoInicial);
        if (contas.Count == 0) return resultado;
        var ids = contas.Select(c => c.Id).ToList();

        var lancamentos = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => ids.Contains(l.ContaBancariaId) && l.Status == "pago" && !l.NaoAfetaSaldo)
            .GroupBy(l => new { l.ContaBancariaId, l.Tipo })
            .Select(g => new { g.Key.ContaBancariaId, g.Key.Tipo, Total = g.Sum(x => x.Valor) })
            .ToListAsync();
        foreach (var l in lancamentos)
        {
            if (l.Tipo == "receber") resultado[l.ContaBancariaId] += l.Total;
            else if (l.Tipo == "pagar") resultado[l.ContaBancariaId] -= l.Total;
        }

        var ajustes = await db.AjustesContaBancaria.AsNoTracking()
            .Where(a => ids.Contains(a.ContaBancariaId))
            .GroupBy(a => new { a.ContaBancariaId, a.Tipo })
            .Select(g => new { g.Key.ContaBancariaId, g.Key.Tipo, Total = g.Sum(x => x.Valor) })
            .ToListAsync();
        foreach (var a in ajustes)
        {
            if (a.Tipo == "entrada" || a.Tipo == "ajuste") resultado[a.ContaBancariaId] += a.Total;
            else if (a.Tipo == "saida") resultado[a.ContaBancariaId] -= a.Total;
        }

        // Faturas pagas debitam da conta da fatura (ou, se não houver, da conta padrão do cartão).
        var faturasPagas = await db.FaturasCartao.AsNoTracking()
            .Where(f => f.LojaId == lojaId && !f.NaoAfetaSaldo && (f.Status == "pago" || f.Status == "parcial" || f.Status == "financiada"))
            .Select(f => new { ContaId = f.ContaBancariaId ?? f.CartaoCredito!.ContaBancariaId, f.ValorPago })
            .ToListAsync();
        foreach (var f in faturasPagas)
            if (resultado.ContainsKey(f.ContaId)) resultado[f.ContaId] -= f.ValorPago;

        var antecipados = await db.PagamentosAntecipadosFatura.AsNoTracking()
            .Where(p => ids.Contains(p.ContaBancariaId))
            .GroupBy(p => p.ContaBancariaId)
            .Select(g => new { ContaId = g.Key, Total = g.Sum(x => x.Valor) })
            .ToListAsync();
        foreach (var p in antecipados) resultado[p.ContaId] -= p.Total;

        return resultado;
    }

    public void Adicionar(ContaBancaria conta) => db.ContasBancarias.Add(conta);
    public void Adicionar(AjusteContaBancaria ajuste) => db.AjustesContaBancaria.Add(ajuste);

    // ── Categorias ──

    public Task<List<CategoriaFinanceiraDto>> ListarCategoriasAtivasAsync(Guid lojaId, string? tipo)
    {
        var q = db.CategoriasFinanceiras.Where(c => c.LojaId == lojaId && c.Ativa);
        if (!string.IsNullOrEmpty(tipo))
            q = q.Where(c => c.Tipo == tipo || c.Tipo == "ambos");

        return q.OrderBy(c => c.Nome).Select(c => new CategoriaFinanceiraDto(c.Id, c.Nome, c.Tipo, c.Icone)).ToListAsync();
    }

    public Task<List<CategoriaFinanceira>> ListarCategoriasDaLojaAsync(Guid lojaId) =>
        db.CategoriasFinanceiras.AsNoTracking().Where(c => c.LojaId == lojaId).ToListAsync();

    public Task<CategoriaFinanceira?> ObterCategoriaAsync(Guid id, Guid lojaId) =>
        db.CategoriasFinanceiras.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public async Task<bool> CategoriaEmUsoAsync(Guid id) =>
        await db.LancamentosFinanceiros.AnyAsync(l => l.CategoriaId == id) || await db.LancamentosFixos.AnyAsync(l => l.CategoriaId == id);

    public Task<bool> LojaTemCategoriasAsync(Guid lojaId) => db.CategoriasFinanceiras.AnyAsync(c => c.LojaId == lojaId);

    public void Adicionar(CategoriaFinanceira categoria) => db.CategoriasFinanceiras.Add(categoria);
    public void Remover(CategoriaFinanceira categoria) => db.CategoriasFinanceiras.Remove(categoria);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
