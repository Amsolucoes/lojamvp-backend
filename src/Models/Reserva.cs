using System.Text.Json.Serialization;
using LojaApi.Models;

namespace LojaApi.src.Models;

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
}