using static LojaApi.Controllers.Plataforma.AdminController;

using LojaApi.Application.Plataforma;

namespace LojaApi.DTOs;

// ── Loja ──────────────────────────────────────────────────────────
public record CriarLojaRequest(
    string Nome, string Email,
    string? Cnpj, string? Cpf,
    string? Telefone, string? Endereco,
    string CorPrimaria,
    int MensalidadeDia,
    decimal MensalidadeValor,
    string AdminNome,
    string AdminEmail,
    string AdminSenha,
    string? PerfilId = null,
    string? TipoPlano = null,
    string? ModulosAtivos = null,
    bool EhTeste = false
);

public record AtualizarLojaRequest(
    string Nome, string Email,
    string? Cnpj, string? Cpf,
    string? Telefone, string? Endereco,
    string CorPrimaria, string? LogoUrl,
    int MensalidadeDia, decimal MensalidadeValor,
    string? TipoPlano, string? ModulosAtivos,
    bool? EhTeste
);

public record LojaDto(
    Guid Id, string Nome, string Email,
    string? Cnpj, string? Cpf,
    string? Telefone, string? Endereco,
    string CorPrimaria, string? LogoUrl,
    string Status,
    DateTime TrialAte,
    int MensalidadeDia, decimal MensalidadeValor,
    DateTime? ProximoVencimento,
    DateTime? UltimaCobranca,
    string SchemaNome, DateTime CriadoEm,
    int TotalUsuarios, decimal TotalPago,
    bool EmAtraso, int DiasAtraso,
    bool Promocional,
    string Fase, int DiasRestantes,
    string TipoPlano, string ModulosAtivos,
    bool EhTeste,string? AssinaturaStatus,
    string? AssinaturaCartaoFinal,
    DateTime? UltimoLoginEm
);

public record LojaResumoDto(
    Guid Id, string Nome, string Email,
    string Status,
    DateTime? ProximoVencimento,
    decimal MensalidadeValor,
    bool EmAtraso, int DiasAtraso
);

public record AlterarStatusLojaRequest(string Status, string? Motivo);

// ── Agendamento ─────────────────────────────────────────────────────
public record AtualizarValorRequest(
    decimal NovoValor,
    bool SincronizarAssinatura   // true = também atualiza no MP; false = só próximas faturas
);

// ── Pagamento ─────────────────────────────────────────────────────
public record RegistrarPagamentoManualRequest(
    Guid LojaId, decimal Valor,
    DateTime Vencimento, DateTime PagoEm,
    string FormaPagamento, string? Observacao
);

// ── Mercado Pago ──────────────────────────────────────────────────
public record CriarPagamentoMpRequest(
    Guid PagamentoId,
    string FormaPagamento, // pix | boleto | cartao
    // Cartão de crédito
    string? CardToken,
    int? Parcelas,
    // Dados do pagador
    string? CpfPagador,
    string? NomePagador,
    string? EmailPagador
);

public record PagamentoMpResponse(
    string Status,
    string? QrCode,
    string? QrCodeBase64,
    string? BoletoUrl,
    string? BoletoBarcode,
    string? MpPaymentId
);

public record CriarAssinaturaRequest(
    string CardToken,
    string? CpfPagador,
    string? NomePagador,
    string? EmailPagador,
    string? CartaoFinal   // últimos 4 dígitos (exibição)
);

public record AssinaturaResponse(
    string Status,
    string? PreapprovalId,
    string? CartaoFinal
);

// ── Dashboard admin ───────────────────────────────────────────────
public record DashboardAdminDto(
    int TotalLojas,
    int LojasAtivas,
    int LojasTrial,
    int LojasBloqueadas,
    int LojasEmAtraso,
    decimal ReceitaMensal,
    decimal ReceitaTotal,
    List<LojaResumoDto> LojasAtrasadas,
    List<PagamentoDto> UltimosPagamentos,
    List<ProjecaoMensalDto> ProjecaoMensal
);

public record AtualizarPlanoRequest(string TipoPlano, string? ModulosAtivos);
