using LojaApi.Domain.Plataforma;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Painel do assinante: situação da assinatura, fatura pendente e as 12 últimas faturas. Antes de responder, grava o bloqueio
// da loja se o prazo passou.
public class DashboardClienteQueryHandler(IPlataformaRepository repositorio) : IQueryHandler<DashboardClienteQuery>
{
    public async Task<Resultado<object>> HandleAsync(DashboardClienteQuery query)
    {
        var loja = await repositorio.ObterMinhaLojaComPagamentosAsync(query.UsuarioId);
        if (loja is null) return Resultado.NaoEncontrado("Loja não encontrada.");

        PlataformaApoio.AtualizarStatusDaLoja(loja, DateTime.UtcNow);
        await repositorio.SalvarAsync();

        var agora = DateTime.UtcNow;
        var diasAtraso = loja.ProximoVencimento.HasValue
            ? Math.Max(0, PlataformaRegras.DiasDeAtraso(loja.ProximoVencimento.Value, agora))
            : 0;

        var faturaPendente = loja.Pagamentos
            .Where(p => p.Status == "pendente" || p.Status == "atrasado")
            .OrderBy(p => p.Vencimento)
            .FirstOrDefault();

        var historico = loja.Pagamentos
            .OrderByDescending(p => p.Vencimento)
            .Take(12)
            .Select(PlataformaApoio.ParaDto)
            .ToList();

        return Resultado.Ok(new DashboardClienteDto(
            NomeLoja: loja.Nome,
            Status: loja.Status.ToString(),
            TrialAte: loja.Status == StatusLoja.Trial ? loja.TrialAte : null,
            ProximoVencimento: loja.ProximoVencimento,
            MensalidadeValor: loja.MensalidadeValor,
            EmAtraso: diasAtraso > 0,
            DiasAtraso: diasAtraso,
            FaturaPendente: faturaPendente != null ? PlataformaApoio.ParaDto(faturaPendente) : null,
            HistoricoFaturas: historico,
            AssinaturaStatus: loja.AssinaturaStatus,
            AssinaturaCartaoFinal: loja.AssinaturaCartaoFinal));
    }
}
