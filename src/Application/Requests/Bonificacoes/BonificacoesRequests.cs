namespace LojaApi.Application.Bonificacoes;

// O que a API de Bonificações RECEBE (os nomes das propriedades são o JSON do corpo)

// Brinde avulso dado a um cliente (fora do caixa): baixa o estoque do produto (ou da variação, se informada)
public record RegistrarBonificacaoRequest(Guid ClienteId, Guid ProdutoId, decimal Quantidade, Guid? VariacaoId = null, string? Observacao = null);
