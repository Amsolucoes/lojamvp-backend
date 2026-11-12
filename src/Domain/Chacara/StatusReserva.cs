namespace LojaApi.Domain.Chacara;

// Situações possíveis de uma reserva (gravadas como texto no banco).
public static class StatusReserva
{
    public const string PendentePagamento = "pendente_pagamento";
    public const string Confirmada = "confirmada";
    public const string ConfirmadaParcial = "confirmada_parcial";
    public const string Cancelada = "cancelada";
    public const string Expirada = "expirada";
}
