namespace LojaApi.Application.Financeiro;

// Comandos do Financeiro (lançamentos, parcelas e fixos): o que muda dados (cada um tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record CriarLancamentoAvulsoCommand(Guid? LojaId, SalvarLancamentoAvulsoRequest Req);
public record CriarLancamentoParceladoCommand(Guid? LojaId, SalvarLancamentoParceladoRequest Req);
public record ExcluirParcelasCommand(Guid? LojaId, Guid Id, ParcelasIdsRequest Req);
public record MarcarParcelasPagasCommand(Guid? LojaId, Guid Id, ParcelasPagamentoRequest Req);
public record AlterarTotalParcelasCommand(Guid? LojaId, Guid Id, ParcelasTotalRequest Req);
public record PagarLoteCommand(Guid? LojaId, PagarLoteRequest Req);
public record MarcarPagamentoCommand(Guid? LojaId, Guid Id, MarcarPagamentoRequest Req);
public record EditarLancamentoCommand(Guid? LojaId, Guid Id, string Modo, EditarLancamentoRequest Req);
public record ExcluirLancamentoCommand(Guid? LojaId, Guid Id, string Modo);
public record AlternarAvisarCommand(Guid? LojaId, Guid Id, ToggleAvisarRequest Req);

public record CriarFixoCommand(Guid? LojaId, SalvarLancamentoFixoRequest Req);
public record AtualizarFixoCommand(Guid? LojaId, Guid Id, SalvarLancamentoFixoRequest Req);
public record RegenerarFixoCommand(Guid? LojaId, Guid Id);
public record AlternarFixoCommand(Guid? LojaId, Guid Id);
