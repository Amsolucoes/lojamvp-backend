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
    string? Telefone
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
    List<ProdutoVariacaoDto>? Variacoes = null
);

public record SalvarProdutoRequest(
    string Nome, string? Descricao, string Categoria,
    decimal PrecoCusto, decimal PrecoVenda,
    decimal Estoque, decimal EstoqueMinimo,
    string? CodigoBarras, bool Ativo,
    string TipoVenda, string UnidadeMedida
);

// ── Cliente ───────────────────────────────────────────────────────
public record ClienteDto(
    Guid Id, string Nome, string Telefone,
    string? Cpf, string? Email, string? Endereco,
    string? Observacoes, DateTime CriadoEm,
    int TotalCompras, decimal TotalGasto,
    DateTime? DataNascimento,
    decimal creditoLoja
);

public record SalvarClienteRequest(
    string Nome, string Telefone,
    string? Cpf, string? Email,
    string? Endereco, string? Observacoes,
    DateTime? DataNascimento
);

// ── Venda ─────────────────────────────────────────────────────────
public record ItemVendaRequest(
    Guid? ProdutoId, 
    decimal Quantidade, 
    decimal PrecoUnitario, 
    Guid? VariacaoId = null,
    Guid? ServicoId = null,
    Guid? AgendamentoId = null
);

public record CriarVendaRequest(
    List<ItemVendaRequest> Itens,
    Guid? ClienteId,
    decimal Desconto,
    string FormaPagamento,
    string? FormasPagamento,
    decimal? Troco,
    decimal? CreditoUsado
);

public record ItemVendaDto(
    Guid Id, Guid? ProdutoId, string NomeProduto,
    decimal Quantidade, decimal PrecoUnitario, decimal Subtotal,
    Guid? ServicoId = null
);

public record VendaDto(
    Guid Id,
    Guid? ClienteId, string? NomeCliente,
    decimal Total, decimal Desconto, decimal TotalFinal,
    string FormaPagamento, string? FormasPagamento,
    decimal? Troco,
    DateTime CriadaEm,
    List<ItemVendaDto> Itens
);

// ── Estoque ───────────────────────────────────────────────────────
public record AjusteEstoqueRequest(
    Guid ProdutoId, decimal Quantidade,
    string Tipo, // entrada | ajuste
    string? Observacao
);

public record MovimentoDto(
    Guid Id, Guid ProdutoId, string NomeProduto,
    string Tipo, decimal Quantidade,
    string? Observacao, DateTime CriadoEm
);

// ── Relatórios ────────────────────────────────────────────────────
public record ProdutoRankingDto(
    Guid Id, string Nome, string Categoria,
    decimal QtdVendida, decimal Receita, decimal LucroEstimado
);

public record FluxoDiaDto(string Data, int QtdVendas, decimal Entradas, decimal Descontos);
public record FluxoMesDto(int Mes, string NomeMes, int QtdVendas, decimal Entradas, decimal Descontos);

public record ResumoVendasDto(
    decimal TotalVendido, decimal TotalDescontos,
    decimal TicketMedio, decimal TotalItensVendidos,
    int TotalVendas
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
    string? OutroCampo, int Estoque,
    int EstoqueMinimo, bool Ativo
);

public record SalvarVariacaoRequest(
    string? Tamanho, string? Cor,
    string? OutroCampo, int Estoque,
    int EstoqueMinimo
);

public record AjusteVariacaoRequest(
    Guid ProdutoId, Guid VariacaoId,
    int Quantidade, string Tipo,
    string? Observacao
);

// ── Trocas ────────────────────────────────────────────────────────
public record ItemTrocaRequest(
    Guid ProdutoId, string NomeProduto,
    Guid? VariacaoId, int Quantidade,
    decimal PrecoUnitario, bool VoltaEstoque
);

public record CriarTrocaRequest(
    Guid ClienteId,
    List<ItemTrocaRequest> Devolvidos,
    List<ItemTrocaRequest> Novos,
    string? FormaPagamento
);

// ── Categoriias ────────────────────────────────────────────────────────
public record CriarCategoriaRequest(
    string Nome,
    string TipoTamanho,        // letra | numero | personalizado
    bool UsaTamanho,
    bool UsaCor,
    string? TamanhosPersonalizados
);