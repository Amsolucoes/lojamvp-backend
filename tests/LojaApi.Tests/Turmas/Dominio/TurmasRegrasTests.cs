using LojaApi.Domain.Turmas;

namespace LojaApi.Tests.Turmas.Dominio;

public class TurmasRegrasTests
{
    // 2030-06-12 é uma quarta-feira
    private static readonly DateTime Quarta = new(2030, 6, 12, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(3, 0, "2030-06-12")]    // hoje mesmo
    [InlineData(4, 0, "2030-06-13")]
    [InlineData(1, 0, "2030-06-17")]    // segunda: dá a volta na semana
    [InlineData(0, 0, "2030-06-16")]    // domingo
    [InlineData(3, 1, "2030-06-19")]    // mesma quarta, uma semana depois
    [InlineData(1, 5, "2030-07-22")]
    public void Proxima_data_do_dia_da_semana(int diaSemana, int semanasAFrente, string esperado) =>
        Assert.Equal(DateTime.Parse(esperado), TurmasRegras.ProximaDataParaDiaSemana(Quarta, diaSemana, semanasAFrente).Date);

    [Fact]
    public void Horario_da_sessao_soma_o_horario_da_turma_em_utc()
    {
        var h = TurmasRegras.HorarioDaSessao(new DateTime(2030, 6, 12), new TimeSpan(8, 30, 0));
        Assert.Equal(new DateTime(2030, 6, 12, 8, 30, 0, DateTimeKind.Utc), h);
        Assert.Equal(DateTimeKind.Utc, h.Kind);
    }

    [Theory]
    [InlineData("confirmado", false, true)]
    [InlineData("compareceu", false, true)]
    [InlineData("falta_avisada", false, false)]   // avisou que falta: libera a vaga
    [InlineData("confirmado", true, false)]       // foi remarcado para outra sessão
    public void Quem_ocupa_vaga(string status, bool remarcado, bool esperado) =>
        Assert.Equal(esperado, TurmasRegras.OcupaVaga(status, remarcado ? Guid.NewGuid() : null));

    [Fact]
    public void Vagas_livres_e_capacidade()
    {
        Assert.Equal(3, TurmasRegras.VagasLivres(8, 5));
        Assert.Equal(0, TurmasRegras.VagasLivres(2, 2));
        Assert.Equal(-1, TurmasRegras.VagasLivres(2, 3));
    }

    [Fact]
    public void Matricula_recusa_repetido_e_turma_cheia()
    {
        Assert.Null(TurmasRegras.ValidarMatricula(false, 1, 2));
        Assert.Equal("Aluno já matriculado nesta turma.", TurmasRegras.ValidarMatricula(true, 0, 8));
        Assert.Equal("Turma cheia (2 vaga(s)).", TurmasRegras.ValidarMatricula(false, 2, 2));
        Assert.Equal("Aluno já matriculado nesta turma.", TurmasRegras.ValidarMatricula(true, 8, 8));   // repetido tem prioridade sobre cheia
    }

    [Fact]
    public void Remarcacao_recusa_destino_lotado()
    {
        Assert.Null(TurmasRegras.ValidarRemarcacao(2, 3));
        Assert.Equal("Sessão de destino está sem vagas.", TurmasRegras.ValidarRemarcacao(3, 3));
    }

    [Fact]
    public void Status_da_presenca()
    {
        Assert.Equal("compareceu", TurmasRegras.StatusDaPresenca(true));
        Assert.Equal("faltou", TurmasRegras.StatusDaPresenca(false));
    }

    [Fact]
    public void Gera_sessoes_para_6_semanas() => Assert.Equal(6, TurmasRegras.SemanasDeAntecedencia);
}
