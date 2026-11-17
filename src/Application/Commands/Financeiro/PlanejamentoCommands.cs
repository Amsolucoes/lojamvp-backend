namespace LojaApi.Application.Financeiro;

// Comandos do Financeiro (Planejamento e troca de categoria): o que muda dados (cada um tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record SalvarPlanejamentoConfigCommand(Guid? LojaId, PlanejamentoConfigRequest Req);
public record SalvarPlanejamentoCategoriasCommand(Guid? LojaId, PlanejamentoCategoriasRequest Req);
public record CriarMetaPlanejamentoCommand(Guid? LojaId, MetaPlanejamentoRequest Req);
public record AtualizarMetaPlanejamentoCommand(Guid? LojaId, Guid Id, MetaPlanejamentoRequest Req);
public record AportarMetaPlanejamentoCommand(Guid? LojaId, Guid Id, AporteMetaRequest Req);
public record ExcluirMetaPlanejamentoCommand(Guid? LojaId, Guid Id);
public record TrocarCategoriaLancamentoCommand(Guid? LojaId, Guid Id, TrocarCategoriaRequest Req);
public record TrocarCategoriaCompraCartaoCommand(Guid? LojaId, Guid Id, TrocarCategoriaRequest Req);
