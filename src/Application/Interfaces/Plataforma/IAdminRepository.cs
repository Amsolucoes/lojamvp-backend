using LojaApi.Models;
using LojaApi.src.Models;

namespace LojaApi.Application.Plataforma;

// Tudo que o backup da loja leva
public record DadosDeBackup(
    List<Produto> Produtos, List<Cliente> Clientes, List<Venda> Vendas, List<Troca> Trocas,
    List<MovimentoEstoque> Movimentos, List<CategoriaLoja> Categorias, List<Pagamento> Pagamentos);

// Filtro e página das faturas de todas as lojas (status: pago | pendente | atrasado; atrasada = pendente vencida ou marcada atrasado)
public record FiltroPagamentosAdmin(string? Status, Guid? LojaId, string? Busca, int? Ano, int? Mes, string? Forma, bool IncluirTeste, int Pagina, int Tamanho);
public record PaginaPagamentosAdmin(List<Pagamento> Itens, int Total, decimal Recebido, decimal EmAtraso, decimal AReceber);

// Acesso aos dados do painel do superadmin (EF Core em Infrastructure/Plataforma). Os handlers decidem as regras.
public interface IAdminRepository
{
    // ── Lojas ──
    Task<List<Loja>> ListarLojasReaisComPagamentosAsync();                                        // fora as de teste
    Task<List<Pagamento>> ListarUltimosPagamentosPagosAsync(int quantidade);                       // das lojas reais, com a loja, do mais recente
    Task<List<Loja>> ListarLojasAsync(StatusLoja? status, string? busca);                          // com faturas e usuários; da mais nova
    Task<Loja?> ObterLojaCompletaAsync(Guid id);                                                   // com faturas e usuários
    Task<bool> LojaExisteAsync(Guid id);
    Task<bool> CnpjDeLojaExisteAsync(string cnpj);
    Task<UsuarioLoja?> ObterVinculoAdminAsync(Guid lojaId, bool apenasAtivo);                      // o admin da loja, com o usuário
    Task<List<Loja>> ListarLojasNaoCanceladasAsync();

    // ── Faturas e comunicados ──
    Task<List<Pagamento>> ListarPagamentosDaLojaAsync(Guid lojaId);                                // com a loja, do vencimento mais novo
    Task<PaginaPagamentosAdmin> ListarPagamentosDeTodasAsLojasAsync(FiltroPagamentosAdmin filtro, DateTime agora);  // com a loja, do vencimento mais novo; totais ignoram o filtro de status
    Task<List<ComunicadoDto>> ListarComunicadosAsync();                                            // do mais recente

    // ── Backup e exclusão ──
    Task<DadosDeBackup> ObterDadosDeBackupAsync(Guid lojaId);
    Task<(bool Ok, string? Erro)> ExcluirLojaComTodosOsDadosAsync(Guid lojaId);                    // numa transação; grava na hora
    Task ExcluirProdutosEVendasAsync(Guid lojaId);                                                 // grava na hora
}
