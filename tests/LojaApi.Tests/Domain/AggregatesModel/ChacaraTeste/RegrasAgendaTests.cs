using LojaApi.Domain.Chacara;

namespace LojaApi.Tests.Domain.AggregatesModel.ChacaraTeste;

public class RegrasAgendaTests
{
    private static readonly DateTime Agora = new(2030, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    private static bool Ocupa(string status, DateTime? expiraEm) =>
        new[] { new Reserva { Status = status, ExpiraEm = expiraEm } }
            .AsQueryable().Where(RegrasAgenda.OcupaAAgenda(Agora)).Any();

    [Theory]
    [InlineData(StatusReserva.Confirmada)]
    [InlineData(StatusReserva.ConfirmadaParcial)]
    public void Reserva_confirmada_ocupa_a_agenda(string status) => Assert.True(Ocupa(status, null));

    [Theory]
    [InlineData(StatusReserva.Cancelada)]
    [InlineData(StatusReserva.Expirada)]
    public void Cancelada_ou_expirada_libera_a_data(string status) => Assert.False(Ocupa(status, null));

    [Fact]
    public void Pendente_dentro_do_prazo_ocupa() => Assert.True(Ocupa(StatusReserva.PendentePagamento, Agora.AddMinutes(1)));

    [Fact]
    public void Pendente_com_prazo_vencido_nao_ocupa() => Assert.False(Ocupa(StatusReserva.PendentePagamento, Agora.AddMinutes(-1)));

    [Fact]
    public void Pendente_no_instante_exato_do_prazo_ja_nao_ocupa() => Assert.False(Ocupa(StatusReserva.PendentePagamento, Agora));

    [Fact]
    public void Pendente_sem_prazo_e_reserva_em_negociacao_e_ocupa() => Assert.True(Ocupa(StatusReserva.PendentePagamento, null));

    private static bool Sobrepoe(DateTime resIni, DateTime resFim, DateTime ini, DateTime fim) =>
        new[] { new Reserva { DataInicio = resIni, DataFim = resFim } }
            .AsQueryable().Where(RegrasAgenda.SobrepoeOPeriodo(ini, fim)).Any();

    private static DateTime D(int dia) => new(2030, 6, dia, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(10, 12, 11, 11, true)]   // contido
    [InlineData(10, 12, 8, 14, true)]    // contém
    [InlineData(10, 12, 12, 15, true)]   // encosta no último dia
    [InlineData(10, 12, 8, 10, true)]    // encosta no primeiro dia
    [InlineData(10, 12, 13, 15, false)]  // depois
    [InlineData(10, 12, 7, 9, false)]    // antes
    [InlineData(10, 10, 10, 10, true)]   // mesmo dia único
    public void Sobreposicao_de_periodos(int resIni, int resFim, int ini, int fim, bool esperado) =>
        Assert.Equal(esperado, Sobrepoe(D(resIni), D(resFim), D(ini), D(fim)));
}
