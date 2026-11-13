using LojaApi.Models;
using LojaApi.src.Models.Fiscal;

namespace LojaApi.Application.Fiscal;

// Acesso aos dados da emissão fiscal. Implementado em Infrastructure/Fiscal (EF Core).
public interface IFiscalRepository
{
    Task<ConfiguracaoFiscal?> ObterConfiguracaoAsync(Guid lojaId);
    void Adicionar(ConfiguracaoFiscal configuracao);

    Task<Loja?> ObterLojaAsync(Guid lojaId);
    Task<Venda?> ObterVendaAsync(Guid vendaId, Guid lojaId);

    void Adicionar(EmissaoFiscal emissao);
    Task<EmissaoFiscal?> ObterEmissaoAsync(Guid emissaoId, Guid? lojaId);
    // Da mais nova para a mais antiga. Sem lojaId, de qualquer loja.
    Task<List<EmissaoFiscal>> ListarEmissoesDaVendaAsync(Guid vendaId, Guid? lojaId);

    Task SalvarAsync();
}
