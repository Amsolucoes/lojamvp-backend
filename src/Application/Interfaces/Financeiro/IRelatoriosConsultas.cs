namespace LojaApi.Application.Financeiro;

// Consultas de leitura dos relatórios do Financeiro: várias tabelas lidas de uma vez, em poucas consultas.
// Implementado em Infrastructure/Financeiro com Dapper (SQL direto), na mesma conexão e transação do AppDbContext.
public interface IRelatoriosConsultas
{
    Task<List<LancamentoDoMesDto>> ListarLancamentosDoPeriodoAsync(Guid lojaId, DateTime inicio, DateTime fim);                // [inicio, fim), pagar e receber
    Task<List<PagamentoDePlanoDto>> ListarPagamentosDePlanoAsync(Guid lojaId, DateTime inicio, DateTime fim, bool apenasAssinaturasAtivas);
    Task<List<decimal>> ListarValoresDosPlanosAtivosAsync(Guid lojaId);                                                        // um valor por assinatura ativa
    Task<List<CartaoDoRelatorioDto>> ListarCartoesAtivosAsync(Guid lojaId);
    Task<List<CompraLeveDto>> ListarComprasLevesAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate);                    // [desde, ate)
    Task<List<CompraCategorizadaDto>> ListarComprasCategorizadasAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate);    // [desde, ate)
    Task<List<FaturaDoRelatorioDto>> ListarFaturasAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate);                  // pelo mês de referência [desde, ate)
    Task<List<FinanciamentoPendenteDto>> ListarFinanciamentoPendentePorCartaoAsync(List<Guid> cartaoIds);
    Task<List<LancamentoDetalheDto>> ListarAPagarDaCategoriaAsync(Guid lojaId, DateTime inicio, DateTime fim, string? categoria); // categoria nula = sem categoria
    Task<List<LancamentoPendenteDto>> ListarPendentesAteAsync(Guid lojaId, DateTime de, DateTime ate);                         // [de, ate] por vencimento
}
