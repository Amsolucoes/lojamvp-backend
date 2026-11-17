namespace LojaApi.Application.Financeiro;

// Comandos do Financeiro (cartões, compras e fixos do cartão): o que muda dados (cada um tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record CriarCartaoCommand(Guid? LojaId, SalvarCartaoRequest Req);
public record AtualizarCartaoCommand(Guid? LojaId, Guid Id, SalvarCartaoRequest Req);
public record LancarCompraCommand(Guid? LojaId, Guid CartaoId, SalvarLancamentoCartaoRequest Req);
public record LancarCompraParceladaCommand(Guid? LojaId, Guid CartaoId, CompraParceladaCartaoRequest Req);
public record CriarCartaoFixoCommand(Guid? LojaId, Guid CartaoId, CartaoFixoRequest Req);
public record AlternarCartaoFixoCommand(Guid? LojaId, Guid Id);
public record EditarLancamentoCartaoCommand(Guid? LojaId, Guid Id, string Modo, EditarLancamentoCartaoRequest Req);
public record ExcluirLancamentoCartaoCommand(Guid? LojaId, Guid Id, string Modo);
