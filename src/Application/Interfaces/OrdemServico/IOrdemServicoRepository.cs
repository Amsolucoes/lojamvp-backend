using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Application.OrdemServico;

// Acesso aos dados de OrdemServico (checklist, orçamentos/ordens e patrimônio) e aos dados de outros módulos que a ordem toca
// (cliente, mecânico, produto, serviço, conta bancária, lançamento financeiro, comissão). Os handlers decidem as regras; aqui só se lê e grava.
// Implementado em Infrastructure/OrdemServico (EF Core).
public interface IOrdemServicoRepository
{
    // ── Checklist ──
    Task<List<ChecklistCategoriaDto>> ListarChecklistAsync(Guid lojaId);                          // categorias por ordem, com os itens por ordem
    Task<ChecklistCategoria?> ObterCategoriaAsync(Guid id, Guid? lojaId);
    Task<ChecklistCategoria?> ObterCategoriaComItensAsync(Guid id, Guid? lojaId);
    Task<ChecklistItem?> ObterItemChecklistAsync(Guid id, Guid? lojaId);
    Task<ChecklistItem?> ObterItemChecklistDeQualquerLojaAsync(Guid id);                            // só para conferir de quem é
    Task<bool> ItensDeChecklistEmUsoAsync(IEnumerable<Guid> itemIds);
    Task<bool> ItemDeChecklistEmUsoAsync(Guid itemId);
    void Adicionar(ChecklistCategoria categoria);
    void Remover(ChecklistCategoria categoria);
    void Adicionar(ChecklistItem item);
    void Remover(ChecklistItem item);

    // ── Orçamentos e ordens ──
    Task<List<OrcamentoListaDto>> ListarOrcamentosAsync(Guid lojaId, string? status, string? placaBusca);
    Task<OrcamentoServico?> ObterOrcamentoDetalheAsync(Guid id, Guid? lojaId);                     // com itens, mecânicos (e nome) e checklist (e nome do item)
    Task<List<OrcamentoDoClienteDto>> ListarOrcamentosDoClienteAsync(Guid lojaId, Guid clienteId);
    Task<List<ResumoClienteOrdemDto>> ResumoPorClienteAsync(Guid lojaId);
    Task<OrcamentoServico?> ObterOrcamentoAsync(Guid id, Guid? lojaId);
    Task<OrcamentoServico?> ObterOrcamentoParaEdicaoAsync(Guid id, Guid? lojaId);                  // com itens, mecânicos e respostas do checklist
    Task<OrcamentoServico?> ObterOrcamentoComItensAsync(Guid id, Guid? lojaId);
    Task<OrcamentoServico?> ObterOrcamentoParaConclusaoAsync(Guid id, Guid? lojaId);               // com itens e mecânicos (com o cadastro do profissional)
    void Adicionar(OrcamentoServico orcamento);
    void Remover(OrcamentoServico orcamento);
    void Adicionar(ItemOrcamentoServico item);
    void Adicionar(MecanicoOrcamento mecanico);
    void Adicionar(ChecklistRespostaItem resposta);
    void RemoverItens(IEnumerable<ItemOrcamentoServico> itens);
    void RemoverMecanicos(IEnumerable<MecanicoOrcamento> mecanicos);
    void RemoverRespostas(IEnumerable<ChecklistRespostaItem> respostas);

    // ── Dados de outros módulos (quando há loja, só o que é dela) ──
    Task<Cliente?> ObterClienteAsync(Guid id, Guid? lojaId);
    Task<Profissional?> ObterMecanicoAsync(Guid id, Guid? lojaId);
    Task<Produto?> ObterProdutoAsync(Guid id, Guid? lojaId);
    Task<Servico?> ObterServicoAsync(Guid id, Guid? lojaId);
    Task<ContaBancaria?> ObterContaBancariaAsync(Guid id, Guid? lojaId);
    Task<CategoriaFinanceira?> ObterCategoriaFinanceiraAsync(Guid lojaId, string nome);
    Task<LancamentoFinanceiro?> ObterLancamentoAsync(Guid id);
    Task<List<ComissaoFuncionario>> ListarComissoesDaOrdemAsync(Guid orcamentoId);
    Task<List<ComissaoServicoProfissional>> ListarExcecoesDeComissaoAsync(IEnumerable<Guid> servicoIds, IEnumerable<Guid> profissionalIds);
    void Adicionar(MovimentoEstoque movimento);
    void Adicionar(CategoriaFinanceira categoria);
    void Adicionar(LancamentoFinanceiro lancamento);
    void Remover(LancamentoFinanceiro lancamento);
    void Adicionar(ComissaoFuncionario comissao);
    void RemoverComissoes(IEnumerable<ComissaoFuncionario> comissoes);

    // ── Patrimônio ──
    Task<List<ItemPatrimonioListaDto>> ListarItensPatrimonioAsync(Guid lojaId, bool incluirInativos);
    Task<ItemPatrimonio?> ObterItemPatrimonioAsync(Guid id, Guid? lojaId);
    Task<List<ItemPatrimonio>> ListarItensPatrimonioPorIdsAsync(IEnumerable<Guid> ids, Guid lojaId);
    Task<List<ItemPatrimonio>> ListarItensAtivosDePatrimonioAsync(Guid lojaId);                    // por categoria e nome
    Task<bool> ItemPatrimonioJaContadoAsync(Guid itemId);
    void Adicionar(ItemPatrimonio item);
    void Remover(ItemPatrimonio item);
    Task<List<ContagemListaDto>> ListarContagensAsync(Guid lojaId);
    Task<ContagemPatrimonio?> ObterContagemAsync(Guid id, Guid? lojaId);
    Task<ContagemPatrimonio?> ObterContagemCompletaAsync(Guid id, Guid? lojaId);                   // com itens e o cadastro de cada item
    void Adicionar(ContagemPatrimonio contagem);
    void Remover(ContagemPatrimonio contagem);
    Task<string?> ObterNomeDaLojaAsync(Guid lojaId);

    Task SalvarAsync();
}
