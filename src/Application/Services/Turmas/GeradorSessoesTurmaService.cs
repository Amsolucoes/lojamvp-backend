using LojaApi.Domain.Turmas;
using LojaApi.src.Models;
using Microsoft.Extensions.Logging;

namespace LojaApi.Application.Turmas;

// Rotina que garante que cada turma ativa tem as sessões das próximas semanas geradas,
// com a inscrição fixa de cada aluno matriculado. Roda todo dia e também ao criar turma/matricular.
public class GeradorSessoesTurmaService(ITurmasRepository repositorio, ILogger<GeradorSessoesTurmaService> logger)
{
    public async Task GerarSessoesFuturasAsync()
    {
        var turmas = await repositorio.ListarTurmasAtivasAsync();
        var hoje = DateTime.UtcNow.Date;
        int sessoesGeradas = 0;

        foreach (var turma in turmas)
        {
            var matriculas = await repositorio.ListarMatriculasAtivasAsync(turma.Id);

            for (int semana = 0; semana < TurmasRegras.SemanasDeAntecedencia; semana++)
            {
                var dataAlvo = TurmasRegras.ProximaDataParaDiaSemana(hoje, turma.DiaSemana, semana);
                var dataHora = TurmasRegras.HorarioDaSessao(dataAlvo, turma.Horario);

                var sessao = await repositorio.ObterSessaoAsync(turma.Id, dataHora);

                if (sessao is null)
                {
                    sessao = new SessaoTurma
                    {
                        LojaId = turma.LojaId,
                        TurmaId = turma.Id,
                        DataHora = dataHora,
                        Status = StatusSessao.Aberta,
                    };
                    repositorio.Adicionar(sessao);
                    await repositorio.SalvarAsync();
                    sessoesGeradas++;
                }

                foreach (var matricula in matriculas)
                {
                    if (!await repositorio.InscricaoExisteAsync(sessao.Id, matricula.ClienteId))
                    {
                        repositorio.Adicionar(new InscricaoSessao
                        {
                            LojaId = turma.LojaId,
                            SessaoTurmaId = sessao.Id,
                            ClienteId = matricula.ClienteId,
                            Tipo = TipoInscricao.Fixo,
                            Status = StatusInscricao.Confirmado,
                        });
                    }
                }
            }
        }

        await repositorio.SalvarAsync();
        if (sessoesGeradas > 0)
            logger.LogInformation("{N} sessão(ões) de turma geradas.", sessoesGeradas);
    }
}
