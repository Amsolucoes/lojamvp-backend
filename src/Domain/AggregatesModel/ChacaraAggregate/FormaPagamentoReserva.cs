namespace LojaApi.Domain.Chacara;

// Como o cliente escolheu pagar a reserva (gravado como texto no banco).
public static class FormaPagamentoReserva
{
    public const string Pix = "pix";                       // sinal de 50%
    public const string Cartao = "cartao";                 // total no cartão (parcelado)
    public const string Combinado = "combinado";           // metade no Pix e metade no cartão
    public const string SemPagamentoOnline = "sem_pagamento_online"; // o dono confirma por fora

    public static bool Valida(string? forma) => forma is Pix or Cartao or Combinado;
}
