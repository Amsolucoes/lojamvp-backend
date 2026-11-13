using LojaApi.Models;

namespace LojaApi.Application.Clientes;

// Acesso aos dados do cadastro de clientes. Implementado em Infrastructure/Clientes (EF Core).
public interface IClientesRepository
{
    // Lista já no formato da tela (com total de compras e saldo da carteira). Sem lojaId, traz de todas as lojas.
    Task<List<ClienteDto>> ListarAsync(Guid? lojaId, string? busca);

    Task<Cliente?> ObterAsync(Guid id);

    // Quantas compras e quanto o cliente já gastou
    Task<(int Quantidade, decimal Total)> ResumoDeComprasAsync(Guid clienteId);

    // Já existe outro cliente com esse CPF (na loja, ou em qualquer loja se lojaId for nulo)?
    Task<bool> CpfJaCadastradoAsync(string cpf, Guid? lojaId, Guid? ignorarClienteId = null);

    void Adicionar(Cliente cliente);
    void Remover(Cliente cliente);
    Task SalvarAsync();
}
