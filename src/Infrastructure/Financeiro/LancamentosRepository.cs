using LojaApi.Application.Financeiro;
using LojaApi.Data;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Financeiro;

// Acesso aos dados dos lançamentos financeiros (EF Core). Só lê e grava; as regras ficam nos handlers.
public class LancamentosRepository(AppDbContext db) : ILancamentosRepository
{
    public Task<List<LancamentoAReceberDto>> ListarAReceberAsync(Guid lojaId, DateTime? de, DateTime? ate)
    {
        var q = db.LancamentosFinanceiros.Where(l => l.LojaId == lojaId && l.Tipo == "receber");
        if (de.HasValue) q = q.Where(l => l.Vencimento >= de.Value);
        if (ate.HasValue) q = q.Where(l => l.Vencimento <= ate.Value.AddDays(1));

        return q.Select(l => new LancamentoAReceberDto(l.Id, l.Descricao, l.Observacao, l.Categoria != null ? l.Categoria.Nome : null, l.Modo, l.CategoriaId,
            l.ContaBancariaId, l.NumeroParcela, l.TotalParcelas, l.Valor, l.Vencimento, l.Status, l.PagoEm, "avulso")).ToListAsync();
    }

    public Task<List<MensalidadeDePlanoDto>> ListarMensalidadesDePlanosAsync(Guid lojaId, DateTime? de, DateTime? ate)
    {
        var q = db.PagamentosPlano.Where(p => p.LojaId == lojaId);
        if (de.HasValue) q = q.Where(p => p.MesReferencia.Date >= de.Value.Date);
        if (ate.HasValue) q = q.Where(p => p.MesReferencia.Date <= ate.Value.Date);

        return q
            .Join(db.AssinaturasCliente, p => p.AssinaturaId, a => a.Id, (p, a) => new { p, a })
            .Join(db.Clientes, x => x.a.ClienteId, c => c.Id, (x, c) => new MensalidadeDePlanoDto(x.p.Id, "Mensalidade - " + c.Nome, x.p.Valor, x.p.MesReferencia, x.p.Status, x.p.PagoEm, "plano"))
            .ToListAsync();
    }

    public Task<List<LancamentoListadoDto>> ListarPorTipoAsync(Guid lojaId, string tipo, DateTime? inicio, DateTime? fim)
    {
        var q = db.LancamentosFinanceiros.Where(l => l.LojaId == lojaId && l.Tipo == tipo);
        if (inicio.HasValue && fim.HasValue) q = q.Where(l => l.Vencimento >= inicio.Value && l.Vencimento < fim.Value);

        return q.OrderBy(l => l.Vencimento)
            .Select(l => new LancamentoListadoDto(l.Id, l.ContaBancariaId, l.Descricao, l.Categoria != null ? l.Categoria.Nome : null, l.Modo, l.Valor, l.Vencimento, l.Status,
                l.PagoEm, l.NumeroParcela, l.TotalParcelas, "lancamento"))
            .ToListAsync();
    }

    public Task<List<string>> ListarDescricoesRecentesAsync(Guid lojaId, string tipo) =>
        db.LancamentosFinanceiros
            .Where(l => l.LojaId == lojaId && l.Tipo == tipo)
            .OrderByDescending(l => l.CriadoEm)
            .Select(l => l.Descricao)
            .Distinct()
            .Take(50)
            .ToListAsync();

    public Task<LancamentoFinanceiro?> ObterAsync(Guid id, Guid lojaId) =>
        db.LancamentosFinanceiros.FirstOrDefaultAsync(l => l.Id == id && l.LojaId == lojaId);

    public Task<List<LancamentoFinanceiro>> ListarGrupoAsync(Guid grupoId, Guid lojaId) =>
        db.LancamentosFinanceiros
            .Where(l => l.GrupoParcelamentoId == grupoId && l.LojaId == lojaId)
            .OrderBy(l => l.NumeroParcela).ThenBy(l => l.Vencimento)
            .ToListAsync();

    public Task<List<LancamentoFinanceiro>> ListarPendentesParaPagarEmLoteAsync(List<Guid> ids, Guid lojaId) =>
        db.LancamentosFinanceiros
            .Where(l => ids.Contains(l.Id) && l.LojaId == lojaId && l.Tipo == "pagar" && l.Status == "pendente" && l.CartaoOrigemId == null)
            .ToListAsync();

    public Task<List<LancamentoFinanceiro>> ListarPendentesDoGrupoAsync(Guid grupoId, Guid lojaId, DateTime? aPartirDe)
    {
        var q = db.LancamentosFinanceiros.Where(l => l.GrupoParcelamentoId == grupoId && l.LojaId == lojaId && l.Status == "pendente");
        if (aPartirDe.HasValue) q = q.Where(l => l.Vencimento >= aPartirDe.Value);
        return q.ToListAsync();
    }

    public Task<List<LancamentoFinanceiro>> ListarPendentesDoFixoAsync(Guid fixoId, Guid lojaId, DateTime aPartirDe) =>
        db.LancamentosFinanceiros.Where(l => l.LancamentoFixoId == fixoId && l.LojaId == lojaId && l.Status == "pendente" && l.Vencimento >= aPartirDe).ToListAsync();

    public Task<LancamentoFinanceiro?> ObterPrimeiraOcorrenciaDoFixoAsync(Guid fixoId) =>
        db.LancamentosFinanceiros.Where(l => l.LancamentoFixoId == fixoId).OrderBy(l => l.Vencimento).FirstOrDefaultAsync();

    public Task<List<LancamentoFinanceiro>> ListarTodosDoFixoAsync(Guid fixoId, Guid lojaId) =>
        db.LancamentosFinanceiros.Where(l => l.LojaId == lojaId && l.LancamentoFixoId == fixoId).ToListAsync();

    public void Adicionar(LancamentoFinanceiro lancamento) => db.LancamentosFinanceiros.Add(lancamento);
    public void Adicionar(IEnumerable<LancamentoFinanceiro> lancamentos) => db.LancamentosFinanceiros.AddRange(lancamentos);
    public void Remover(LancamentoFinanceiro lancamento) => db.LancamentosFinanceiros.Remove(lancamento);
    public void Remover(IEnumerable<LancamentoFinanceiro> lancamentos) => db.LancamentosFinanceiros.RemoveRange(lancamentos);

    public Task<LancamentoFixo?> ObterFixoAsync(Guid id, Guid lojaId) => db.LancamentosFixos.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
    public void Adicionar(LancamentoFixo fixo) => db.LancamentosFixos.Add(fixo);

    public Task<CartaoCredito?> ObterCartaoAsync(Guid id, Guid lojaId) => db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
    public void Adicionar(LancamentoCartao compra) => db.LancamentosCartao.Add(compra);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
