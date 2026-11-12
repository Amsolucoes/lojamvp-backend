using System.Linq.Expressions;

namespace LojaApi.Modules.Chacara.Domain;

// Regras da agenda da chácara, escritas como expressões para serem usadas direto nas consultas ao banco.
public static class RegrasAgenda
{
    // A reserva ocupa a agenda quando está confirmada (total ou parcial) ou pendente ainda dentro do prazo
    // (ou em negociação, que é pendente sem prazo de expiração).
    public static Expression<Func<Reserva, bool>> OcupaAAgenda(DateTime agora) =>
        r => r.Status == StatusReserva.Confirmada
          || r.Status == StatusReserva.ConfirmadaParcial
          || (r.Status == StatusReserva.PendentePagamento && (r.ExpiraEm == null || r.ExpiraEm > agora));

    // O período da reserva encosta no intervalo [inicio, fim]
    public static Expression<Func<Reserva, bool>> SobrepoeOPeriodo(DateTime inicio, DateTime fim) =>
        r => r.DataInicio <= fim && r.DataFim >= inicio;
}
