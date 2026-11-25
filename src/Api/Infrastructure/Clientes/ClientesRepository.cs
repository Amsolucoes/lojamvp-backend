using LojaApi.Application.Clientes;
using LojaApi.Data;
using LojaApi.Domain.Carteira;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Clientes;

// Implementação EF Core do acesso aos dados de clientes.
public class ClientesRepository(AppDbContext db) : IClientesRepository
{
    public Task<List<ClienteDto>> ListarAsync(Guid? lojaId, string? busca)
    {
        var q = db.Clientes.AsQueryable();

        if (lojaId.HasValue)
            q = q.Where(c => c.LojaId == lojaId);

        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(c =>
                c.Nome.ToLower().Contains(busca.ToLower()) ||
                c.Telefone.Contains(busca) ||
                (c.Cpf != null && c.Cpf.Contains(busca)));

        return q.OrderBy(c => c.Nome)
            .Select(c => new ClienteDto(
                c.Id, c.Nome, c.Telefone, c.Cpf, c.Email,
                c.Endereco, c.Observacoes, c.CriadoEm,
                c.Vendas.Count, c.Vendas.Sum(v => v.TotalFinal),
                c.DataNascimento, c.CreditoLoja,
                db.MovimentosCarteira.Where(m => m.ClienteId == c.Id)
                    .Sum(m => m.Tipo == TipoMovimentoCarteira.Compra ? m.Valor : -m.Valor),
                c.LimiteCarteira
            )).ToListAsync();
    }

    public Task<Cliente?> ObterAsync(Guid id) => db.Clientes.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<(int Quantidade, decimal Total)> ResumoDeComprasAsync(Guid clienteId)
    {
        var compras = await db.Vendas.Where(v => v.ClienteId == clienteId).Select(v => v.TotalFinal).ToListAsync();
        return (compras.Count, compras.Sum());
    }

    public Task<bool> CpfJaCadastradoAsync(string cpf, Guid? lojaId, Guid? ignorarClienteId = null) =>
        db.Clientes.AnyAsync(c =>
            c.Cpf == cpf &&
            (!ignorarClienteId.HasValue || c.Id != ignorarClienteId.Value) &&
            (!lojaId.HasValue || c.LojaId == lojaId));

    public void Adicionar(Cliente cliente) => db.Clientes.Add(cliente);
    public void Remover(Cliente cliente) => db.Clientes.Remove(cliente);
    public Task SalvarAsync() => db.SaveChangesAsync();
}
