using LojaApi.Application.Turmas;
using LojaApi.Data;
using LojaApi.Domain.Turmas;
using LojaApi.src.Models;
using LojaApi.src.Models.Funcionarios;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Turmas;

// Implementação EF Core do acesso aos dados de turmas.
public class TurmasRepository(AppDbContext db) : ITurmasRepository
{
    // ── Turmas ──

    public async Task<List<TurmaComAlunos>> ListarTurmasDaLojaAsync(Guid lojaId)
    {
        var turmas = await db.Turmas.Where(t => t.LojaId == lojaId).ToListAsync();
        var resultado = new List<TurmaComAlunos>();
        foreach (var t in turmas)
            resultado.Add(new TurmaComAlunos(t, await ContarMatriculasAtivasAsync(t.Id)));
        return resultado;
    }

    public Task<Turma?> ObterTurmaAsync(Guid id, Guid? lojaId) =>
        db.Turmas.FirstOrDefaultAsync(t => t.Id == id && t.LojaId == lojaId);

    public void Adicionar(Turma turma) => db.Turmas.Add(turma);
    public void Remover(Turma turma) => db.Turmas.Remove(turma);

    // ── Matrículas ──

    public Task<bool> TurmaTemMatriculaAtivaAsync(Guid turmaId) =>
        db.MatriculasTurma.AnyAsync(m => m.TurmaId == turmaId && m.Ativa);

    public Task<bool> ClienteMatriculadoAsync(Guid turmaId, Guid clienteId) =>
        db.MatriculasTurma.AnyAsync(m => m.TurmaId == turmaId && m.ClienteId == clienteId && m.Ativa);

    public Task<int> ContarMatriculasAtivasAsync(Guid turmaId) =>
        db.MatriculasTurma.CountAsync(m => m.TurmaId == turmaId && m.Ativa);

    public Task<MatriculaTurma?> ObterMatriculaAsync(Guid id, Guid? lojaId) =>
        db.MatriculasTurma.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);

    public void Adicionar(MatriculaTurma matricula) => db.MatriculasTurma.Add(matricula);

    public Task<List<AlunoDaTurmaDto>> ListarAlunosAsync(Guid turmaId) =>
        db.MatriculasTurma
            .Where(m => m.TurmaId == turmaId && m.Ativa)
            .Join(db.Clientes, m => m.ClienteId, c => c.Id, (m, c) => new AlunoDaTurmaDto(m.Id, m.ClienteId, c.Nome, c.Telefone))
            .ToListAsync();

    public async Task RemoverInscricoesFixasFuturasAsync(Guid clienteId, Guid turmaId, DateTime aPartirDe)
    {
        var futuras = await db.InscricoesSessao
            .Where(i => i.ClienteId == clienteId && i.Tipo == TipoInscricao.Fixo)
            .Join(db.SessoesTurma.Where(s => s.TurmaId == turmaId && s.DataHora >= aPartirDe),
                  i => i.SessaoTurmaId, s => s.Id, (i, s) => i)
            .ToListAsync();
        db.InscricoesSessao.RemoveRange(futuras);
    }

    // ── Sessões e inscrições ──

    public Task<List<SessaoTurma>> ListarSessoesAsync(Guid lojaId, DateTime deUtc, DateTime ateUtcExclusivo, bool apenasAbertas)
    {
        var q = db.SessoesTurma.Include(s => s.Turma)
            .Where(s => s.LojaId == lojaId && s.DataHora >= deUtc && s.DataHora < ateUtcExclusivo);
        if (apenasAbertas) q = q.Where(s => s.Status == StatusSessao.Aberta);
        return q.OrderBy(s => s.DataHora).ToListAsync();
    }

    // Quem faltou na chamada some da lista da sessão
    public Task<List<InscricaoDaSessaoDto>> ListarInscricoesDaSessaoAsync(Guid sessaoId) =>
        db.InscricoesSessao
            .Include(i => i.Profissional)
            .Where(i => i.SessaoTurmaId == sessaoId && i.Status != StatusInscricao.Faltou)
            .Join(db.Clientes, i => i.ClienteId, c => c.Id, (i, c) => new InscricaoDaSessaoDto(
                i.Id, i.ClienteId, c.Nome, i.Tipo, i.Status, i.RemarcadoParaSessaoId,
                i.ProfissionalId, i.Profissional != null ? i.Profissional.Nome : null))
            .ToListAsync();

    public Task<int> ContarVagasOcupadasAsync(Guid sessaoId) =>
        db.InscricoesSessao.CountAsync(i =>
            i.SessaoTurmaId == sessaoId && i.Status != StatusInscricao.FaltaAvisada && i.RemarcadoParaSessaoId == null);

    public Task<SessaoTurma?> ObterSessaoComTurmaAsync(Guid id, Guid? lojaId) =>
        db.SessoesTurma.Include(s => s.Turma).FirstOrDefaultAsync(s => s.Id == id && s.LojaId == lojaId);

    public Task<InscricaoSessao?> ObterInscricaoAsync(Guid id, Guid? lojaId) =>
        db.InscricoesSessao.FirstOrDefaultAsync(i => i.Id == id && i.LojaId == lojaId);

    public void Adicionar(InscricaoSessao inscricao) => db.InscricoesSessao.Add(inscricao);

    // ── Profissionais ──

    public Task<List<ProfissionalDaTurmaDto>> ListarProfissionaisAtivosAsync(Guid lojaId) =>
        db.Profissionais
            .Where(p => p.LojaId == lojaId && p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new ProfissionalDaTurmaDto(p.Id, p.Nome))
            .ToListAsync();

    public Task<Profissional?> ObterProfissionalAsync(Guid id, Guid? lojaId) =>
        db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);

    public void Adicionar(Profissional profissional) => db.Profissionais.Add(profissional);

    // ── Vínculos da mesma loja ──

    public Task<bool> ClienteDaLojaExisteAsync(Guid clienteId, Guid lojaId) =>
        db.Clientes.AnyAsync(c => c.Id == clienteId && c.LojaId == lojaId);

    public Task<bool> ProfissionalDaLojaExisteAsync(Guid profissionalId, Guid lojaId) =>
        db.Profissionais.AnyAsync(p => p.Id == profissionalId && p.LojaId == lojaId);

    // ── Geração de sessões ──

    public Task<List<Turma>> ListarTurmasAtivasAsync() => db.Turmas.Where(t => t.Ativa).ToListAsync();

    public Task<List<MatriculaTurma>> ListarMatriculasAtivasAsync(Guid turmaId) =>
        db.MatriculasTurma.Where(m => m.TurmaId == turmaId && m.Ativa).ToListAsync();

    public Task<SessaoTurma?> ObterSessaoAsync(Guid turmaId, DateTime dataHora) =>
        db.SessoesTurma.FirstOrDefaultAsync(s => s.TurmaId == turmaId && s.DataHora == dataHora);

    public void Adicionar(SessaoTurma sessao) => db.SessoesTurma.Add(sessao);

    public Task<bool> InscricaoExisteAsync(Guid sessaoId, Guid clienteId) =>
        db.InscricoesSessao.AnyAsync(i => i.SessaoTurmaId == sessaoId && i.ClienteId == clienteId);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
