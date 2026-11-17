namespace LojaApi.Application.Financeiro;

// Dados lidos para montar os relatórios do Financeiro (os nomes das propriedades casam com as colunas lidas no Dapper)

// Lançamento do mês com a categoria (pagar e receber; parcelas de financiamento de cartão trazem o cartão de origem)
public record LancamentoDoMesDto(string Tipo, string Status, decimal Valor, DateTime Vencimento, Guid? CartaoOrigemId, string? CategoriaNome, string? CategoriaIcone);

public record PagamentoDePlanoDto(string Status, decimal Valor, DateTime MesReferencia);

public record CartaoDoRelatorioDto(Guid Id, string Nome, string? Bandeira, decimal Limite, int DiaFechamento, int DiaVencimento);

public record CompraLeveDto(Guid CartaoId, DateTime DataCompra, decimal Valor, string Modo);

public record CompraCategorizadaDto(Guid Id, Guid CartaoId, string Descricao, decimal Valor, DateTime DataCompra, string Modo, string? CategoriaNome, string? CategoriaIcone);

public record FaturaDoRelatorioDto(Guid Id, Guid CartaoCreditoId, DateTime MesReferencia, string Status, decimal Total);

public record FinanciamentoPendenteDto(Guid CartaoId, decimal Total);

public record LancamentoDetalheDto(Guid Id, string Modo, string Descricao, decimal Valor, DateTime Vencimento, string Status, string? ContaNome);

public record LancamentoPendenteDto(Guid Id, string Descricao, string Tipo, decimal Valor, DateTime Vencimento);
