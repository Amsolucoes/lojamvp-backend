using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Vendas;

// Acesso aos dados de Vendas (vendas, trocas, caixa, origens e relatórios) e aos dados de outros módulos que a venda toca
// (produto, serviço, plano, agendamento, cliente, funcionário). Os handlers decidem as regras; aqui só se lê e grava.
// Implementado em Infrastructure/Vendas (EF Core).
public interface IVendasRepository
{
    // ── Vendas ──
    Task<List<Venda>> ListarVendasAsync(Guid? lojaId, DateTime? de, DateTime? ate, Guid? clienteId);   // com cliente, itens e produto; mais nova primeiro
    Task<Venda?> ObterVendaCompletaAsync(Guid id);                                                    // com cliente, itens e produto
    Task<Venda?> ObterVendaComItensAsync(Guid id);
    Task<bool> TemNotaFiscalAutorizadaAsync(Guid vendaId);
    Task<ComissaoFuncionario?> ObterComissaoDaVendaAsync(Guid vendaId);
    Task<List<Agendamento>> ListarAgendamentosDaVendaAsync(Guid vendaId);
    Task RemoverConsumosDePlanoDaVendaAsync(Guid vendaId);                                           // executa na hora, fora do SalvarAsync
    void Adicionar(Venda venda);
    void Adicionar(ItemVenda item);
    void Adicionar(ConsumoPlano consumo);
    void Adicionar(MovimentoEstoque movimento);
    void Adicionar(ComissaoFuncionario comissao);
    void Remover(Venda venda);
    void Remover(ComissaoFuncionario comissao);
    void RemoverItens(IEnumerable<ItemVenda> itens);

    // ── Dados de outros módulos usados na venda e na troca (quando há loja, só o que é dela) ──
    Task<Produto?> ObterProdutoAsync(Guid id, Guid? lojaId);
    Task<ProdutoVariacao?> ObterVariacaoAsync(Guid id, Guid? lojaId);
    Task<Servico?> ObterServicoAsync(Guid id, Guid? lojaId);
    Task<Cliente?> ObterClienteAsync(Guid id, Guid? lojaId);
    Task<AssinaturaCliente?> ObterAssinaturaAsync(Guid id, Guid? lojaId);
    Task<Plano?> ObterPlanoAsync(Guid id);
    Task<int> ContarConsumosDeAssinaturaAsync(Guid assinaturaId, DateTime inicioUtc, DateTime fimUtc);
    Task<Agendamento?> ObterAgendamentoAsync(Guid id);
    Task<Profissional?> ObterFuncionarioDaLojaAsync(Guid id, Guid? lojaId);

    // ── Trocas ──
    Task<List<TrocaListaDto>> ListarTrocasAsync(Guid? lojaId);                                       // as 50 mais novas
    void Adicionar(Troca troca);
    void Adicionar(ItemTroca item);

    // ── Caixa ──
    Task<List<MovimentoCaixaDto>> ListarMovimentosCaixaAsync(Guid lojaId, DateTime? deUtc, DateTime? ateUtc);
    Task<MovimentoCaixa?> ObterMovimentoCaixaAsync(Guid id, Guid? lojaId);
    Task<ContaBancaria?> ObterContaBancariaAsync(Guid id, Guid? lojaId);
    Task<AjusteContaBancaria?> ObterAjusteDaContaAsync(Guid id);
    void Adicionar(MovimentoCaixa movimento);
    void Remover(MovimentoCaixa movimento);
    void Adicionar(AjusteContaBancaria ajuste);
    void Remover(AjusteContaBancaria ajuste);

    // ── Origens de venda ──
    Task<List<OrigemVendaResumoDto>> ListarOrigensAtivasAsync(Guid lojaId);
    Task<List<OrigemVenda>> ListarOrigensAsync(Guid lojaId);
    Task<OrigemVenda?> ObterOrigemAsync(Guid id, Guid? lojaId);
    Task<bool> OrigemComNomeExisteAsync(Guid? lojaId, string nome);
    Task<bool> OrigemEmUsoAsync(Guid id);
    Task<bool> LojaTemOrigensAsync(Guid? lojaId);
    void Adicionar(OrigemVenda origem);
    void Remover(OrigemVenda origem);

    // ── Relatórios ──
    Task<List<Venda>> ListarVendasComItensDoPeriodoAsync(Guid? lojaId, DateTime? de, DateTime? ate);
    Task<List<ItemVenda>> ListarItensVendidosAsync(Guid? lojaId, DateTime? de, DateTime? ate);       // com o produto
    Task<List<Venda>> ListarVendasEntreAsync(Guid? lojaId, DateTime deInclusivo, DateTime ateExclusivo);

    Task SalvarAsync();
}
