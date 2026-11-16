namespace LojaApi.Application.Financeiro;

// Comandos do Financeiro (contas e categorias): o que muda dados (cada um tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record CriarContaCommand(Guid? LojaId, SalvarContaBancariaRequest Req);
public record AtualizarContaCommand(Guid? LojaId, Guid Id, SalvarContaBancariaRequest Req);
public record AlternarContaAtivaCommand(Guid? LojaId, Guid Id);
public record AjustarSaldoCommand(Guid? LojaId, Guid Id, AjusteSaldoRequest Req);
public record TransferirEntreContasCommand(Guid? LojaId, TransferenciaContaRequest Req);

public record CriarCategoriaFinanceiraCommand(Guid? LojaId, SalvarCategoriaFinanceiraRequest Req);
public record AtualizarCategoriaFinanceiraCommand(Guid? LojaId, Guid Id, SalvarCategoriaFinanceiraRequest Req);
public record ExcluirCategoriaFinanceiraCommand(Guid? LojaId, Guid Id);
public record SeedCategoriasPadraoCommand(Guid? LojaId);
