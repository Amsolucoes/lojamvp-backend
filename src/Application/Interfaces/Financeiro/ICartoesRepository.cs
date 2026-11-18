using LojaApi.Models;

namespace LojaApi.Application.Financeiro;

// Acesso aos dados dos cartões de crédito, das compras e dos fixos do cartão. Os handlers decidem as regras; aqui só se lê e grava.
// Implementado em Infrastructure/Financeiro (EF Core). Ao salvar, vale a mesma unidade de trabalho dos outros repositórios do Financeiro.
public interface ICartoesRepository
{
    // ── Leituras prontas para a API ──
    Task<List<CartaoDto>> ListarCartoesAsync(Guid lojaId);
    Task<List<CartaoFixoDto>> ListarFixosDoCartaoAsync(Guid cartaoId, Guid lojaId);
    Task<List<string>> ListarDescricoesRecentesAsync(Guid lojaId);                                  // distintas, da mais nova, até 50

    // ── Cartão ──
    Task<CartaoCredito?> ObterCartaoAsync(Guid id, Guid lojaId);
    Task<List<CartaoCredito>> ListarAtivosAsync(Guid lojaId);                                        // sem rastrear
    void Adicionar(CartaoCredito cartao);

    // ── Compras ──
    Task<LancamentoCartao?> ObterCompraAsync(Guid id, Guid lojaId);                                 // com o cartão
    Task<LancamentoCartao?> ObterCompraDaLojaAsync(Guid id, Guid lojaId);                           // pela loja da própria compra
    Task<List<LancamentoCartao>> ListarComprasDoFixoAsync(Guid fixoId, Guid lojaId);                // todas, de qualquer mês
    Task<List<LancamentoCartao>> ListarGrupoDeComprasAsync(Guid grupoId, Guid lojaId);
    Task<List<LancamentoCartao>> ListarComprasDoFixoAPartirDeAsync(Guid fixoId, Guid lojaId, DateTime desde);
    Task<HashSet<(int Ano, int Mes)>> ListarMesesDeFaturasPagasAsync(Guid cartaoId);
    void Adicionar(LancamentoCartao compra);
    void Remover(LancamentoCartao compra);
    void Remover(IEnumerable<LancamentoCartao> compras);

    // ── Fixos do cartão ──
    Task<CartaoLancamentoFixo?> ObterFixoAsync(Guid id, Guid lojaId);                               // com o cartão
    Task<CartaoLancamentoFixo?> ObterFixoPorLojaAsync(Guid id, Guid lojaId);
    void Adicionar(CartaoLancamentoFixo fixo);

    Task SalvarAsync();
}

// Gera e limpa as compras mensais de um fixo do cartão (hoje o FinanceiroService; ver Infrastructure/Financeiro).
public interface IGeradorDeComprasFixas
{
    Task GerarLoteAsync(CartaoLancamentoFixo fixo, CartaoCredito cartao, DateTime desde);
    Task LimparFuturosAsync(Guid cartaoFixoId);
}
