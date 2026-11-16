using LojaApi.Models;

namespace LojaApi.Application.Plataforma;

// O que o painel do superadmin e a cobrança da mensalidade DEVOLVEM (os nomes das propriedades viram o JSON da API)

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
    bool EhTeste, string? AssinaturaStatus,
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

public record ProjecaoMensalDto(Guid Id, string Nome, decimal MensalidadeValor, int MensalidadeDia, DateTime? ProximoVencimento, string Status);

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

public record LojaPlanoDto(Guid Id, string TipoPlano, string ModulosAtivos);
public record TrialAlteradoDto(Guid Id, DateTime TrialAte, StatusLoja Status);
public record ValorAtualizadoDto(string Mensagem, string? Aviso, bool Sincronizou);
public record AcessoSuporteDto(string Token, string Nome, string Email, string Role, string NomeLoja);

public record ComunicadoDto(
    Guid Id, string Assunto, string CorpoHtml, bool TodasLojas, int TotalEnviados, int TotalFalhas,
    string[] DestinatariosSucesso, string[] DestinatariosFalha, DateTime EnviadoEm
);

public record ComunicadoEnviadoDto(string Mensagem, int TotalEnviados, int TotalFalhas, List<string> Falhas);

// Arquivo para baixar (o controller devolve como File)
public record ArquivoDto(byte[] Conteudo, string TipoConteudo, string NomeArquivo);

// ── Cobrança da mensalidade ──
public record PagamentoMpResponse(
    string Status,
    string? QrCode,
    string? QrCodeBase64,
    string? BoletoUrl,
    string? BoletoBarcode,
    string? MpPaymentId
);

public record AssinaturaResponse(string Status, string? PreapprovalId, string? CartaoFinal);
public record StatusDto(string? Status);
