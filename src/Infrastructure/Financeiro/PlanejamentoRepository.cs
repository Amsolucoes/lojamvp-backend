using LojaApi.Application.Financeiro;
using LojaApi.Data;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Financeiro;

// Acesso aos dados do Planejamento (EF Core). Só lê e grava; as regras ficam nos handlers.
public class PlanejamentoRepository(AppDbContext db) : IPlanejamentoRepository
{
    public Task<PlanejamentoFinanceiro?> ObterPlanejamentoAsync(Guid lojaId) => db.PlanejamentosFinanceiros.FirstOrDefaultAsync(p => p.LojaId == lojaId);
    public void Adicionar(PlanejamentoFinanceiro planejamento) => db.PlanejamentosFinanceiros.Add(planejamento);

    public Task<List<Guid>> ListarIdsDeCategoriasDaLojaAsync(Guid lojaId) => db.CategoriasFinanceiras.Where(c => c.LojaId == lojaId).Select(c => c.Id).ToListAsync();
    public Task<List<PlanejamentoCategoria>> ListarTetosAsync(Guid lojaId) => db.PlanejamentoCategorias.Where(p => p.LojaId == lojaId).ToListAsync();
    public void Adicionar(PlanejamentoCategoria teto) => db.PlanejamentoCategorias.Add(teto);

    public Task<MetaPlanejamento?> ObterMetaAsync(Guid id, Guid lojaId) => db.MetasPlanejamento.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);
    public void Adicionar(MetaPlanejamento meta) => db.MetasPlanejamento.Add(meta);
    public void Remover(MetaPlanejamento meta) => db.MetasPlanejamento.Remove(meta);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
