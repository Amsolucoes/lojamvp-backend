namespace LojaApi.Application.NfImportacao;

// Comandos de NfImportacao: o que muda dados (cada um tem o seu handler em Application/Handlers/NfImportacao).
// LojaId nulo = usuário sem vínculo com loja.

public record ConfirmarImportacaoCommand(Guid? LojaId, ConfirmarImportacaoRequest Req);
public record LancarNfManualCommand(Guid? LojaId, LancarNfManualRequest Req);
public record EditarNfManualCommand(Guid? LojaId, Guid Id, EditarNfManualRequest Req);
public record DesfazerImportacaoCommand(Guid? LojaId, Guid Id);
