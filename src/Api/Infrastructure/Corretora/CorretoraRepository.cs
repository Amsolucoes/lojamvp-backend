using LojaApi.Application.Corretora;
using LojaApi.Data;
using LojaApi.Domain.Corretora;
using LojaApi.Models;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Corretora;

// Implementação EF Core do acesso aos dados da corretora.
public class CorretoraRepository(AppDbContext db) : ICorretoraRepository
{
    // ── Seguradoras ──

    public Task<List<SeguradoraDto>> ListarSeguradorasAsync(Guid lojaId, bool apenasAtivas)
    {
        var q = db.Seguradoras.Where(s => s.LojaId == lojaId);
        if (apenasAtivas) q = q.Where(s => s.Ativa);
        return q.OrderBy(s => s.Nome).Select(s => new SeguradoraDto(s.Id, s.Nome, s.Ativa)).ToListAsync();
    }

    public Task<Seguradora?> ObterSeguradoraAsync(Guid id, Guid? lojaId) =>
        db.Seguradoras.FirstOrDefaultAsync(s => s.Id == id && s.LojaId == lojaId);

    public Task<bool> SeguradoraDaLojaExisteAsync(Guid id, Guid lojaId) =>
        db.Seguradoras.AnyAsync(s => s.Id == id && s.LojaId == lojaId);

    public async Task<bool> SeguradoraEmUsoAsync(Guid id) =>
        await db.Oportunidades.AnyAsync(o => o.SeguradoraId == id) || await db.Apolices.AnyAsync(a => a.SeguradoraId == id);

    public void Adicionar(Seguradora seguradora) => db.Seguradoras.Add(seguradora);
    public void Remover(Seguradora seguradora) => db.Seguradoras.Remove(seguradora);

    // ── Funil ──

    public async Task<List<OportunidadeDto>> ListarOportunidadesAsync(Guid lojaId)
    {
        // Projeção anônima + ordenação no banco; o record é montado em memória (o EF não ordena por membro de construtor)
        var linhas = await db.Oportunidades
            .Where(o => o.LojaId == lojaId)
            .Include(o => o.Seguradora)
            .Join(db.Clientes, o => o.ClienteId, c => c.Id, (o, c) => new
            {
                o.Id, o.ClienteId, ClienteNome = c.Nome, ClienteTelefone = c.Telefone,
                SeguradoraNome = o.Seguradora != null ? o.Seguradora.Nome : null,
                o.PlanoDesejado, o.ValorEstimado, o.Etapa, o.Ordem, o.Observacao, o.QuantidadeVidas, o.CriadoEm,
            })
            .OrderBy(x => x.Ordem)
            .ToListAsync();

        return linhas.Select(x => new OportunidadeDto(x.Id, x.ClienteId, x.ClienteNome, x.ClienteTelefone, x.SeguradoraNome,
            x.PlanoDesejado, x.ValorEstimado, x.Etapa, x.Ordem, x.Observacao, x.QuantidadeVidas, x.CriadoEm)).ToList();
    }

    public async Task<int> MaiorOrdemNaEtapaAsync(Guid lojaId, string etapa) =>
        await db.Oportunidades
            .Where(o => o.LojaId == lojaId && o.Etapa == etapa)
            .Select(o => (int?)o.Ordem)
            .MaxAsync() ?? 0;

    public Task<Oportunidade?> ObterOportunidadeAsync(Guid id, Guid? lojaId) =>
        db.Oportunidades.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);

    public Task<List<OportunidadeAbertaDto>> ListarOportunidadesEmAbertoDoClienteAsync(Guid lojaId, Guid clienteId) =>
        db.Oportunidades
            .Where(o => o.LojaId == lojaId && o.ClienteId == clienteId && o.Etapa != EtapaOportunidade.Ganho && o.Etapa != EtapaOportunidade.Perdido)
            .Select(o => new OportunidadeAbertaDto(o.Id, o.Etapa, o.PlanoDesejado))
            .ToListAsync();

    public Task<List<Oportunidade>> ListarOportunidadesCriadasDesdeAsync(Guid lojaId, DateTime desde) =>
        db.Oportunidades.Where(o => o.LojaId == lojaId && o.CriadoEm >= desde).ToListAsync();

    public Task<List<FunilEtapaDto>> ContarOportunidadesPorEtapaAsync(Guid lojaId) =>
        db.Oportunidades
            .Where(o => o.LojaId == lojaId)
            .GroupBy(o => o.Etapa)
            .Select(g => new FunilEtapaDto(g.Key, g.Count()))
            .ToListAsync();

    public void Adicionar(Oportunidade oportunidade) => db.Oportunidades.Add(oportunidade);
    public void Remover(Oportunidade oportunidade) => db.Oportunidades.Remove(oportunidade);

    // ── Apólices ──

    public async Task<List<ApoliceDto>> ListarApolicesAsync(Guid lojaId, DateTime hoje)
    {
        var linhas = await db.Apolices
            .Where(a => a.LojaId == lojaId)
            .Include(a => a.Seguradora)
            .Join(db.Clientes, a => a.ClienteId, c => c.Id, (a, c) => new
            {
                a.Id, a.ClienteId, ClienteNome = c.Nome, ClienteTelefone = c.Telefone, SeguradoraNome = a.Seguradora!.Nome,
                a.NomePlano, a.NumeroApolice, a.ValorPremio, a.ValorComissao, a.PercentualComissao,
                a.VigenciaInicio, a.VigenciaFim, a.Status,
                DiasParaVencer = (a.VigenciaFim.Date - hoje).Days,
            })
            .OrderBy(x => x.VigenciaFim)
            .ToListAsync();

        return linhas.Select(x => new ApoliceDto(x.Id, x.ClienteId, x.ClienteNome, x.ClienteTelefone, x.SeguradoraNome,
            x.NomePlano, x.NumeroApolice, x.ValorPremio, x.ValorComissao, x.PercentualComissao,
            x.VigenciaInicio, x.VigenciaFim, x.Status, x.DiasParaVencer)).ToList();
    }

    public Task<List<Apolice>> ListarApolicesNaoCanceladasAsync(Guid lojaId) =>
        db.Apolices.Where(a => a.LojaId == lojaId && a.Status != StatusApolice.Cancelada).ToListAsync();

    public Task<Apolice?> ObterApoliceAsync(Guid id, Guid? lojaId) =>
        db.Apolices.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);

    public void Adicionar(Apolice apolice) => db.Apolices.Add(apolice);

    // ── Vínculos da mesma loja ──

    public Task<Cliente?> ObterClienteDaLojaAsync(Guid clienteId, Guid lojaId) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId && c.LojaId == lojaId);

    public Task<bool> ContaBancariaDaLojaExisteAsync(Guid contaId, Guid lojaId) =>
        db.ContasBancarias.AnyAsync(c => c.Id == contaId && c.LojaId == lojaId);

    public void Adicionar(LancamentoFinanceiro lancamento) => db.LancamentosFinanceiros.Add(lancamento);
    public Task SalvarAsync() => db.SaveChangesAsync();
}
