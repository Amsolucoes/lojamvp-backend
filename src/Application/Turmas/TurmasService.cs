using LojaApi.Domain.Turmas;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Turmas;

public record SalvarTurmaRequest(string Nome, int DiaSemana, string Horario, int DuracaoMin, int Capacidade);
public record MatricularRequest(Guid ClienteId);
public record RemarcarRequest(Guid? SessaoDestinoId); // null = só avisa falta, sem remarcar ainda
public record MarcarPresencaRequest(bool Compareceu);
public record SalvarProfissionalRequest(string Nome);
public record DefinirProfissionalRequest(Guid? ProfissionalId);

public record TurmaDaListaDto(Guid Id, string Nome, int DiaSemana, string Horario, int DuracaoMin, int Capacidade, bool Ativa, int QtdAlunos);

// Turmas (aulas recorrentes): cadastro, matrícula, agenda de sessões, falta/remarcação, presença e profissionais.
public class TurmasService(ITurmasRepository repositorio, GeradorSessoesTurmaService gerador)
{
    private const string LojaNaoEncontrada = "Loja não encontrada.";

    // ══════════════════ TURMAS (CRUD) ══════════════════

    public async Task<Resultado<object>> ListarAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var turmas = await repositorio.ListarTurmasDaLojaAsync(lojaId.Value);
        return Resultado.Ok(turmas.Select(t => new TurmaDaListaDto(
            t.Turma.Id, t.Turma.Nome, t.Turma.DiaSemana, t.Turma.Horario.ToString(@"hh\:mm"),
            t.Turma.DuracaoMin, t.Turma.Capacidade, t.Turma.Ativa, t.QtdAlunos)).ToList());
    }

    public async Task<Resultado<object>> CriarAsync(Guid? lojaId, SalvarTurmaRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var turma = new Turma
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            DiaSemana = req.DiaSemana,
            Horario = TimeSpan.Parse(req.Horario),
            DuracaoMin = req.DuracaoMin,
            Capacidade = req.Capacidade,
        };
        repositorio.Adicionar(turma);
        await repositorio.SalvarAsync();

        await gerador.GerarSessoesFuturasAsync();

        return Resultado.Ok(new { turma.Id });
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, Guid id, SalvarTurmaRequest req)
    {
        var turma = await repositorio.ObterTurmaAsync(id, lojaId);
        if (turma is null) return Resultado.NaoEncontrado();

        turma.Nome = req.Nome.Trim();
        turma.DiaSemana = req.DiaSemana;
        turma.Horario = TimeSpan.Parse(req.Horario);
        turma.DuracaoMin = req.DuracaoMin;
        turma.Capacidade = req.Capacidade;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { turma.Id });
    }

    public async Task<Resultado<object>> AlternarAtivaAsync(Guid? lojaId, Guid id)
    {
        var turma = await repositorio.ObterTurmaAsync(id, lojaId);
        if (turma is null) return Resultado.NaoEncontrado();

        turma.Ativa = !turma.Ativa;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { turma.Id, turma.Ativa });
    }

    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, Guid id)
    {
        var turma = await repositorio.ObterTurmaAsync(id, lojaId);
        if (turma is null) return Resultado.NaoEncontrado();

        if (await repositorio.TurmaTemMatriculaAtivaAsync(id))
            return Resultado.Invalido("Não é possível excluir: esta turma tem alunos matriculados. Desative em vez de excluir.");

        repositorio.Remover(turma);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Turma excluída." });
    }

    // ══════════════════ MATRÍCULA ══════════════════

    public async Task<Resultado<object>> ListarAlunosAsync(Guid? lojaId, Guid turmaId)
    {
        var turma = await repositorio.ObterTurmaAsync(turmaId, lojaId);
        if (turma is null) return Resultado.NaoEncontrado();

        return Resultado.Ok(await repositorio.ListarAlunosAsync(turmaId));
    }

    public async Task<Resultado<object>> MatricularAsync(Guid? lojaId, Guid turmaId, MatricularRequest req)
    {
        var turma = await repositorio.ObterTurmaAsync(turmaId, lojaId);
        if (turma is null) return Resultado.NaoEncontrado();

        // O aluno tem que ser cliente desta mesma loja
        if (!await repositorio.ClienteDaLojaExisteAsync(req.ClienteId, lojaId!.Value))
            return Resultado.Invalido("Cliente não encontrado.");

        var erro = TurmasRegras.ValidarMatricula(
            await repositorio.ClienteMatriculadoAsync(turmaId, req.ClienteId),
            await repositorio.ContarMatriculasAtivasAsync(turmaId),
            turma.Capacidade);
        if (erro is not null) return Resultado.Invalido(erro);

        repositorio.Adicionar(new MatriculaTurma { LojaId = lojaId.Value, TurmaId = turmaId, ClienteId = req.ClienteId });
        await repositorio.SalvarAsync();

        await gerador.GerarSessoesFuturasAsync();

        return Resultado.Ok(new { mensagem = "Aluno matriculado." });
    }

    public async Task<Resultado<object>> DesmatricularAsync(Guid? lojaId, Guid matriculaId)
    {
        var matricula = await repositorio.ObterMatriculaAsync(matriculaId, lojaId);
        if (matricula is null) return Resultado.NaoEncontrado();

        matricula.Ativa = false;
        await repositorio.SalvarAsync();

        // Remove as inscrições futuras (aulas que ainda não aconteceram) desse aluno nessa turma
        await repositorio.RemoverInscricoesFixasFuturasAsync(matricula.ClienteId, matricula.TurmaId, DateTime.UtcNow);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { mensagem = "Aluno desmatriculado." });
    }

    // ══════════════════ SESSÕES (agenda semanal) ══════════════════

    public async Task<Resultado<object>> ListarSessoesAsync(Guid? lojaId, DateTime de, DateTime ate)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var (deUtc, ateUtc) = Janela(de, ate);
        var sessoes = await repositorio.ListarSessoesAsync(lojaId.Value, deUtc, ateUtc, apenasAbertas: false);

        var resultado = new List<object>();
        foreach (var s in sessoes)
        {
            var inscricoes = await repositorio.ListarInscricoesDaSessaoAsync(s.Id);

            resultado.Add(new
            {
                s.Id,
                s.TurmaId,
                nomeTurma = s.Turma!.Nome,
                dataHora = s.DataHora,
                s.Status,
                capacidade = s.Turma.Capacidade,
                vagasOcupadas = inscricoes.Count(i => TurmasRegras.OcupaVaga(i.Status, i.RemarcadoParaSessaoId)),
                alunos = inscricoes,
            });
        }

        return Resultado.Ok(resultado);
    }

    // Vagas disponíveis noutras sessões (para remarcar)
    public async Task<Resultado<object>> SessoesComVagaAsync(Guid? lojaId, DateTime de, DateTime ate)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var (deUtc, ateUtc) = Janela(de, ate);
        var sessoes = await repositorio.ListarSessoesAsync(lojaId.Value, deUtc, ateUtc, apenasAbertas: true);

        var resultado = new List<object>();
        foreach (var s in sessoes)
        {
            var vagas = TurmasRegras.VagasLivres(s.Turma!.Capacidade, await repositorio.ContarVagasOcupadasAsync(s.Id));
            if (vagas > 0)
                resultado.Add(new { s.Id, nomeTurma = s.Turma.Nome, dataHora = s.DataHora, vagas });
        }

        return Resultado.Ok(resultado);
    }

    private static (DateTime DeUtc, DateTime AteUtcExclusivo) Janela(DateTime de, DateTime ate) =>
        (DateTime.SpecifyKind(de.Date, DateTimeKind.Utc), DateTime.SpecifyKind(ate.Date, DateTimeKind.Utc).AddDays(1));

    // ══════════════════ FALTA / REMARCAÇÃO ══════════════════

    public async Task<Resultado<object>> MarcarFaltaAvisadaAsync(Guid? lojaId, Guid inscricaoId, RemarcarRequest req)
    {
        var inscricao = await repositorio.ObterInscricaoAsync(inscricaoId, lojaId);
        if (inscricao is null) return Resultado.NaoEncontrado();

        inscricao.Status = StatusInscricao.FaltaAvisada;

        if (req.SessaoDestinoId.HasValue)
        {
            var destino = await repositorio.ObterSessaoComTurmaAsync(req.SessaoDestinoId.Value, lojaId);
            if (destino is null) return Resultado.NaoEncontrado("Sessão de destino não encontrada.");

            var erro = TurmasRegras.ValidarRemarcacao(await repositorio.ContarVagasOcupadasAsync(destino.Id), destino.Turma!.Capacidade);
            if (erro is not null) return Resultado.Invalido(erro);

            inscricao.RemarcadoParaSessaoId = destino.Id;

            repositorio.Adicionar(new InscricaoSessao
            {
                LojaId = lojaId!.Value,
                SessaoTurmaId = destino.Id,
                ClienteId = inscricao.ClienteId,
                Tipo = TipoInscricao.Remarcacao,
                Status = StatusInscricao.Confirmado,
            });
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = req.SessaoDestinoId.HasValue ? "Remarcado com sucesso." : "Falta registrada." });
    }

    // ══════════════════ CHAMADA (presença real) ══════════════════

    public async Task<Resultado<object>> MarcarPresencaAsync(Guid? lojaId, Guid inscricaoId, MarcarPresencaRequest req)
    {
        var inscricao = await repositorio.ObterInscricaoAsync(inscricaoId, lojaId);
        if (inscricao is null) return Resultado.NaoEncontrado();

        inscricao.Status = TurmasRegras.StatusDaPresenca(req.Compareceu);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { inscricao.Id, inscricao.Status });
    }

    // ══════════════════ PROFISSIONAIS ══════════════════

    public async Task<Resultado<object>> ListarProfissionaisAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarProfissionaisAtivosAsync(lojaId.Value));
    }

    public async Task<Resultado<object>> CriarProfissionalAsync(Guid? lojaId, SalvarProfissionalRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var prof = new Profissional { LojaId = lojaId.Value, Nome = req.Nome.Trim() };
        repositorio.Adicionar(prof);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { prof.Id, prof.Nome });
    }

    public async Task<Resultado<object>> AlternarProfissionalAsync(Guid? lojaId, Guid id)
    {
        var prof = await repositorio.ObterProfissionalAsync(id, lojaId);
        if (prof is null) return Resultado.NaoEncontrado();

        prof.Ativo = !prof.Ativo;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { prof.Id, prof.Ativo });
    }

    // Define/troca o profissional de um aluno NESSA sessão
    public async Task<Resultado<object>> DefinirProfissionalAsync(Guid? lojaId, Guid inscricaoId, DefinirProfissionalRequest req)
    {
        var inscricao = await repositorio.ObterInscricaoAsync(inscricaoId, lojaId);
        if (inscricao is null) return Resultado.NaoEncontrado();

        if (req.ProfissionalId.HasValue && !await repositorio.ProfissionalDaLojaExisteAsync(req.ProfissionalId.Value, lojaId!.Value))
            return Resultado.Invalido("Profissional não encontrado.");

        inscricao.ProfissionalId = req.ProfissionalId;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { inscricao.Id, inscricao.ProfissionalId });
    }
}
