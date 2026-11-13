namespace LojaApi.Domain.Turmas;

public static class StatusSessao
{
    public const string Aberta = "aberta";
}

public static class StatusInscricao
{
    public const string Confirmado = "confirmado";
    public const string FaltaAvisada = "falta_avisada";
    public const string Compareceu = "compareceu";
    public const string Faltou = "faltou";
}

public static class TipoInscricao
{
    public const string Fixo = "fixo";           // matrícula fixa: entra em todas as sessões da turma
    public const string Remarcacao = "remarcacao";
}

// Regras de turmas, vagas e geração de sessões — sem banco.
public static class TurmasRegras
{
    // Quantas semanas de sessões ficam geradas à frente
    public const int SemanasDeAntecedencia = 6;

    // Próxima data (a partir de hoje) que cai no dia da semana informado (0 = domingo ... 6 = sábado),
    // pulando "semanasAFrente" semanas.
    public static DateTime ProximaDataParaDiaSemana(DateTime hoje, int diaSemana, int semanasAFrente)
    {
        int diasAte = (diaSemana - (int)hoje.DayOfWeek + 7) % 7;
        return hoje.AddDays(diasAte).AddDays(semanasAFrente * 7);
    }

    public static DateTime HorarioDaSessao(DateTime data, TimeSpan horario) =>
        DateTime.SpecifyKind(data, DateTimeKind.Utc) + horario;

    // Quem avisou que vai faltar, ou já foi remarcado para outra sessão, não ocupa vaga
    public static bool OcupaVaga(string statusDaInscricao, Guid? remarcadoParaSessaoId) =>
        statusDaInscricao != StatusInscricao.FaltaAvisada && remarcadoParaSessaoId == null;

    public static int VagasLivres(int capacidade, int ocupadas) => capacidade - ocupadas;

    public static string? ValidarMatricula(bool jaMatriculado, int alunosAtuais, int capacidade)
    {
        if (jaMatriculado) return "Aluno já matriculado nesta turma.";
        if (alunosAtuais >= capacidade) return $"Turma cheia ({capacidade} vaga(s)).";
        return null;
    }

    public static string? ValidarRemarcacao(int ocupadasNoDestino, int capacidadeDoDestino) =>
        ocupadasNoDestino >= capacidadeDoDestino ? "Sessão de destino está sem vagas." : null;

    public static string StatusDaPresenca(bool compareceu) => compareceu ? StatusInscricao.Compareceu : StatusInscricao.Faltou;
}
