namespace LojaApi.Application.Clientes;

public record ClienteDto(
    Guid Id, string Nome, string Telefone,
    string? Cpf, string? Email, string? Endereco,
    string? Observacoes, DateTime CriadoEm,
    int TotalCompras, decimal TotalGasto,
    DateTime? DataNascimento,
    decimal creditoLoja,
    decimal SaldoCarteira = 0,
    decimal? LimiteCarteira = null
);

public record SalvarClienteRequest(
    string Nome, string Telefone,
    string? Cpf, string? Email,
    string? Endereco, string? Observacoes,
    DateTime? DataNascimento
);
