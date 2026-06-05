namespace LojaApi.DTOs;

// ── Auth ──────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Senha);
public record LoginResponse(string Token, string Nome, string Email, string Role);

// ── Usuario ───────────────────────────────────────────────────────
public record UsuarioDto(Guid Id, string Nome, string Email, string Role, bool Ativo);
public record CriarUsuarioRequest(string Nome, string Email, string Senha, string Role);
public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);

// ── Produto ───────────────────────────────────────────────────────
public record ProdutoDto(
    Guid Id, string Nome, string? Descricao, string Categoria,
    decimal PrecoCusto, decimal PrecoVenda,
    int Estoque, int EstoqueMinimo,
    string? CodigoBarras, bool Ativo,
    DateTime CriadoEm, DateTime AtualizadoEm
);

public record SalvarProdutoRequest(
    string Nome, string? Descricao, string Categoria,
    decimal PrecoCusto, decimal PrecoVenda,
    int Estoque, int EstoqueMinimo,
    string? CodigoBarras, bool Ativo
);

// ── Cliente ───────────────────────────────────────────────────────
public record ClienteDto(
    Guid Id, string Nome, string Telefone,
    string? Cpf, string? Email, string? Endereco,
    string? Observacoes, DateTime CriadoEm,
    int TotalCompras, decimal TotalGasto
);

public record SalvarClienteRequest(
    string Nome, string Telefone,
    string? Cpf, string? Email,
    string? Endereco, string? Observacoes
);

// ── Venda ─────────────────────────────────────────────────────────
public record ItemVendaRequest(
    Guid ProdutoId, int Quantidade, decimal PrecoUnitario
);

public record CriarVendaRequest(
    List<ItemVendaRequest> Itens,
    Guid? ClienteId,
    decimal Desconto,
    string FormaPagamento,
    decimal? Troco
);

public record ItemVendaDto(
    Guid Id, Guid ProdutoId, string NomeProduto,
    int Quantidade, decimal PrecoUnitario, decimal Subtotal
);

public record VendaDto(
    Guid Id,
    Guid? ClienteId, string? NomeCliente,
    decimal Total, decimal Desconto, decimal TotalFinal,
    string FormaPagamento, decimal? Troco,
    DateTime CriadaEm,
    List<ItemVendaDto> Itens
);

// ── Estoque ───────────────────────────────────────────────────────
public record AjusteEstoqueRequest(
    Guid ProdutoId, int Quantidade,
    string Tipo, // entrada | ajuste
    string? Observacao
);

public record MovimentoDto(
    Guid Id, Guid ProdutoId, string NomeProduto,
    string Tipo, int Quantidade,
    string? Observacao, DateTime CriadoEm
);

// ── Relatórios ────────────────────────────────────────────────────
public record ProdutoRankingDto(
    Guid Id, string Nome, string Categoria,
    int QtdVendida, decimal Receita, decimal LucroEstimado
);

public record FluxoDiaDto(string Data, int QtdVendas, decimal Entradas, decimal Descontos);
public record FluxoMesDto(int Mes, string NomeMes, int QtdVendas, decimal Entradas, decimal Descontos);

public record ResumoVendasDto(
    decimal TotalVendido, decimal TotalDescontos,
    decimal TicketMedio, int TotalItensVendidos,
    int TotalVendas
);
