namespace LojaApi.DTOs;

// ── Auth ──────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Senha);
public record LoginResponse(string Token, string Nome, string Email, string Role);

public record SignupRequest(
    string NomeLoja,
    string PerfilId,
    string NomeResponsavel,
    string Email,
    string Senha,
    string? Telefone,
    List<string>? ModulosExtras = null
);

public record TrocarSenhaRequest(string SenhaAtual, string NovaSenha);

public record TrocarMeuEmailRequest(string NovoEmail, string SenhaAtual);

// ── Usuario ───────────────────────────────────────────────────────
public record UsuarioDto(Guid Id, string Nome, string Email, string Role, bool Ativo);
public record CriarUsuarioRequest(string Nome, string Email, string Senha, string Role);
public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
public record TrocarEmailRequest(
    string NovoEmail,
    bool TrocarLogin,   // troca o e-mail do usuário (login)
    bool TrocarLoja     // troca o e-mail de contato da loja
);

// ── Produto ───────────────────────────────────────────────────────
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

public record SalvarProdutoRequest(
    string Nome, string? Descricao, string Categoria,
    decimal PrecoCusto, decimal PrecoVenda,
    decimal Estoque, decimal EstoqueMinimo,
    string? CodigoBarras, bool Ativo,
    string TipoVenda, string UnidadeMedida,
    Guid? MarcaId = null,
    Guid? FornecedorId = null
);

// ── Fornecedor ────────────────────────────────────────────────────
public record FornecedorDto(
    Guid Id, string Nome, string? CnpjCpf, string? Telefone,
    string? Email, string? Endereco, string? Observacoes,
    bool Ativo, DateTime CriadoEm, int QtdProdutos,
    string? Cep = null, string? Rua = null, string? Numero = null,
    string? Bairro = null, string? Cidade = null, string? Uf = null
);

public record SalvarFornecedorRequest(
    string Nome, string? CnpjCpf, string? Telefone,
    string? Email, string? Endereco, string? Observacoes,
    bool Ativo = true,
    string? Cep = null, string? Rua = null, string? Numero = null,
    string? Bairro = null, string? Cidade = null, string? Uf = null
);

public record ProdutoPrecoItem(Guid Id, decimal PrecoCusto, decimal PrecoVenda);
public record AtualizarPrecosEmLoteRequest(List<ProdutoPrecoItem> Produtos);

public record AtualizarStatusEmLoteRequest(List<Guid> Ids, bool Ativo);

// ── Estoque ───────────────────────────────────────────────────────
public record AjusteEstoqueRequest(
    Guid ProdutoId, decimal Quantidade,
    string Tipo, // entrada | saida | ajuste
    string? Observacao
);

public record MovimentoDto(
    Guid Id, Guid ProdutoId, string NomeProduto,
    string Tipo, decimal Quantidade,
    string? Observacao, DateTime CriadoEm
);

// ── Perfis ────────────────────────────────────────────────────────
public record PerfilLojaDto(
    Guid Id, string Nome, string? Descricao, string Icone,
    List<CategoriaPerfilDto> Categorias,
    List<CampoExtraPerfilDto> CamposExtras
);
public record CategoriaPerfilDto(Guid Id, string Nome, int Ordem);
public record CampoExtraPerfilDto(
    Guid Id, string Chave, string Label,
    string Tipo, string? Opcoes,
    bool Obrigatorio, int Ordem
);
public record CategoriaLojaDto(Guid Id, string Nome, bool Ativo, int Ordem);
public record CampoExtraLojaDto(
    Guid Id, string Chave, string Label,
    string Tipo, string? Opcoes,
    bool Obrigatorio, bool Ativo, int Ordem
);

// ── Variações ─────────────────────────────────────────────────────
public record ProdutoVariacaoDto(
    Guid Id, string? Tamanho, string? Cor,
    string? OutroCampo, string? CodigoBarras, 
    int Estoque,int EstoqueMinimo, bool Ativo
);

public record SalvarVariacaoRequest(
    string? Tamanho, string? Cor,
    string? OutroCampo, string? CodigoBarras,
    int Estoque, int EstoqueMinimo
);

public record AjusteVariacaoRequest(
    Guid ProdutoId, Guid VariacaoId,
    int Quantidade, string Tipo,
    string? Observacao
);

// ── Categoriias ────────────────────────────────────────────────────────
public record CriarCategoriaRequest(
    string Nome,
    string TipoTamanho,        // letra | numero | personalizado
    bool UsaTamanho,
    bool UsaCor,
    string? TamanhosPersonalizados
);