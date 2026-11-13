namespace LojaApi.Application.Clientes;

public record SalvarClienteRequest(
    string Nome, string Telefone,
    string? Cpf, string? Email,
    string? Endereco, string? Observacoes,
    DateTime? DataNascimento
);
