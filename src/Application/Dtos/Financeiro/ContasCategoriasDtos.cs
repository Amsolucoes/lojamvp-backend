namespace LojaApi.Application.Financeiro;

// O que o Financeiro (contas e categorias) DEVOLVE (os nomes das propriedades viram o JSON da API)

public record ContaBancariaDto(Guid Id, string Nome, decimal SaldoInicial, bool Ativa, string? Banco, decimal Limite, decimal SaldoAtual);
public record ContaAtivaDto(Guid Id, bool Ativa);
public record SaldoAtualDto(decimal SaldoAtual);
public record SaldosDaTransferenciaDto(decimal SaldoOrigem, decimal SaldoDestino);
public record CategoriaFinanceiraDto(Guid Id, string Nome, string Tipo, string? Icone);
public record MensagemDto(string Mensagem);
