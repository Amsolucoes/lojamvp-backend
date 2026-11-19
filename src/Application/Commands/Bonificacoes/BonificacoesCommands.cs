namespace LojaApi.Application.Bonificacoes;

// Comandos de Bonificações: o que muda dados (cada um tem o seu handler em Application/Handlers/Bonificacoes).
// LojaId nulo = usuário sem vínculo com loja.

public record RegistrarBonificacaoCommand(Guid? LojaId, Guid? UsuarioId, RegistrarBonificacaoRequest Req);
public record EstornarBonificacaoCommand(Guid? LojaId, Guid Id);
