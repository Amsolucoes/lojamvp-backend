using LojaApi.Application.Carteira;
using LojaApi.Domain.Carteira;
using LojaApi.Models;

namespace LojaApi.Tests.Suporte.Falsos;

// Repositório da carteira em memória: permite testar o SERVICE (as regras) sem banco.
// "Salvamentos" conta quantas vezes o service mandou gravar (zero = nada foi gravado).
public sealed class RepositorioCarteiraFalso : ICarteiraRepository
{
    public List<Cliente> Clientes { get; } = [];
    public List<MovimentoCarteira> Movimentos { get; } = [];
    public List<ContaBancaria> Contas { get; } = [];
    public List<MovimentoCaixa> Entradas { get; } = [];
    public List<AjusteContaBancaria> Ajustes { get; } = [];
    public List<Guid> AjustesExcluidos { get; } = [];
    public int Salvamentos { get; private set; }

    public Task<Cliente?> ObterClienteAsync(Guid lojaId, Guid clienteId) =>
        Task.FromResult(Clientes.FirstOrDefault(c => c.Id == clienteId && c.LojaId == lojaId));

    public Task<decimal> SaldoAsync(Guid lojaId, Guid clienteId) =>
        Task.FromResult(Movimentos.Where(m => m.LojaId == lojaId && m.ClienteId == clienteId).Sum(m => m.EhCompra() ? m.Valor : -m.Valor));

    public Task<decimal> SaldoPorClienteAsync(Guid clienteId) =>
        Task.FromResult(Movimentos.Where(m => m.ClienteId == clienteId).Sum(m => m.EhCompra() ? m.Valor : -m.Valor));

    public Task<MovimentoCarteira?> ObterCompraDaVendaAsync(Guid lojaId, Guid vendaId) =>
        Task.FromResult(Movimentos.FirstOrDefault(m => m.LojaId == lojaId && m.VendaId == vendaId));

    public Task<MovimentoCarteira?> ObterPagamentoAsync(Guid lojaId, Guid clienteId, Guid movimentoId) =>
        Task.FromResult(Movimentos.FirstOrDefault(m => m.Id == movimentoId && m.LojaId == lojaId && m.ClienteId == clienteId && !m.EhCompra()));

    public Task<List<MovimentoCarteira>> ListarMovimentosDoClienteAsync(Guid lojaId, Guid clienteId) =>
        Task.FromResult(Movimentos.Where(m => m.LojaId == lojaId && m.ClienteId == clienteId).ToList());

    public Task<List<MovimentoCarteira>> ListarMovimentosDaLojaAsync(Guid lojaId) =>
        Task.FromResult(Movimentos.Where(m => m.LojaId == lojaId).ToList());

    public Task<List<DevedorDaCarteira>> ListarDevedoresAsync(Guid lojaId) => Task.FromResult(
        Movimentos.Where(m => m.LojaId == lojaId).GroupBy(m => m.ClienteId)
            .Select(g => (ClienteId: g.Key, Saldo: g.Sum(m => m.EhCompra() ? m.Valor : -m.Valor)))
            .Where(x => x.Saldo > 0)
            .Join(Clientes.Where(c => c.LojaId == lojaId), x => x.ClienteId, c => c.Id,
                (x, c) => new DevedorDaCarteira(c.Id, c.Nome, c.Telefone, x.Saldo, c.LimiteCarteira))
            .ToList());

    public Task<Dictionary<Guid, string>> NomesDosClientesComMovimentoAsync(Guid lojaId) => Task.FromResult(
        Clientes.Where(c => c.LojaId == lojaId && Movimentos.Any(m => m.ClienteId == c.Id)).ToDictionary(c => c.Id, c => c.Nome));

    public Task<ContaBancaria?> ObterContaAsync(Guid lojaId, Guid contaId) =>
        Task.FromResult(Contas.FirstOrDefault(c => c.Id == contaId && c.LojaId == lojaId));

    public Task<MovimentoCaixa?> ObterEntradaDeCaixaAsync(Guid lojaId, Guid movimentoCaixaId) =>
        Task.FromResult(Entradas.FirstOrDefault(m => m.Id == movimentoCaixaId && m.LojaId == lojaId));

    public Task ExcluirAjusteDeContaAsync(Guid lojaId, Guid ajusteId)
    {
        AjustesExcluidos.Add(ajusteId);
        Ajustes.RemoveAll(a => a.Id == ajusteId && a.LojaId == lojaId);
        return Task.CompletedTask;
    }

    public void Adicionar(MovimentoCarteira movimento) => Movimentos.Add(movimento);
    public void Remover(MovimentoCarteira movimento) => Movimentos.Remove(movimento);
    public void Adicionar(MovimentoCaixa entrada) => Entradas.Add(entrada);
    public void Remover(MovimentoCaixa entrada) => Entradas.Remove(entrada);
    public void Adicionar(AjusteContaBancaria ajuste) => Ajustes.Add(ajuste);
    public Task SalvarAsync() { Salvamentos++; return Task.CompletedTask; }
}
