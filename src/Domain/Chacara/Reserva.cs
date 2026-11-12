using System.Text.Json.Serialization;
using LojaApi.Models;

namespace LojaApi.Domain.Chacara;

public class Reserva
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public DateTime DataInicio { get; set; } // Utc, meio-dia, seguindo o padrao do projeto
    public DateTime DataFim { get; set; }

    public string? HoraEntrada { get; set; } // "HH:mm" — horário escolhido no dia de DataInicio
    public string? HoraSaida { get; set; }   // "HH:mm" — horário escolhido no dia seguinte a DataFim

    public int Pessoas { get; set; }

    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public string ClienteTelefone { get; set; } = string.Empty;
    public string? ClienteDocumento { get; set; } // CPF, usado no contrato
    public string? ClienteCep { get; set; }
    public string? ClienteEndereco { get; set; } // logradouro + bairro
    public string? ClienteNumero { get; set; }
    public string? ClienteCidade { get; set; } // cidade - UF
    public DateTime? DataConfirmacao { get; set; } // quando a reserva virou "confirmada" — usado no contrato como data da entrada

    public decimal Valor { get; set; }
    public decimal ValorPago { get; set; } = 0;
    public string Status { get; set; } = "pendente_pagamento";
    // valores: pendente_pagamento | confirmada | cancelada | expirada

    public string? MpPreferenceId { get; set; }
    public string? MpPaymentId { get; set; } // pagamento via Pix (sinal ou combinado)
    public string? MpStatusPix { get; set; }
    public string? MpPaymentIdCartao { get; set; } // pagamento via cartão (total ou parte do combinado)
    public string? MpStatusCartao { get; set; }
    public string? FormaPagamento { get; set; } // pix | cartao | combinado

    public DateTime? ContratoEnviadoEm { get; set; }
    public bool AvisoCheckoutEnviado { get; set; }
    public bool AvisoExpiracaoEnviado { get; set; } // aviso de 24h para reservas sem pagamento online (evita repetir)

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiraEm { get; set; } // usada pra liberar reservas pendentes nao pagas
    public decimal? ValorPrejuizo { get; set; } // registro interno, não gera cobrança automática
    public string? ObservacaoPrejuizo { get; set; }
    public int? NotaCliente { get; set; } // 1 a 5 — sua avaliação sobre o cliente
    public string? ComentarioCliente { get; set; }
    public bool AvisoAvaliacaoEnviado { get; set; } // controla o lembrete por e-mail (evita mandar 2x)
    public string? MotivoCancelamento { get; set; } // preenchido quando o próprio cliente cancela

    // Ignorado no JSON: se algum outro código no mesmo request rastrear uma Loja completa
    // (ex.: reenvio de contrato), o EF religa essa navegação automaticamente e a serialização
    // entra em ciclo (Loja -> Usuarios -> Loja -> ...). A API nunca precisou desse campo.
    [JsonIgnore]
    public Loja? Loja { get; set; }

    // ── Comportamento da reserva (regras de negócio) ─────────────────────────────
    // Os métodos abaixo devolvem a mensagem de erro quando a regra não permite a operação, ou null quando deu certo.
    // (São métodos, e não propriedades, para não aparecerem no JSON que a API devolve.)

    private static string? Limpar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    // Reserva feita por fora (combinada direto com o cliente): entra já confirmada, sem e-mail ou contrato automático.
    public static Reserva CriarManual(
        Guid lojaId, DateTime inicio, DateTime fim, int pessoas,
        string clienteNome, string? clienteEmail, string? clienteTelefone,
        string? documento, string? cep, string? endereco, string? numero, string? cidade,
        string? horaEntrada, string? horaSaida, decimal valor, decimal? valorPago, DateTime agora) => new()
    {
        LojaId = lojaId,
        DataInicio = DiaDeReserva.MeioDiaUtc(inicio),
        DataFim = DiaDeReserva.MeioDiaUtc(fim),
        Pessoas = pessoas,
        ClienteNome = clienteNome.Trim(),
        ClienteEmail = clienteEmail?.Trim() ?? "",
        ClienteTelefone = new string((clienteTelefone ?? "").Where(char.IsDigit).ToArray()),
        ClienteDocumento = Limpar(documento),
        ClienteCep = Limpar(cep),
        ClienteEndereco = Limpar(endereco),
        ClienteNumero = Limpar(numero),
        ClienteCidade = Limpar(cidade),
        HoraEntrada = Limpar(horaEntrada),
        HoraSaida = Limpar(horaSaida),
        Valor = valor,
        ValorPago = valorPago ?? valor, // sem informar, assume que já foi pago integralmente
        Status = StatusReserva.Confirmada,
        DataConfirmacao = agora,
    };

    public decimal SaldoPendente() => Valor - ValorPago;

    // ── Reserva feita pelo cliente no site público ───────────────────────────────

    // Nasce pendente de pagamento. Com pagamento online, segura a data por 15 minutos enquanto o cliente paga;
    // sem pagamento online, fica aguardando a confirmação do dono por até 48h.
    public static Reserva CriarPeloSite(
        Guid lojaId, DateTime inicio, DateTime fim, int pessoas,
        string clienteNome, string clienteEmail, string clienteTelefone,
        string? documento, string? cep, string? endereco,
        string? horaEntrada, string? horaSaida, decimal valor, bool semPagamentoOnline, DateTime agora) => new()
    {
        LojaId = lojaId,
        DataInicio = DiaDeReserva.MeioDiaUtc(inicio),
        DataFim = DiaDeReserva.MeioDiaUtc(fim),
        Pessoas = pessoas,
        ClienteNome = clienteNome.Trim(),
        ClienteEmail = clienteEmail.Trim(),
        ClienteTelefone = new string(clienteTelefone.Where(char.IsDigit).ToArray()),
        ClienteDocumento = Limpar(documento),
        ClienteCep = Limpar(cep),
        ClienteEndereco = Limpar(endereco),
        HoraEntrada = Limpar(horaEntrada),
        HoraSaida = Limpar(horaSaida),
        Valor = valor,
        Status = StatusReserva.PendentePagamento,
        FormaPagamento = semPagamentoOnline ? FormaPagamentoReserva.SemPagamentoOnline : null,
        ExpiraEm = semPagamentoOnline ? agora.AddHours(48) : agora.AddMinutes(15),
    };

    public string? CancelarPeloCliente()
    {
        if (Status != StatusReserva.PendentePagamento)
            return "Essa reserva não pode mais ser cancelada.";
        Status = StatusReserva.Cancelada;
        MotivoCancelamento = "Cliente desistiu";
        ExpiraEm = null;
        return null;
    }

    public bool EstadiaTerminou(DateTime agora) => !(DataFim > agora);

    // Escolha da forma de pagamento: só vale para reserva ainda pendente e com uma forma conhecida.
    public string? ValidarEscolhaDePagamento(string? forma)
    {
        if (Status != StatusReserva.PendentePagamento)
            return "Essa reserva não está mais aguardando pagamento.";
        if (!FormaPagamentoReserva.Valida(forma))
            return "Forma de pagamento inválida.";
        return null;
    }

    public void DefinirFormaDePagamento(string forma) => FormaPagamento = forma;

    // Os pagamentos que a forma escolhida exige já foram aprovados? (Pix e/ou cartão)
    public bool PagamentoOnlineAprovado()
    {
        var pixOk = MpStatusPix == "approved";
        var cartaoOk = MpStatusCartao == "approved";
        return FormaPagamento switch
        {
            FormaPagamentoReserva.Pix => pixOk,
            FormaPagamentoReserva.Cartao => cartaoOk,
            FormaPagamentoReserva.Combinado => pixOk && cartaoOk,
            _ => false,
        };
    }

    public bool PrazoVencido(DateTime agora) => ExpiraEm.HasValue && ExpiraEm.Value < agora;

    // Só o Pix (sinal de 50%) deixa a reserva "confirmada parcial"; cartão ou combinado pagam o total.
    public void ConfirmarPorPagamentoOnline(DateTime agora)
    {
        var soPix = FormaPagamento == FormaPagamentoReserva.Pix;
        ValorPago = soPix ? Valor / 2 : Valor;
        Status = soPix ? StatusReserva.ConfirmadaParcial : StatusReserva.Confirmada;
        DataConfirmacao = agora;
        ExpiraEm = null;
    }

    public string? MarcarComoExpirada()
    {
        if (Status != StatusReserva.PendentePagamento)
            return "Só é possível marcar como expirada uma reserva pendente de pagamento.";
        Status = StatusReserva.Expirada;
        return null;
    }

    public string? PodeReativar() =>
        Status != StatusReserva.Expirada ? "Só é possível reativar uma reserva expirada." : null;

    // Volta a ser pendente com prazo novo de 15 minutos (para seguir negociando, use "manter em negociação" depois)
    public void Reativar(DateTime agora)
    {
        Status = StatusReserva.PendentePagamento;
        ExpiraEm = agora.AddMinutes(15);
    }

    public string? Confirmar(decimal? valorPago, DateTime agora)
    {
        if (Status == StatusReserva.Confirmada)
            return "Esta reserva já está confirmada.";
        // Sem valor informado, assume pagamento integral (compatibilidade com o fluxo antigo)
        ValorPago = valorPago ?? Valor;
        Status = StatusReserva.Confirmada;
        DataConfirmacao = agora;
        return null;
    }

    public string? RegistrarPrejuizo(decimal? valor, string? observacao)
    {
        if (valor.HasValue && valor.Value < 0)
            return "O valor não pode ser negativo.";
        ValorPrejuizo = valor;
        ObservacaoPrejuizo = Limpar(observacao);
        return null;
    }

    public string? AvaliarCliente(int? nota, string? comentario)
    {
        if (nota.HasValue && (nota < 1 || nota > 5))
            return "A nota deve ser de 1 a 5.";
        NotaCliente = nota;
        ComentarioCliente = Limpar(comentario);
        return null;
    }

    // Em negociação: continua pendente, mas não expira mais sozinha (só sai se for cancelada/excluída)
    public string? ManterEmNegociacao()
    {
        if (Status != StatusReserva.PendentePagamento)
            return "Só é possível fazer isso em reservas pendentes de pagamento.";
        ExpiraEm = null;
        return null;
    }

    // Reinicia o prazo: volta a expirar sozinha em 15 minutos
    public string? DesfazerNegociacao(DateTime agora)
    {
        if (Status != StatusReserva.PendentePagamento)
            return "Só é possível fazer isso em reservas pendentes de pagamento.";
        ExpiraEm = agora.AddMinutes(15);
        return null;
    }

    // Pagamento adicional de uma reserva já confirmada. Quitado o saldo, sai do "parcial".
    public string? RegistrarPagamento(decimal valor)
    {
        if (Status != StatusReserva.Confirmada && Status != StatusReserva.ConfirmadaParcial)
            return "Só é possível registrar pagamento adicional em reservas já confirmadas.";
        if (valor <= 0)
            return "Informe um valor de pagamento maior que zero.";
        ValorPago = Math.Min(Valor, ValorPago + valor);
        if (ValorPago >= Valor) Status = StatusReserva.Confirmada;
        return null;
    }

    // Edição pelo dono: troca datas, pessoas, valor e dados do cliente. Campos opcionais em branco mantêm o valor atual.
    public void AtualizarDados(
        DateTime inicio, DateTime fim, int pessoas, decimal valor,
        string clienteNome, string clienteEmail, string clienteTelefone,
        string? documento, string? cep, string? endereco, string? numero, string? cidade,
        string? horaEntrada, string? horaSaida)
    {
        DataInicio = inicio;
        DataFim = fim;
        Pessoas = pessoas;
        ClienteNome = clienteNome.Trim();
        ClienteEmail = clienteEmail.Trim();
        ClienteTelefone = new string(clienteTelefone.Where(char.IsDigit).ToArray());
        ClienteDocumento = Limpar(documento) ?? ClienteDocumento;
        ClienteCep = Limpar(cep) ?? ClienteCep;
        ClienteEndereco = Limpar(endereco) ?? ClienteEndereco;
        ClienteNumero = Limpar(numero) ?? ClienteNumero;
        ClienteCidade = Limpar(cidade) ?? ClienteCidade;
        HoraEntrada = Limpar(horaEntrada) ?? HoraEntrada;
        HoraSaida = Limpar(horaSaida) ?? HoraSaida;
        Valor = valor;
    }
}