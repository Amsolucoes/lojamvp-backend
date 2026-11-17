namespace LojaApi.Application.Financeiro;

// Comandos do Financeiro (fatura do cartão): o que muda dados (cada um tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record AdicionarAntecipadoFaturaCommand(Guid? LojaId, Guid CartaoId, int Ano, int Mes, AntecipadoFaturaRequest Req);
public record ExcluirAntecipadoFaturaCommand(Guid? LojaId, Guid Id);
public record PagarFaturaCommand(Guid? LojaId, Guid CartaoId, int Ano, int Mes, PagarFaturaRequest Req);
