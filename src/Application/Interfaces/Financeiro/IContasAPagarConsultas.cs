namespace LojaApi.Application.Financeiro;

// Consultas de leitura pesadas de Contas a Pagar e das faturas do mês: várias tabelas lidas de uma vez, em poucas consultas.
// Implementado em Infrastructure/Financeiro com Dapper (SQL direto), na mesma conexão e transação do AppDbContext.
public interface IContasAPagarConsultas
{
    Task<List<LancamentoAPagarDto>> ListarAPagarAsync(Guid lojaId, DateTime inicio, DateTime fim);                  // sem as parcelas de financiamento de cartão
    Task<List<CartaoAtivoDto>> ListarCartoesAtivosAsync(Guid lojaId);
    Task<List<ItemDeCartaoDto>> ListarItensDeCartaoAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate);       // [desde, ate)
    Task<List<CompraDeCartaoDto>> ListarComprasDeCartaoAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate);   // [desde, ate)
    Task<List<ParcelaDoCartaoDto>> ListarParcelasDeFinanciamentoAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate); // por vencimento [desde, ate)
    Task<List<ParcelaDoCartaoDto>> ListarParcelasDasFaturasAsync(List<Guid> faturaIds);                             // por número da parcela
    Task<List<FaturaLeveDto>> ListarFaturasAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate);               // pelo mês de referência [desde, ate)
    Task<Dictionary<Guid, decimal>> SomarAntecipadosPorFaturaAsync(List<Guid> faturaIds);
}
