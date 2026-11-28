using LojaApi.Application.Carteira;
using LojaApi.Data;
using LojaApi.Domain.Carteira;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Carteira;

// Implementação EF Core do acesso aos dados da carteira do cliente.
public class CarteiraRepository(AppDbContext db) : ICarteiraRepository
{
    public Task<Cliente?> ObterClienteAsync(Guid lojaId, Guid clienteId) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId && c.LojaId == lojaId);

    public Task<decimal> SaldoAsync(Guid lojaId, Guid clienteId) =>
        db.MovimentosCarteira
            .Where(m => m.LojaId == lojaId && m.ClienteId == clienteId)
            .SumAsync(m => m.Tipo == TipoMovimentoCarteira.Compra ? m.Valor : -m.Valor);

    public Task<decimal> SaldoPorClienteAsync(Guid clienteId) =>
        db.MovimentosCarteira
            .Where(m => m.ClienteId == clienteId)
            .SumAsync(m => m.Tipo == TipoMovimentoCarteira.Compra ? m.Valor : -m.Valor);

    public Task<MovimentoCarteira?> ObterCompraDaVendaAsync(Guid lojaId, Guid vendaId) =>
        db.MovimentosCarteira.FirstOrDefaultAsync(m => m.LojaId == lojaId && m.VendaId == vendaId);

    public Task<MovimentoCarteira?> ObterPagamentoAsync(Guid lojaId, Guid clienteId, Guid movimentoId) =>
        db.MovimentosCarteira.FirstOrDefaultAsync(m =>
            m.Id == movimentoId && m.LojaId == lojaId && m.ClienteId == clienteId && m.Tipo == TipoMovimentoCarteira.Pagamento);

    public Task<List<MovimentoCarteira>> ListarMovimentosDoClienteAsync(Guid lojaId, Guid clienteId) =>
        db.MovimentosCarteira.AsNoTracking()
            .Where(m => m.LojaId == lojaId && m.ClienteId == clienteId)
            .ToListAsync();

    public Task<List<MovimentoCarteira>> ListarMovimentosDaLojaAsync(Guid lojaId) =>
        db.MovimentosCarteira.AsNoTracking().Where(m => m.LojaId == lojaId).ToListAsync();

    public async Task<List<DevedorDaCarteira>> ListarDevedoresAsync(Guid lojaId)
    {
        var saldos = await db.MovimentosCarteira
            .Where(m => m.LojaId == lojaId)
            .GroupBy(m => m.ClienteId)
            .Select(g => new { ClienteId = g.Key, Saldo = g.Sum(m => m.Tipo == TipoMovimentoCarteira.Compra ? m.Valor : -m.Valor) })
            .Where(x => x.Saldo > 0)
            .ToListAsync();

        var ids = saldos.Select(s => s.ClienteId).ToList();
        var clientes = await db.Clientes.AsNoTracking()
            .Where(c => c.LojaId == lojaId && ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Nome, c.Telefone, c.LimiteCarteira })
            .ToListAsync();

        return saldos
            .Join(clientes, s => s.ClienteId, c => c.Id, (s, c) => new DevedorDaCarteira(c.Id, c.Nome, c.Telefone, s.Saldo, c.LimiteCarteira))
            .ToList();
    }

    public Task<Dictionary<Guid, string>> NomesDosClientesComMovimentoAsync(Guid lojaId) =>
        db.Clientes.AsNoTracking()
            .Where(c => c.LojaId == lojaId && db.MovimentosCarteira.Any(m => m.ClienteId == c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Nome);

    public Task<ContaBancaria?> ObterContaAsync(Guid lojaId, Guid contaId) =>
        db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == contaId && c.LojaId == lojaId);

    public Task<MovimentoCaixa?> ObterEntradaDeCaixaAsync(Guid lojaId, Guid movimentoCaixaId) =>
        db.MovimentosCaixa.FirstOrDefaultAsync(m => m.Id == movimentoCaixaId && m.LojaId == lojaId);

    public Task ExcluirAjusteDeContaAsync(Guid lojaId, Guid ajusteId) =>
        db.AjustesContaBancaria.Where(a => a.Id == ajusteId && a.LojaId == lojaId).ExecuteDeleteAsync();

    public void Adicionar(MovimentoCarteira movimento) => db.MovimentosCarteira.Add(movimento);
    public void Remover(MovimentoCarteira movimento) => db.MovimentosCarteira.Remove(movimento);
    public void Adicionar(MovimentoCaixa entrada) => db.MovimentosCaixa.Add(entrada);
    public void Remover(MovimentoCaixa entrada) => db.MovimentosCaixa.Remove(entrada);
    public void Adicionar(AjusteContaBancaria ajuste) => db.AjustesContaBancaria.Add(ajuste);
    public Task SalvarAsync() => db.SaveChangesAsync();
}
