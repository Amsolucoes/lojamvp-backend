namespace LojaApi.Application.Plataforma;

// Comandos da Plataforma: o que muda dados (cada um tem o seu handler em Application/Handlers/Plataforma).
// LojaId nulo = usuário sem vínculo com loja.

// Auth
public record LoginCommand(LoginRequest Req);
public record RegistrarSessaoCommand(string? Id, string? Nome, string? Email, string? Role, bool AcessoSuporte);
public record CadastrarLojaCommand(SignupRequest Req);
public record TrocarSenhaCommand(Guid UsuarioId, TrocarSenhaRequest Req);

// Perfis e preços dos módulos
public record AplicarPerfilCommand(Guid? LojaId, AplicarPerfilRequest Req);
public record AdicionarCategoriaCommand(Guid? LojaId, SalvarCategoriaRequest Req);
public record AtualizarModuloPrecoCommand(Guid Id, AtualizarModuloPrecoRequest Req);

// Cliente
public record AtualizarConfigClienteCommand(Guid UsuarioId, AtualizarConfigRequest Req);
public record TrocarMeuEmailCommand(Guid UsuarioId, TrocarMeuEmailRequest Req);

// Loja
public record AtualizarAgendaHorarioCommand(Guid? LojaId, AgendaHorarioRequest Req);
public record ConfigurarAgendamentoOnlineCommand(Guid? LojaId, AgendamentoOnlineConfigRequest Req);
public record AlternarModuloCommand(Guid? LojaId, AlternarModuloRequest Req);
public record ConfigurarPausaCommand(Guid? LojaId, PausaAgendamentoRequest Req);
public record DefinirSlugCommand(Guid? LojaId, DefinirSlugRequest Req);

// Push
public record AssinarPushCommand(Guid UsuarioId, Guid? LojaId, AssinarRequest Req);
public record CancelarPushCommand(Guid UsuarioId, CancelarRequest Req);
public record TestarPushCommand(Guid UsuarioId);
