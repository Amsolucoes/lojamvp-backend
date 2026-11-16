namespace LojaApi.Application.Financeiro;

// O que a API do Financeiro (cartões) DEVOLVE nas listagens (os nomes das propriedades são o JSON da resposta)

public record CartaoDto(Guid Id, string Nome, decimal Limite, int DiaFechamento, int DiaVencimento, Guid ContaBancariaId, bool Ativo, decimal TaxaJurosMensal, string? Bandeira);

public record CartaoFixoDto(Guid Id, string Descricao, decimal Valor, bool Ativo);
