using System.Data;
using Dapper;
using LojaApi.Application.Financeiro;
using LojaApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LojaApi.Infrastructure.Financeiro;

// Consultas de leitura pesadas de Contas a Pagar e das faturas do mês, com Dapper (SQL direto): em vez de várias consultas do EF por cartão e mês,
// cada dado é lido de uma vez para todos os cartões. Usa a mesma conexão e transação do AppDbContext. Só lê; as regras ficam nos handlers.
// As colunas ganham o nome da propriedade do DTO (o Dapper casa sem diferenciar maiúscula, mas sem o "_").
public class ContasAPagarConsultas(AppDbContext db) : IContasAPagarConsultas
{
    public Task<List<LancamentoAPagarDto>> ListarAPagarAsync(Guid lojaId, DateTime inicio, DateTime fim) => ConsultarAsync<LancamentoAPagarDto>("""
        SELECT l.id AS Id, l.descricao AS Descricao, l.observacao AS Observacao, c.nome AS CategoriaNome, l.categoria_id AS CategoriaId,
               l.conta_bancaria_id AS ContaBancariaId, l.modo AS Modo, l.avisar AS Avisar, l.valor AS Valor, l.vencimento AS Vencimento,
               l.status AS Status, l.pago_em AS PagoEm, l.forma_pagamento AS FormaPagamento, l.numero_parcela AS NumeroParcela, l.total_parcelas AS TotalParcelas
        FROM lancamentos_financeiros l
        LEFT JOIN categorias_financeiras c ON c.id = l.categoria_id
        WHERE l.loja_id = @LojaId AND l.tipo = 'pagar' AND l.vencimento >= @Inicio AND l.vencimento < @Fim AND l.cartao_origem_id IS NULL
        """, new { LojaId = lojaId, Inicio = inicio, Fim = fim });

    public Task<List<CartaoAtivoDto>> ListarCartoesAtivosAsync(Guid lojaId) => ConsultarAsync<CartaoAtivoDto>("""
        SELECT id AS Id, nome AS Nome, dia_fechamento AS DiaFechamento, dia_vencimento AS DiaVencimento
        FROM cartoes_credito
        WHERE loja_id = @LojaId AND ativo
        """, new { LojaId = lojaId });

    public Task<List<ItemDeCartaoDto>> ListarItensDeCartaoAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate) => ConsultarAsync<ItemDeCartaoDto>("""
        SELECT l.id AS Id, l.cartao_credito_id AS CartaoId, l.descricao AS Descricao, l.valor AS Valor, l.data_compra AS DataCompra, c.nome AS CategoriaNome
        FROM lancamentos_cartao l
        LEFT JOIN categorias_financeiras c ON c.id = l.categoria_id
        WHERE l.cartao_credito_id = ANY(@CartaoIds) AND l.data_compra >= @Desde AND l.data_compra < @Ate
        """, new { CartaoIds = cartaoIds.ToArray(), Desde = desde, Ate = ate });

    public Task<List<CompraDeCartaoDto>> ListarComprasDeCartaoAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate) => ConsultarAsync<CompraDeCartaoDto>("""
        SELECT cartao_credito_id AS CartaoId, data_compra AS DataCompra, valor AS Valor
        FROM lancamentos_cartao
        WHERE cartao_credito_id = ANY(@CartaoIds) AND data_compra >= @Desde AND data_compra < @Ate
        """, new { CartaoIds = cartaoIds.ToArray(), Desde = desde, Ate = ate });

    public Task<List<ParcelaDoCartaoDto>> ListarParcelasDeFinanciamentoAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate) => ConsultarAsync<ParcelaDoCartaoDto>("""
        SELECT id AS Id, cartao_origem_id AS CartaoOrigemId, descricao AS Descricao, valor AS Valor, vencimento AS Vencimento, status AS Status,
               numero_parcela AS NumeroParcela, total_parcelas AS TotalParcelas, fatura_cartao_id AS FaturaCartaoId, pago_em AS PagoEm
        FROM lancamentos_financeiros
        WHERE cartao_origem_id = ANY(@CartaoIds) AND vencimento >= @Desde AND vencimento < @Ate
        """, new { CartaoIds = cartaoIds.ToArray(), Desde = desde, Ate = ate });

    public Task<List<ParcelaDoCartaoDto>> ListarParcelasDasFaturasAsync(List<Guid> faturaIds) => ConsultarAsync<ParcelaDoCartaoDto>("""
        SELECT id AS Id, cartao_origem_id AS CartaoOrigemId, descricao AS Descricao, valor AS Valor, vencimento AS Vencimento, status AS Status,
               numero_parcela AS NumeroParcela, total_parcelas AS TotalParcelas, fatura_cartao_id AS FaturaCartaoId, pago_em AS PagoEm
        FROM lancamentos_financeiros
        WHERE fatura_cartao_id = ANY(@FaturaIds)
        ORDER BY numero_parcela
        """, new { FaturaIds = faturaIds.ToArray() });

    public Task<List<FaturaLeveDto>> ListarFaturasAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate) => ConsultarAsync<FaturaLeveDto>("""
        SELECT id AS Id, cartao_credito_id AS CartaoCreditoId, mes_referencia AS MesReferencia, status AS Status, valor_pago AS ValorPago, pago_em AS PagoEm
        FROM faturas_cartao
        WHERE cartao_credito_id = ANY(@CartaoIds) AND mes_referencia >= @Desde AND mes_referencia < @Ate
        """, new { CartaoIds = cartaoIds.ToArray(), Desde = desde, Ate = ate });

    public async Task<Dictionary<Guid, decimal>> SomarAntecipadosPorFaturaAsync(List<Guid> faturaIds) =>
        (await ConsultarAsync<AntecipadoPorFaturaDto>("""
            SELECT fatura_cartao_id AS FaturaId, SUM(valor) AS Total
            FROM pagamentos_antecipados_fatura
            WHERE fatura_cartao_id = ANY(@FaturaIds)
            GROUP BY fatura_cartao_id
            """, new { FaturaIds = faturaIds.ToArray() })).ToDictionary(a => a.FaturaId, a => a.Total);

    // Roda a consulta na conexão do EF (abre e fecha se ela estiver fechada) e dentro da transação em andamento, se houver
    private async Task<List<T>> ConsultarAsync<T>(string sql, object parametros)
    {
        var conexao = db.Database.GetDbConnection();
        var abriu = conexao.State != ConnectionState.Open;
        if (abriu) await conexao.OpenAsync();
        try
        {
            var transacao = db.Database.CurrentTransaction?.GetDbTransaction();
            return (await conexao.QueryAsync<T>(new CommandDefinition(sql, parametros, transacao))).ToList();
        }
        finally
        {
            if (abriu) await conexao.CloseAsync();
        }
    }
}
