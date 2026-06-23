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
    string AdminSenha
);

public record AtualizarLojaRequest(
    string Nome, string Email,
    string? Cnpj, string? Cpf,
    string? Telefone, string? Endereco,
    string CorPrimaria, string? LogoUrl,
    int MensalidadeDia, decimal MensalidadeValor
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
    bool Promocional
);

public record LojaResumoDto(
    Guid Id, string Nome, string Email,
    string Status,
    DateTime? ProximoVencimento,
    decimal MensalidadeValor,
    bool EmAtraso, int DiasAtraso
);

public record LojaConfigDto(
    Guid Id, string Nome,
    string CorPrimaria, string? LogoUrl,
    string Status, string? MotivoBloqueo
);

public record AlterarStatusLojaRequest(string Status, string? Motivo);

// ── Pagamento ─────────────────────────────────────────────────────
public record PagamentoDto(
    Guid Id, Guid LojaId, string NomeLoja,
    decimal Valor, string Status,
    DateTime Vencimento, DateTime? PagoEm,
    string? FormaPagamento, string? Observacao,
    string? MpQrCode, string? MpQrCodeBase64,
    string? MpBoletoUrl, string? MpBoletoBarcode,
    DateTime CriadoEm
);

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
    List<PagamentoDto> UltimosPagamentos
);

// ── Dashboard cliente ─────────────────────────────────────────────
public record DashboardClienteDto(
    string NomeLoja,
    string Status,
    DateTime? TrialAte,
    DateTime? ProximoVencimento,
    decimal MensalidadeValor,
    bool EmAtraso,
    int DiasAtraso,
    PagamentoDto? FaturaPendente,
    List<PagamentoDto> HistoricoFaturas
);
