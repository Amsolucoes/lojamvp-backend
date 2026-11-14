namespace LojaApi.Application.Vendas;

// Comandos de Vendas: o que muda dados (cada um tem o seu handler em Application/Handlers/Vendas).

// Vendas
public record RegistrarVendaCommand(Guid? LojaId, CriarVendaRequest Req);
public record ExcluirVendaCommand(Guid? LojaId, Guid Id);

// Trocas
public record RegistrarTrocaCommand(Guid? LojaId, CriarTrocaRequest Req);

// Caixa
public record CriarMovimentoCaixaCommand(Guid? LojaId, CriarMovimentoRequest Req);
public record EditarMovimentoCaixaCommand(Guid? LojaId, Guid Id, EditarMovimentoRequest Req);
public record ExcluirMovimentoCaixaCommand(Guid? LojaId, Guid Id);

// Origens de venda
public record CriarOrigemVendaCommand(Guid? LojaId, SalvarOrigemRequest Req);
public record AtualizarOrigemVendaCommand(Guid? LojaId, Guid Id, SalvarOrigemRequest Req);
public record ExcluirOrigemVendaCommand(Guid? LojaId, Guid Id);
public record CriarOrigensPadraoCommand(Guid? LojaId);
