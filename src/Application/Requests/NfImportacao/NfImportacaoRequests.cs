namespace LojaApi.Application.NfImportacao;

// O que a API de NfImportacao RECEBE (os nomes das propriedades são o JSON do corpo)

public record ItemConfirmacao(
    string CodigoFornecedor, string? Gtin,
    string NomeBase, string? Cor, string? Tamanho,
    decimal Quantidade,
    string Acao, // "existente" | "novo"
    Guid? ProdutoId, // obrigatório se Acao == "existente"
    decimal? PrecoCusto, // obrigatório se Acao == "novo" — editável, pré-preenchido com o valor da nota
    decimal? PrecoVenda, // obrigatório se Acao == "novo"
    string? CategoriaNome // obrigatório se Acao == "novo"
);

public record ConfirmarImportacaoRequest(string CnpjFornecedor, string NumeroNf, string ChaveAcesso, string NomeFornecedor, List<ItemConfirmacao> Itens);

public record ItemNfManualRequest(
    Guid? ProdutoId, Guid? VariacaoId, decimal Quantidade, decimal? PrecoCusto,
    string? Acao, // null/"existente" (padrão) | "novo"
    string? NomeBase, string? CategoriaNome, string? Cor, string? Tamanho, decimal? PrecoVenda, string? Gtin,
    string? TipoVenda = null, // "unidade" (padrão) | "fracionado" — só usado quando Acao == "novo"
    string? UnidadeMedida = null
);

public record LancarNfManualRequest(
    Guid FornecedorId, string NumeroNf, DateTime? DataEmissao, decimal? ValorTotal,
    List<ItemNfManualRequest> Itens
);

public record ItemNfEditRequest(Guid ProdutoId, Guid? VariacaoId, decimal Quantidade, decimal? PrecoCusto);

public record EditarNfManualRequest(
    Guid FornecedorId, string NumeroNf, DateTime? DataEmissao, decimal? ValorTotal,
    List<ItemNfEditRequest> Itens
);
