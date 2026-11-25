using LojaApi.Application.Fiscal;
using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models.Fiscal;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Fiscal;

// Implementação EF Core do acesso aos dados da emissão fiscal.
public class FiscalRepository(AppDbContext db) : IFiscalRepository
{
    public Task<ConfiguracaoFiscal?> ObterConfiguracaoAsync(Guid lojaId) =>
        db.ConfiguracoesFiscais.FirstOrDefaultAsync(c => c.LojaId == lojaId);

    public void Adicionar(ConfiguracaoFiscal configuracao) => db.ConfiguracoesFiscais.Add(configuracao);

    public Task<Loja?> ObterLojaAsync(Guid lojaId) => db.Lojas.FindAsync(lojaId).AsTask();

    public Task<Venda?> ObterVendaAsync(Guid vendaId, Guid lojaId) =>
        db.Vendas.FirstOrDefaultAsync(v => v.Id == vendaId && v.LojaId == lojaId);

    public void Adicionar(EmissaoFiscal emissao) => db.EmissoesFiscais.Add(emissao);

    public Task<EmissaoFiscal?> ObterEmissaoAsync(Guid emissaoId, Guid? lojaId) =>
        db.EmissoesFiscais.FirstOrDefaultAsync(e => e.Id == emissaoId && (!lojaId.HasValue || e.LojaId == lojaId));

    public Task<List<EmissaoFiscal>> ListarEmissoesDaVendaAsync(Guid vendaId, Guid? lojaId)
    {
        var q = db.EmissoesFiscais.Where(e => e.VendaId == vendaId);
        if (lojaId.HasValue) q = q.Where(e => e.LojaId == lojaId);
        return q.OrderByDescending(e => e.SolicitadaEm).ToListAsync();
    }

    public Task SalvarAsync() => db.SaveChangesAsync();
}
