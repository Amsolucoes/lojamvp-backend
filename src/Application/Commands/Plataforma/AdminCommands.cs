namespace LojaApi.Application.Plataforma;

// Comandos do painel do superadmin e da cobrança da mensalidade (cada um tem o seu handler em Application/Handlers/Plataforma).

// Painel do superadmin
public record CriarLojaAdminCommand(CriarLojaRequest Req);
public record AtualizarLojaAdminCommand(Guid Id, AtualizarLojaRequest Req);
public record AlterarStatusLojaCommand(Guid Id, AlterarStatusLojaRequest Req);
public record AtualizarPlanoLojaCommand(Guid Id, AtualizarPlanoRequest Req);
public record TrocarEmailLojaCommand(Guid Id, TrocarEmailRequest Req);
public record AtualizarValorLojaCommand(Guid Id, AtualizarValorRequest Req);
public record AlterarTrialCommand(Guid Id, AlterarTrialRequest Req);
public record ExcluirLojaCommand(Guid Id);
public record ExcluirProdutosEVendasCommand(Guid Id);
public record AcessarComoSuporteCommand(Guid Id);
public record RegistrarPagamentoManualCommand(Guid AdminId, RegistrarPagamentoManualRequest Req);
public record VerificarBloqueiosCommand();
public record EnviarComunicadoCommand(EnviarComunicadoRequest Req);

// Cobrança da mensalidade (o assinante usa; "EhSuperadmin" libera ver a fatura de qualquer loja)
public record CriarCobrancaCommand(Guid? LojaId, bool EhSuperadmin, CriarPagamentoMpRequest Req);
public record CriarAssinaturaCommand(Guid? LojaId, CriarAssinaturaRequest Req);
public record CancelarAssinaturaCommand(Guid? LojaId);
public record ProcessarWebhookMpCommand(string Corpo);
public record ConsultarStatusPagamentoCommand(Guid Id, Guid? LojaId, bool EhSuperadmin);
