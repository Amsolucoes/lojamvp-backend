namespace LojaApi.Application.Plataforma;

// Consultas da Plataforma: só leem dados (cada uma tem o seu handler em Application/Handlers/Plataforma).

public record GerarHashQuery(string Senha);

public record ListarPerfisQuery();
public record ListarCategoriasDaLojaQuery(Guid? LojaId);
public record ListarCamposExtrasDaLojaQuery(Guid? LojaId);

public record ListarModulosPrecoQuery();

// O dashboard grava o bloqueio da loja quando o vencimento passou (mesmo sendo uma leitura)
public record DashboardClienteQuery(Guid UsuarioId);
public record ConfigClienteQuery(Guid UsuarioId);
public record ListarFaturasClienteQuery(Guid UsuarioId);

public record SituacaoLojaQuery(Guid? LojaId);

public record ChavePublicaPushQuery();
public record StatusPushQuery(Guid UsuarioId, string? Endpoint);
