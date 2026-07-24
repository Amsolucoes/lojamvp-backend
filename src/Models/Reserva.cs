using LojaApi.Models;

namespace LojaApi.src.Models;

public class Reserva
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public DateTime DataInicio { get; set; } // Utc, meio-dia, seguindo o padrao do projeto
    public DateTime DataFim { get; set; }

    public int Pessoas { get; set; }

    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public string ClienteTelefone { get; set; } = string.Empty;
    public string? ClienteDocumento { get; set; } // CPF, usado no contrato

    public decimal Valor { get; set; }
    public decimal ValorPago { get; set; } = 0;
    public string Status { get; set; } = "pendente_pagamento";
    // valores: pendente_pagamento | confirmada | cancelada | expirada

    public string? MpPreferenceId { get; set; }
    public string? MpPaymentId { get; set; }

    public DateTime? ContratoEnviadoEm { get; set; }
    public bool AvisoCheckoutEnviado { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiraEm { get; set; } // usada pra liberar reservas pendentes nao pagas

    public Loja? Loja { get; set; }
}