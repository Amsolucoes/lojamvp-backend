using System.Data;
using Dapper;
using LojaApi.Application.Financeiro;
using LojaApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LojaApi.Infrastructure.Financeiro;

// Consultas de leitura dos relatórios do Financeiro, com Dapper (SQL direto): cada dado é lido de uma vez para todos os cartões e meses, em vez de
// várias consultas do EF por cartão e mês. Usa a mesma conexão e transação do AppDbContext. Só lê; as regras ficam nos handlers e no Domain.
// As colunas ganham o nome da propriedade do DTO (o Dapper casa sem diferenciar maiúscula, mas sem o "_").
// Toda consulta por cartão recebe ids de cartões já filtrados pela loja (ListarCartoesAtivosAsync).
public class RelatoriosConsultas(AppDbContext db) : IRelatoriosConsultas
{
    public Task<List<LancamentoDoMesDto>> ListarLancamentosDoPeriodoAsync(Guid lojaId, DateTime inicio, DateTime fim) => ConsultarAsync<LancamentoDoMesDto>("""
        SELECT l.tipo AS Tipo, l.status AS Status, l.valor AS Valor, l.vencimento AS Vencimento, l.cartao_origem_id AS CartaoOrigemId,
               c.nome AS CategoriaNome, c.icone AS CategoriaIcone
        FROM lancamentos_financeiros l
        LEFT JOIN categorias_financeiras c ON c.id = l.categoria_id
        WHERE l.loja_id = @LojaId AND l.vencimento >= @Inicio AND l.vencimento < @Fim
        """, new { LojaId = lojaId, Inicio = inicio, Fim = fim });

    public Task<List<PagamentoDePlanoDto>> ListarPagamentosDePlanoAsync(Guid lojaId, DateTime inicio, DateTime fim, bool apenasAssinaturasAtivas) => ConsultarAsync<PagamentoDePlanoDto>("""
        SELECT p.status AS Status, p.valor AS Valor, p.mes_referencia AS MesReferencia
        FROM pagamentos_plano p
        WHERE p.mes_referencia >= @Inicio AND p.mes_referencia < @Fim
          AND p.assinatura_id IN (SELECT a.id FROM assinaturas_cliente a WHERE a.loja_id = @LojaId AND (NOT @ApenasAtivas OR a.status = 'ativa'))
        """, new { LojaId = lojaId, Inicio = inicio, Fim = fim, ApenasAtivas = apenasAssinaturasAtivas });

    public Task<List<decimal>> ListarValoresDosPlanosAtivosAsync(Guid lojaId) => ConsultarAsync<decimal>("""
        SELECT COALESCE(p.valor, 0)
        FROM assinaturas_cliente a
        LEFT JOIN planos p ON p.id = a.plano_id
        WHERE a.loja_id = @LojaId AND a.status = 'ativa'
        """, new { LojaId = lojaId });

    public Task<List<CartaoDoRelatorioDto>> ListarCartoesAtivosAsync(Guid lojaId) => ConsultarAsync<CartaoDoRelatorioDto>("""
        SELECT id AS Id, nome AS Nome, bandeira AS Bandeira, limite AS Limite, dia_fechamento AS DiaFechamento, dia_vencimento AS DiaVencimento
        FROM cartoes_credito
        WHERE loja_id = @LojaId AND ativo
        """, new { LojaId = lojaId });

    public Task<List<CompraLeveDto>> ListarComprasLevesAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate) => ConsultarAsync<CompraLeveDto>("""
        SELECT cartao_credito_id AS CartaoId, data_compra AS DataCompra, valor AS Valor, modo AS Modo
        FROM lancamentos_cartao
        WHERE cartao_credito_id = ANY(@CartaoIds) AND data_compra >= @Desde AND data_compra < @Ate
        """, new { CartaoIds = cartaoIds.ToArray(), Desde = desde, Ate = ate });

    public Task<List<CompraCategorizadaDto>> ListarComprasCategorizadasAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate) => ConsultarAsync<CompraCategorizadaDto>("""
        SELECT l.id AS Id, l.cartao_credito_id AS CartaoId, l.descricao AS Descricao, l.valor AS Valor, l.data_compra AS DataCompra, l.modo AS Modo,
               c.nome AS CategoriaNome, c.icone AS CategoriaIcone
        FROM lancamentos_cartao l
        LEFT JOIN categorias_financeiras c ON c.id = l.categoria_id
        WHERE l.cartao_credito_id = ANY(@CartaoIds) AND l.data_compra >= @Desde AND l.data_compra < @Ate
        """, new { CartaoIds = cartaoIds.ToArray(), Desde = desde, Ate = ate });

    public Task<List<FaturaDoRelatorioDto>> ListarFaturasAsync(List<Guid> cartaoIds, DateTime desde, DateTime ate) => ConsultarAsync<FaturaDoRelatorioDto>("""
        SELECT id AS Id, cartao_credito_id AS CartaoCreditoId, mes_referencia AS MesReferencia, status AS Status, total AS Total
        FROM faturas_cartao
        WHERE cartao_credito_id = ANY(@CartaoIds) AND mes_referencia >= @Desde AND mes_referencia < @Ate
        """, new { CartaoIds = cartaoIds.ToArray(), Desde = desde, Ate = ate });

    public Task<List<FinanciamentoPendenteDto>> ListarFinanciamentoPendentePorCartaoAsync(List<Guid> cartaoIds) => ConsultarAsync<FinanciamentoPendenteDto>("""
        SELECT cartao_origem_id AS CartaoId, SUM(valor) AS Total
        FROM lancamentos_financeiros
        WHERE cartao_origem_id = ANY(@CartaoIds) AND status = 'pendente'
        GROUP BY cartao_origem_id
        """, new { CartaoIds = cartaoIds.ToArray() });

    // categoria nula = lançamentos sem categoria; senão, os da categoria com esse nome. Parcelas de financiamento de cartão ficam de fora (entram pelo cartão).
    public Task<List<LancamentoDetalheDto>> ListarAPagarDaCategoriaAsync(Guid lojaId, DateTime inicio, DateTime fim, string? categoria) => ConsultarAsync<LancamentoDetalheDto>($"""
        SELECT l.id AS Id, l.modo AS Modo, l.descricao AS Descricao, l.valor AS Valor, l.vencimento AS Vencimento, l.status AS Status, cb.nome AS ContaNome
        FROM lancamentos_financeiros l
        LEFT JOIN contas_bancarias cb ON cb.id = l.conta_bancaria_id
        LEFT JOIN categorias_financeiras c ON c.id = l.categoria_id
        WHERE l.loja_id = @LojaId AND l.tipo = 'pagar' AND l.cartao_origem_id IS NULL AND l.vencimento >= @Inicio AND l.vencimento < @Fim
          AND {(categoria is null ? "c.id IS NULL" : "c.nome = @Categoria")}
        """, new { LojaId = lojaId, Inicio = inicio, Fim = fim, Categoria = categoria });

    public Task<List<LancamentoPendenteDto>> ListarPendentesAteAsync(Guid lojaId, DateTime de, DateTime ate) => ConsultarAsync<LancamentoPendenteDto>("""
        SELECT id AS Id, descricao AS Descricao, tipo AS Tipo, valor AS Valor, vencimento AS Vencimento
        FROM lancamentos_financeiros
        WHERE loja_id = @LojaId AND status = 'pendente' AND vencimento >= @De AND vencimento <= @Ate
        ORDER BY vencimento
        """, new { LojaId = lojaId, De = de, Ate = ate });

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
