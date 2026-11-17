using LojaApi.Models;

namespace LojaApi.Application.Financeiro;

// Acesso aos dados do Planejamento (configuração, tetos por categoria e metas). Os handlers decidem as regras; aqui só se lê e grava.
// Implementado em Infrastructure/Financeiro (EF Core), na mesma unidade de trabalho dos outros repositórios do Financeiro.
public interface IPlanejamentoRepository
{
    Task<PlanejamentoFinanceiro?> ObterPlanejamentoAsync(Guid lojaId);
    void Adicionar(PlanejamentoFinanceiro planejamento);

    Task<List<Guid>> ListarIdsDeCategoriasDaLojaAsync(Guid lojaId);                // ativas ou não
    Task<List<PlanejamentoCategoria>> ListarTetosAsync(Guid lojaId);
    void Adicionar(PlanejamentoCategoria teto);

    Task<MetaPlanejamento?> ObterMetaAsync(Guid id, Guid lojaId);
    void Adicionar(MetaPlanejamento meta);
    void Remover(MetaPlanejamento meta);

    Task SalvarAsync();
}
