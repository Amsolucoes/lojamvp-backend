using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Painel do superadmin: contagem de lojas por situação, receita, lojas em atraso, últimos pagamentos e projeção mensal (sem as lojas de teste).
public class DashboardAdminQueryHandler(IAdminRepository repositorio) : IQueryHandler<DashboardAdminQuery>
{
    public async Task<Resultado<object>> HandleAsync(DashboardAdminQuery query)
    {
        var lojas = await repositorio.ListarLojasReaisComPagamentosAsync();
        var agora = DateTime.UtcNow;

        var atrasadas = lojas
            .Where(l => l.ProximoVencimento.HasValue &&
                        (agora - l.ProximoVencimento.Value).TotalDays > 0 &&
                        l.Status != StatusLoja.Cancelado)
            .Select(l => PlataformaApoio.ParaResumoDto(l, agora))
            .ToList();

        var ultimosPagamentos = (await repositorio.ListarUltimosPagamentosPagosAsync(10)).Select(PlataformaApoio.ParaDto).ToList();

        // Projeção: lojas ativas ou em trial (receita confirmada + esperada)
        var lojasAtivas = lojas.Where(l => l.Status == StatusLoja.Ativo).ToList();
        var lojasProjecao = lojas.Where(l => l.Status == StatusLoja.Ativo || l.Status == StatusLoja.Trial).ToList();
        var projecaoMensal = lojasProjecao
            .OrderByDescending(l => l.Status == StatusLoja.Ativo) // ativas primeiro
            .ThenByDescending(l => l.MensalidadeValor)
            .Select(l => new ProjecaoMensalDto(l.Id, l.Nome, l.MensalidadeValor, l.MensalidadeDia, l.ProximoVencimento, l.Status.ToString()))
            .ToList();

        return Resultado.Ok(new DashboardAdminDto(
            TotalLojas: lojas.Count,
            LojasAtivas: lojas.Count(l => l.Status == StatusLoja.Ativo),
            LojasTrial: lojas.Count(l => l.Status == StatusLoja.Trial),
            LojasBloqueadas: lojas.Count(l => l.Status == StatusLoja.Bloqueado),
            LojasEmAtraso: atrasadas.Count,
            ReceitaMensal: lojasAtivas.Sum(l => l.MensalidadeValor),
            ReceitaTotal: lojas.SelectMany(l => l.Pagamentos).Where(p => p.Status == "pago").Sum(p => p.Valor),
            LojasAtrasadas: atrasadas,
            UltimosPagamentos: ultimosPagamentos,
            ProjecaoMensal: projecaoMensal));
    }
}
