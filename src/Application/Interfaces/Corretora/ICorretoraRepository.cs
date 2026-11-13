using LojaApi.Models;
using LojaApi.src.Models;

namespace LojaApi.Application.Corretora;

// Acesso aos dados da corretora. Implementado em Infrastructure/Corretora (EF Core).
public interface ICorretoraRepository
{
    // ── Seguradoras ──
    Task<List<SeguradoraDto>> ListarSeguradorasAsync(Guid lojaId, bool apenasAtivas);
    Task<Seguradora?> ObterSeguradoraAsync(Guid id, Guid? lojaId);
    Task<bool> SeguradoraDaLojaExisteAsync(Guid id, Guid lojaId);
    Task<bool> SeguradoraEmUsoAsync(Guid id);
    void Adicionar(Seguradora seguradora);
    void Remover(Seguradora seguradora);

    // ── Funil ──
    Task<List<OportunidadeDto>> ListarOportunidadesAsync(Guid lojaId);
    Task<int> MaiorOrdemNaEtapaAsync(Guid lojaId, string etapa);
    Task<Oportunidade?> ObterOportunidadeAsync(Guid id, Guid? lojaId);
    Task<List<OportunidadeAbertaDto>> ListarOportunidadesEmAbertoDoClienteAsync(Guid lojaId, Guid clienteId);
    Task<List<Oportunidade>> ListarOportunidadesCriadasDesdeAsync(Guid lojaId, DateTime desde);
    Task<List<FunilEtapaDto>> ContarOportunidadesPorEtapaAsync(Guid lojaId);
    void Adicionar(Oportunidade oportunidade);
    void Remover(Oportunidade oportunidade);

    // ── Apólices ──
    Task<List<ApoliceDto>> ListarApolicesAsync(Guid lojaId, DateTime hoje);
    Task<List<Apolice>> ListarApolicesNaoCanceladasAsync(Guid lojaId);
    Task<Apolice?> ObterApoliceAsync(Guid id, Guid? lojaId);
    void Adicionar(Apolice apolice);

    // ── Vínculos que precisam ser da mesma loja ──
    Task<Cliente?> ObterClienteDaLojaAsync(Guid clienteId, Guid lojaId);
    Task<bool> ContaBancariaDaLojaExisteAsync(Guid contaId, Guid lojaId);

    void Adicionar(LancamentoFinanceiro lancamento);
    Task SalvarAsync();
}
