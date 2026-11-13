using LojaApi.src.Models;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Turmas;

// Acesso aos dados de turmas, matrículas, sessões e inscrições. Implementado em Infrastructure/Turmas (EF Core).
public interface ITurmasRepository
{
    // ── Turmas ──
    Task<List<TurmaComAlunosDto>> ListarTurmasDaLojaAsync(Guid lojaId);
    Task<Turma?> ObterTurmaAsync(Guid id, Guid? lojaId);
    void Adicionar(Turma turma);
    void Remover(Turma turma);

    // ── Matrículas ──
    Task<bool> TurmaTemMatriculaAtivaAsync(Guid turmaId);
    Task<bool> ClienteMatriculadoAsync(Guid turmaId, Guid clienteId);
    Task<int> ContarMatriculasAtivasAsync(Guid turmaId);
    Task<MatriculaTurma?> ObterMatriculaAsync(Guid id, Guid? lojaId);
    void Adicionar(MatriculaTurma matricula);
    Task<List<AlunoDaTurmaDto>> ListarAlunosAsync(Guid turmaId);
    // Tira as inscrições fixas do aluno nas sessões da turma que ainda não aconteceram
    Task RemoverInscricoesFixasFuturasAsync(Guid clienteId, Guid turmaId, DateTime aPartirDe);

    // ── Sessões e inscrições ──
    Task<List<SessaoTurma>> ListarSessoesAsync(Guid lojaId, DateTime deUtc, DateTime ateUtcExclusivo, bool apenasAbertas);
    Task<List<InscricaoDaSessaoDto>> ListarInscricoesDaSessaoAsync(Guid sessaoId);
    Task<int> ContarVagasOcupadasAsync(Guid sessaoId);
    Task<SessaoTurma?> ObterSessaoComTurmaAsync(Guid id, Guid? lojaId);
    Task<InscricaoSessao?> ObterInscricaoAsync(Guid id, Guid? lojaId);
    void Adicionar(InscricaoSessao inscricao);

    // ── Profissionais ──
    Task<List<ProfissionalDaTurmaDto>> ListarProfissionaisAtivosAsync(Guid lojaId);
    Task<Profissional?> ObterProfissionalAsync(Guid id, Guid? lojaId);
    void Adicionar(Profissional profissional);

    // ── Vínculos que precisam ser da mesma loja ──
    Task<bool> ClienteDaLojaExisteAsync(Guid clienteId, Guid lojaId);
    Task<bool> ProfissionalDaLojaExisteAsync(Guid profissionalId, Guid lojaId);

    // ── Geração de sessões (rotina) ──
    Task<List<Turma>> ListarTurmasAtivasAsync();
    Task<List<MatriculaTurma>> ListarMatriculasAtivasAsync(Guid turmaId);
    Task<SessaoTurma?> ObterSessaoAsync(Guid turmaId, DateTime dataHora);
    void Adicionar(SessaoTurma sessao);
    Task<bool> InscricaoExisteAsync(Guid sessaoId, Guid clienteId);

    Task SalvarAsync();
}
