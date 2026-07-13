using LojaApi.Data;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.src.Services;

public class TurmasService(AppDbContext db, ILogger<TurmasService> logger)
{
    private const int SEMANAS_LOTE = 6;

    // ── Job semanal: garante que cada turma ativa tem sessões geradas
    // pras próximas 6 semanas, com inscrição fixa de cada aluno matriculado ──
    public async Task GerarSessoesFuturasAsync()
    {
        var turmas = await db.Turmas.Where(t => t.Ativa).ToListAsync();
        var hoje = DateTime.UtcNow.Date;
        int sessoesGeradas = 0;

        foreach (var turma in turmas)
        {
            var matriculas = await db.MatriculasTurma
                .Where(m => m.TurmaId == turma.Id && m.Ativa)
                .ToListAsync();

            for (int semana = 0; semana < SEMANAS_LOTE; semana++)
            {
                var dataAlvo = ProximaDataParaDiaSemana(hoje, turma.DiaSemana, semana);
                var dataHora = DateTime.SpecifyKind(dataAlvo, DateTimeKind.Utc) + turma.Horario;

                var sessao = await db.SessoesTurma
                    .FirstOrDefaultAsync(s => s.TurmaId == turma.Id && s.DataHora == dataHora);

                if (sessao is null)
                {
                    sessao = new SessaoTurma
                    {
                        LojaId = turma.LojaId,
                        TurmaId = turma.Id,
                        DataHora = dataHora,
                        Status = "aberta",
                    };
                    db.SessoesTurma.Add(sessao);
                    await db.SaveChangesAsync(); // precisa do Id pra vincular inscrições
                    sessoesGeradas++;
                }

                foreach (var matricula in matriculas)
                {
                    var jaInscrito = await db.InscricoesSessao.AnyAsync(i =>
                        i.SessaoTurmaId == sessao.Id && i.ClienteId == matricula.ClienteId);

                    if (!jaInscrito)
                    {
                        db.InscricoesSessao.Add(new InscricaoSessao
                        {
                            LojaId = turma.LojaId,
                            SessaoTurmaId = sessao.Id,
                            ClienteId = matricula.ClienteId,
                            Tipo = "fixo",
                            Status = "confirmado",
                        });
                    }
                }
            }
        }

        await db.SaveChangesAsync();
        if (sessoesGeradas > 0)
            logger.LogInformation("{N} sessão(ões) de turma geradas.", sessoesGeradas);
    }

    // Acha a próxima data (a partir de hoje) que cai no dia da semana informado,
    // pulando "semanasAFrente" semanas.
    private static DateTime ProximaDataParaDiaSemana(DateTime hoje, int diaSemana, int semanasAFrente)
    {
        int diasAte = ((int)diaSemana - (int)hoje.DayOfWeek + 7) % 7;
        var data = hoje.AddDays(diasAte).AddDays(semanasAFrente * 7);
        return data;
    }
}