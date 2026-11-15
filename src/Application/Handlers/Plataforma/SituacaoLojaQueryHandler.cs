using LojaApi.Domain.Plataforma;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Situação da assinatura e configurações da loja (fase trial/carência/ativo/bloqueado, módulos, agenda, link, pausa).
// Superadmin (sem loja) recebe só a fase fixa "superadmin".
public class SituacaoLojaQueryHandler(IPlataformaRepository repositorio) : IQueryHandler<SituacaoLojaQuery>
{
    public async Task<Resultado<object>> HandleAsync(SituacaoLojaQuery query)
    {
        if (query.LojaId is not { } lojaId) return Resultado.Ok(new SituacaoSuperadminDto("superadmin", 0));

        var loja = await repositorio.ObterLojaAsync(lojaId);
        if (loja is null) return Resultado.NaoEncontrado();

        var (fase, dias) = PlataformaRegras.CalcularSituacao(loja.Status.ToString(), loja.TrialAte, loja.ProximoVencimento, DateTime.UtcNow);

        return Resultado.Ok(new SituacaoLojaDto(
            fase, dias, loja.Status.ToString(), loja.MensalidadeValor, loja.TrialAte, loja.ProximoVencimento,
            loja.Nome, loja.Endereco, loja.Telefone, loja.LogoUrl, loja.TipoPlano, PlataformaRegras.SepararModulos(loja.ModulosAtivos),
            loja.AgendaHoraInicio, loja.AgendaHoraFim, loja.AssinaturaStatus, loja.AssinaturaCartaoFinal,
            loja.AgendamentoOnlineAtivo, string.IsNullOrEmpty(loja.AgendamentoOnlineConfirmacao) ? "aprovacao" : loja.AgendamentoOnlineConfirmacao, loja.Slug,
            loja.ModulosAlteradoEm, loja.PausaAte, loja.PausaMensagem));
    }
}
