using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Faturas de todas as lojas (reais; as de teste só se pedir), com filtros, página e totais do período.
public class ListarPagamentosAdminQueryHandler(IAdminRepository repositorio) : IQueryHandler<ListarPagamentosAdminQuery>
{
    private const int TamanhoMaximo = 100;

    public async Task<Resultado<object>> HandleAsync(ListarPagamentosAdminQuery q)
    {
        var status = string.IsNullOrWhiteSpace(q.Status) ? null : q.Status.Trim().ToLowerInvariant();
        if (status is not (null or "pago" or "pendente" or "atrasado")) return Resultado.Invalido("Status inválido. Use pago, pendente ou atrasado.");
        if (q.Mes.HasValue && !q.Ano.HasValue) return Resultado.Invalido("Informe o ano junto com o mês.");
        if (q.Mes is < 1 or > 12) return Resultado.Invalido("Mês inválido.");
        if (q.Ano is < 2000 or > 2200) return Resultado.Invalido("Ano inválido.");

        var pagina = Math.Max(1, q.Pagina);
        var tamanho = Math.Clamp(q.Tamanho, 1, TamanhoMaximo);
        var forma = string.IsNullOrWhiteSpace(q.Forma) ? null : q.Forma.Trim().ToLowerInvariant();

        var r = await repositorio.ListarPagamentosDeTodasAsLojasAsync(
            new FiltroPagamentosAdmin(status, q.LojaId, q.Busca, q.Ano, q.Mes, forma, q.IncluirTeste, pagina, tamanho), DateTime.UtcNow);

        return Resultado.Ok(new
        {
            itens = r.Itens.Select(PlataformaApoio.ParaDto).ToList(),
            total = r.Total,
            pagina,
            tamanho,
            totais = new { aReceber = r.AReceber, emAtraso = r.EmAtraso, recebido = r.Recebido },
        });
    }
}
