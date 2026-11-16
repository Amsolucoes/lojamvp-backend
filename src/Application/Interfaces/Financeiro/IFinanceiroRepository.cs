using LojaApi.Models;

namespace LojaApi.Application.Financeiro;

// Acesso aos dados do Financeiro (contas e categorias). Os handlers decidem as regras; aqui só se lê e grava.
// Implementado em Infrastructure/Financeiro (EF Core).
public interface IFinanceiroRepository
{
    // ── Contas bancárias ──
    Task<List<ContaBancaria>> ListarContasAsync(Guid lojaId);                                      // só leitura
    Task<ContaBancaria?> ObterContaAsync(Guid id, Guid lojaId);
    // Saldo de cada conta: saldo inicial + lançamentos pagos que afetam o saldo + ajustes − faturas de cartão pagas − pagamentos antecipados
    Task<Dictionary<Guid, decimal>> CalcularSaldosAsync(Guid lojaId, List<ContaBancaria> contas);
    void Adicionar(ContaBancaria conta);
    void Adicionar(AjusteContaBancaria ajuste);

    // ── Categorias ──
    Task<List<CategoriaFinanceiraDto>> ListarCategoriasAtivasAsync(Guid lojaId, string? tipo);     // por nome; o tipo inclui as "ambos"
    Task<List<CategoriaFinanceira>> ListarCategoriasDaLojaAsync(Guid lojaId);                      // só leitura, ativas ou não
    Task<CategoriaFinanceira?> ObterCategoriaAsync(Guid id, Guid lojaId);
    Task<bool> CategoriaEmUsoAsync(Guid id);                                                       // usada em lançamento ou lançamento fixo
    Task<bool> LojaTemCategoriasAsync(Guid lojaId);
    void Adicionar(CategoriaFinanceira categoria);
    void Remover(CategoriaFinanceira categoria);

    Task SalvarAsync();
}
