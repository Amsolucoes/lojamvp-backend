using LojaApi.Models;
using LojaApi.src.Models;

namespace LojaApi.Application.Plataforma;

// Acesso aos dados da Plataforma (usuários, lojas, perfis, preços dos módulos, faturas e push). Os handlers decidem as regras;
// aqui só se lê e grava. Implementado em Infrastructure/Plataforma (EF Core).
public interface IPlataformaRepository
{
    // ── Usuários ──
    Task<Usuario?> ObterUsuarioAtivoPorEmailAsync(string email);                                   // sem diferenciar maiúscula
    Task<Usuario?> ObterUsuarioAsync(Guid id);
    Task<bool> EmailDeUsuarioExisteAsync(string email, Guid? ignorarUsuarioId);                   // sem diferenciar maiúscula
    Task<UsuarioLoja?> ObterVinculoAtivoAsync(Guid usuarioId);
    void Adicionar(Usuario usuario);
    void Adicionar(UsuarioLoja vinculo);

    // ── Lojas ──
    Task<Loja?> ObterLojaAsync(Guid id);
    Task<Loja?> ObterMinhaLojaComPagamentosAsync(Guid usuarioId);                                  // a loja do vínculo ativo, com as faturas
    Task<bool> EmailDeLojaExisteAsync(string email, Guid? ignorarLojaId = null);                  // sem diferenciar maiúscula
    Task<bool> SlugEmUsoAsync(string slug, Guid ignorarLojaId);
    Task<int> ContarLojasDaPromocaoAsync();                                                        // fora as de teste e as de financeiro puro
    void Adicionar(Loja loja);
    void Adicionar(Pagamento pagamento);

    // ── Faturas (mensalidade) ──
    Task<Pagamento?> ObterPagamentoAsync(Guid id);
    Task<Pagamento?> ObterPagamentoComLojaAsync(Guid id);
    Task<Pagamento?> ObterPagamentoDaCobrancaAsync(string mpPaymentId);                            // pelo id da cobrança no Mercado Pago, com a loja
    Task<bool> PagamentoDoMpJaPagoAsync(string mpPaymentId);
    Task<Pagamento?> ObterFaturaEmAbertoAsync(Guid lojaId);                                        // a primeira pendente ou atrasada
    Task<int> ContarFaturasPagasAsync(Guid lojaId);
    Task<Loja?> ObterLojaPorAssinaturaAsync(string preapprovalId);

    // ── Perfis ──
    Task<List<PerfilLoja>> ListarPerfisAtivosAsync();                                              // com categorias e campos extras por ordem
    Task<string?> ObterTipoPlanoDoPerfilAsync(Guid perfilId);
    Task<PerfilLoja?> ObterPerfilComCategoriasECamposAsync(Guid id);
    Task<PerfilLoja?> ObterPerfilComCategoriasEServicosAsync(Guid id);
    Task<PerfilLoja?> ObterPerfilParaEmailAsync(Guid id);                                          // só leitura, com categorias e serviços

    // ── Categorias e campos da loja ──
    Task<bool> LojaTemCategoriasAsync(Guid lojaId);
    Task RemoverCategoriasECamposDaLojaAsync(Guid lojaId);
    Task<List<CategoriaLojaDto>> ListarCategoriasAtivasAsync(Guid lojaId);                         // por ordem
    Task<List<CampoExtraLojaDto>> ListarCamposExtrasAtivosAsync(Guid lojaId);                      // por ordem
    void Adicionar(CategoriaLoja categoria);
    void Adicionar(CampoExtraLoja campo);
    void Adicionar(Servico servico);

    // ── Preços dos módulos ──
    Task<List<ModuloPreco>> ListarModulosPrecoAsync();                                             // por nome
    Task<ModuloPreco?> ObterModuloPrecoAsync(Guid id);
    Task<ModuloPreco?> ObterModuloPrecoPorChaveAsync(string chave);
    Task<List<ModuloPreco>> ListarModulosDisponiveisAsync(IEnumerable<string> chaves);             // só os "disponíveis para ativar"

    // ── Push ──
    Task<bool> AssinaturaPushExisteAsync(Guid usuarioId, string endpoint);
    Task<AssinaturaPush?> ObterAssinaturaPushPorEndpointAsync(string endpoint);
    Task<AssinaturaPush?> ObterAssinaturaPushDoUsuarioAsync(Guid usuarioId, string endpoint);
    Task<List<AssinaturaPush>> ListarAssinaturasPushDoUsuarioAsync(Guid usuarioId);
    void Adicionar(AssinaturaPush assinatura);
    void Remover(AssinaturaPush assinatura);

    Task SalvarAsync();
}
