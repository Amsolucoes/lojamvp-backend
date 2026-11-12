using LojaApi.Data;
using LojaApi.Domain.Chacara;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Chacara;

public record AtualizarConfiguracaoPrecoRequest(
    int MinimoPessoas,
    int LimitePessoasParaTaxaLimpeza,
    decimal ValorTaxaLimpeza,
    decimal ValorMultaNaoLimpeza,
    decimal PercentualEntradaMinimo,
    decimal? PercentualTaxaCartao = null,
    decimal? ValorLimiteParcelas = null,
    int? ParcelasAteLimite = null,
    int? ParcelasAcimaLimite = null
);

// Regras gerais de preço: mínimo de pessoas, taxa de limpeza, entrada mínima e parcelamento no cartão.
public class ConfiguracaoPrecoChacaraService(AppDbContext db)
{
    public async Task<Resultado<object>> ObterAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == lojaId);
        if (cfg is null)
        {
            cfg = new ConfiguracaoPrecoChacara { LojaId = lojaId.Value };
            db.ConfiguracoesPrecoChacara.Add(cfg);
            await db.SaveChangesAsync();
        }
        return Resultado.Ok(cfg);
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, AtualizarConfiguracaoPrecoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var erro = ConfiguracaoPrecoChacara.ValidarCartao(req.PercentualTaxaCartao, req.ParcelasAteLimite, req.ParcelasAcimaLimite);
        if (erro != null) return Resultado.Invalido(erro);

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == lojaId);
        if (cfg is null)
        {
            cfg = new ConfiguracaoPrecoChacara { LojaId = lojaId.Value };
            db.ConfiguracoesPrecoChacara.Add(cfg);
        }

        cfg.Atualizar(
            req.MinimoPessoas, req.LimitePessoasParaTaxaLimpeza, req.ValorTaxaLimpeza, req.ValorMultaNaoLimpeza,
            req.PercentualEntradaMinimo, req.PercentualTaxaCartao, req.ValorLimiteParcelas,
            req.ParcelasAteLimite, req.ParcelasAcimaLimite);
        await db.SaveChangesAsync();
        return Resultado.Ok(cfg);
    }
}
