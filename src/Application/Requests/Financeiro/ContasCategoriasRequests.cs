namespace LojaApi.Application.Financeiro;

// O que a API do Financeiro (contas e categorias) RECEBE (os nomes das propriedades são o JSON do corpo)

public record SalvarContaBancariaRequest(string Nome, decimal SaldoInicial, string? Banco = null, decimal Limite = 0);
public record AjusteSaldoRequest(string Tipo, decimal? Valor, decimal NovoSaldo, string? Observacao);
public record TransferenciaContaRequest(Guid ContaOrigemId, Guid ContaDestinoId, decimal Valor, bool Registrar, string? Observacao);
public record SalvarCategoriaFinanceiraRequest(string Nome, string Tipo, string? Icone);
