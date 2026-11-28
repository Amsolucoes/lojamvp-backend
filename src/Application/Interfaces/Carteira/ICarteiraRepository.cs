using LojaApi.Domain.Carteira;
using LojaApi.Models;

namespace LojaApi.Application.Carteira;

// Cliente que está devendo, com o saldo da carteira e o limite (para cobrar).
public record DevedorDaCarteira(Guid ClienteId, string Nome, string Telefone, decimal Saldo, decimal? Limite);

// Acesso aos dados da carteira do cliente ("fiado"). Implementado em Infrastructure/Carteira (EF Core).
// As gravações (Adicionar/Remover) só marcam a mudança: quem salva é o chamador (SalvarAsync). Na venda, quem salva é o repository
// de Vendas, na mesma transação (os dois compartilham o mesmo contexto do banco).
public interface ICarteiraRepository
{
    Task<Cliente?> ObterClienteAsync(Guid lojaId, Guid clienteId);

    // Saldo = compras - pagamentos, sempre calculado a partir dos lançamentos
    Task<decimal> SaldoAsync(Guid lojaId, Guid clienteId);

    // Saldo pelo id do cliente (o id já é único em todas as lojas): listagem e exclusão de clientes
    Task<decimal> SaldoPorClienteAsync(Guid clienteId);

    Task<MovimentoCarteira?> ObterCompraDaVendaAsync(Guid lojaId, Guid vendaId);
    Task<MovimentoCarteira?> ObterPagamentoAsync(Guid lojaId, Guid clienteId, Guid movimentoId);

    // Só leitura (sem rastreamento), na ordem do banco: quem chama ordena
    Task<List<MovimentoCarteira>> ListarMovimentosDoClienteAsync(Guid lojaId, Guid clienteId);
    Task<List<MovimentoCarteira>> ListarMovimentosDaLojaAsync(Guid lojaId);

    // Clientes com saldo maior que zero, sem ordem definida
    Task<List<DevedorDaCarteira>> ListarDevedoresAsync(Guid lojaId);

    // Nome dos clientes da loja que têm algum lançamento na carteira
    Task<Dictionary<Guid, string>> NomesDosClientesComMovimentoAsync(Guid lojaId);

    // Recebimento: conta bancária da loja, entrada que ele criou no caixa e o ajuste espelhado na conta
    Task<ContaBancaria?> ObterContaAsync(Guid lojaId, Guid contaId);
    Task<MovimentoCaixa?> ObterEntradaDeCaixaAsync(Guid lojaId, Guid movimentoCaixaId);
    Task ExcluirAjusteDeContaAsync(Guid lojaId, Guid ajusteId);

    void Adicionar(MovimentoCarteira movimento);
    void Remover(MovimentoCarteira movimento);
    void Adicionar(MovimentoCaixa entrada);
    void Remover(MovimentoCaixa entrada);
    void Adicionar(AjusteContaBancaria ajuste);
    Task SalvarAsync();
}
