namespace LojaApi.Application.Produtos;

// O que Produtos RECEBE no corpo da requisição

// Cadastro
public record SalvarProdutoRequest(
    string Nome, string? Descricao, string Categoria,
    decimal PrecoCusto, decimal PrecoVenda,
    decimal Estoque, decimal EstoqueMinimo,
    string? CodigoBarras, bool Ativo,
    string TipoVenda, string UnidadeMedida,
    Guid? MarcaId = null,
    Guid? FornecedorId = null
);

public record ProdutoPrecoItem(Guid Id, decimal PrecoCusto, decimal PrecoVenda);
public record AtualizarPrecosEmLoteRequest(List<ProdutoPrecoItem> Produtos);
public record AtualizarStatusEmLoteRequest(List<Guid> Ids, bool Ativo);

public record SalvarVariacaoRequest(
    string? Tamanho, string? Cor,
    string? OutroCampo, string? CodigoBarras,
    int Estoque, int EstoqueMinimo
);

// Estoque
public record AjusteEstoqueRequest(
    Guid ProdutoId, decimal Quantidade,
    string Tipo, // entrada | saida | ajuste
    string? Observacao
);

public record AjusteVariacaoRequest(
    Guid ProdutoId, Guid VariacaoId,
    int Quantidade, string Tipo,
    string? Observacao
);

// Categorias e marcas
public record CriarCategoriaRequest(
    string Nome,
    string TipoTamanho,        // letra | numero | personalizado
    bool UsaTamanho,
    bool UsaCor,
    string? TamanhosPersonalizados
);

public record CriarMarcaRequest(string Nome);

// Fornecedores
public record SalvarFornecedorRequest(
    string Nome, string? CnpjCpf, string? Telefone,
    string? Email, string? Endereco, string? Observacoes,
    bool Ativo = true,
    string? Cep = null, string? Rua = null, string? Numero = null,
    string? Bairro = null, string? Cidade = null, string? Uf = null
);

// Modelos de etiqueta
public record SalvarModeloRequest(
    string Nome,
    bool IncluirLogo, bool UsarLogoPropria, string? LogoEtiquetaUrl,
    bool IncluirNomeMarca, bool IncluirNomeProduto, bool IncluirPreco, bool IncluirCodigoBarras,
    decimal LarguraMm, decimal AlturaMm,
    string? CorTexto, string? CorFundo, string? FonteFamilia, int? EscalaFonte
);
