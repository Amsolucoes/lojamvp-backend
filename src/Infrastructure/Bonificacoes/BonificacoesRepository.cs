using LojaApi.Application.Bonificacoes;
using LojaApi.Data;
using LojaApi.Domain.Bonificacoes;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Bonificacoes;

// Acesso aos dados das bonificações (EF Core). Só lê e grava; as regras ficam nos handlers.
public class BonificacoesRepository(AppDbContext db) : IBonificacoesRepository
{
    public Task<string?> ObterNomeDoClienteAsync(Guid clienteId, Guid lojaId) =>
        db.Clientes.Where(c => c.Id == clienteId && c.LojaId == lojaId).Select(c => c.Nome).FirstOrDefaultAsync();

    public Task<Bonificacao?> ObterAsync(Guid id, Guid lojaId) => db.Bonificacoes.FirstOrDefaultAsync(b => b.Id == id && b.LojaId == lojaId);

    public Task<List<Bonificacao>> ListarAsync(Guid lojaId, Guid? clienteId, Guid? produtoId, DateTime? de, DateTime? ate, bool incluirEstornadas)
    {
        var q = db.Bonificacoes.AsNoTracking().Where(b => b.LojaId == lojaId);
        if (clienteId.HasValue) q = q.Where(b => b.ClienteId == clienteId);
        if (produtoId.HasValue) q = q.Where(b => b.ProdutoId == produtoId);
        if (de.HasValue)
        {
            var inicio = BonificacoesRegras.EmUtc(de.Value);
            q = q.Where(b => b.CriadoEm >= inicio);
        }
        if (ate.HasValue)
        {
            var fimExclusivo = BonificacoesRegras.EmUtc(ate.Value.Date.AddDays(1));   // o dia "ate" entra inteiro
            q = q.Where(b => b.CriadoEm < fimExclusivo);
        }
        if (!incluirEstornadas) q = q.Where(b => b.EstornadaEm == null);
        return q.OrderByDescending(b => b.CriadoEm).ToListAsync();
    }

    public void Adicionar(Bonificacao bonificacao) => db.Bonificacoes.Add(bonificacao);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
