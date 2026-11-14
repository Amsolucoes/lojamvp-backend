namespace LojaApi.Application.Produtos;

// O que Produtos DEVOLVE (os nomes das propriedades viram o JSON da API)

public record ProdutoVariacaoDto(
    Guid Id, string? Tamanho, string? Cor,
    string? OutroCampo, string? CodigoBarras,
    int Estoque, int EstoqueMinimo, bool Ativo
);

public record ProdutoDto(
    Guid Id, string Nome, string? Descricao, string Categoria,
    decimal PrecoCusto, decimal PrecoVenda,
    decimal Estoque, decimal EstoqueMinimo,
    string? CodigoBarras, bool Ativo,
    DateTime CriadoEm, DateTime AtualizadoEm,
    string TipoVenda, string UnidadeMedida,
    List<ProdutoVariacaoDto>? Variacoes = null,
    Guid? MarcaId = null,
    string? NomeMarca = null,
    Guid? FornecedorId = null,
    string? NomeFornecedor = null
);

public record FornecedorDto(
    Guid Id, string Nome, string? CnpjCpf, string? Telefone,
    string? Email, string? Endereco, string? Observacoes,
    bool Ativo, DateTime CriadoEm, int QtdProdutos,
    string? Cep = null, string? Rua = null, string? Numero = null,
    string? Bairro = null, string? Cidade = null, string? Uf = null
);

public record FornecedorResumoDto(Guid Id, string Nome);

public record MovimentoDto(
    Guid Id, Guid ProdutoId, string NomeProduto,
    string Tipo, decimal Quantidade,
    string? Observacao, DateTime CriadoEm
);

public record AlertaEstoqueDto(Guid Id, string Nome, string Categoria, decimal Estoque, decimal EstoqueMinimo, bool Zerado);

public record CategoriaProdutoDto(Guid Id, string Nome, string TipoTamanho, bool UsaTamanho, bool UsaCor, string? TamanhosPersonalizados);

public record MarcaDto(Guid Id, string Nome);
