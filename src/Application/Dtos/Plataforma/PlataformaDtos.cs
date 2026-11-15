namespace LojaApi.Application.Plataforma;

// O que a Plataforma DEVOLVE (os nomes das propriedades viram o JSON da API)

public record LoginResponse(string Token, string Nome, string Email, string Role);
public record SessaoDto(string? Id, string? Nome, string? Email, string? Role);
public record HashDto(string Hash);
public record MensagemDto(string Mensagem);

// ── Perfis ──
public record PerfilCategoriaDto(Guid Id, string Nome, int Ordem, string TipoTamanho);
public record PerfilCampoExtraDto(Guid Id, string Chave, string Label, string Tipo, string? Opcoes, bool Obrigatorio, int Ordem);
public record PerfilDto(Guid Id, string Nome, string? Descricao, string Icone, string TipoPlanoAplica, IEnumerable<PerfilCategoriaDto> Categorias, IEnumerable<PerfilCampoExtraDto> CamposExtras);
public record CategoriaLojaDto(Guid Id, string Nome, bool Ativo, int Ordem, string TipoTamanho);
public record CampoExtraLojaDto(Guid Id, string Chave, string Label, string Tipo, string? Opcoes, bool Obrigatorio, bool Ativo, int Ordem);
public record CategoriaCriadaDto(Guid Id, string Nome, bool Ativo, int Ordem);

// ── Cliente ──
public record PagamentoDto(
    Guid Id, Guid LojaId, string NomeLoja,
    decimal Valor, string Status,
    DateTime Vencimento, DateTime? PagoEm,
    string? FormaPagamento, string? Observacao,
    string? MpQrCode, string? MpQrCodeBase64,
    string? MpBoletoUrl, string? MpBoletoBarcode,
    DateTime CriadoEm
);

public record DashboardClienteDto(
    string NomeLoja,
    string Status,
    DateTime? TrialAte,
    DateTime? ProximoVencimento,
    decimal MensalidadeValor,
    bool EmAtraso,
    int DiasAtraso,
    PagamentoDto? FaturaPendente,
    List<PagamentoDto> HistoricoFaturas,
    string? AssinaturaStatus = null,
    string? AssinaturaCartaoFinal = null
);

public record LojaConfigDto(
    Guid Id, string Nome,
    string CorPrimaria, string? LogoUrl,
    string Status, string? MotivoBloqueo,
    string? Endereco, string? Telefone
);

// ── Loja ──
public record SituacaoSuperadminDto(string Fase, int DiasRestantes);

public record SituacaoLojaDto(
    string Fase, int DiasRestantes, string Status, decimal MensalidadeValor, DateTime TrialAte, DateTime? ProximoVencimento,
    string NomeLoja, string? EnderecoLoja, string? TelefoneLoja, string? LogoUrlLoja, string TipoPlano, string[] ModulosAtivos,
    int AgendaHoraInicio, int AgendaHoraFim, string? AssinaturaStatus, string? AssinaturaCartaoFinal,
    bool AgendamentoOnlineAtivo, string AgendamentoOnlineConfirmacao, string? Slug,
    Dictionary<string, DateTime> ModulosAlteradoEm, DateTime? PausaAte, string? PausaMensagem
);

public record AgendaHorarioDto(int AgendaHoraInicio, int AgendaHoraFim);
public record AgendamentoOnlineDto(bool AgendamentoOnlineAtivo, string AgendamentoOnlineConfirmacao, string? Slug);
public record ModulosDaLojaDto(string ModulosAtivos, decimal MensalidadeValor);
public record PausaDto(DateTime? PausaAte, string? PausaMensagem);
public record SlugDto(string? Slug);

// ── Push ──
public record ChavePushDto(string? Chave);
public record StatusPushDto(bool Configurado, bool Ativo);
public record AtivoPushDto(bool Ativo);
public record EnviadosPushDto(int Enviados);
