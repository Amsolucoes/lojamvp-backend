using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/chacara/configuracao-preco")]
[Authorize]
public class ConfiguracaoPrecoChacaraController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet]
    public async Task<IActionResult> Obter()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == lojaId);
        if (cfg is null)
        {
            cfg = new ConfiguracaoPrecoChacara { LojaId = lojaId.Value };
            db.ConfiguracoesPrecoChacara.Add(cfg);
            await db.SaveChangesAsync();
        }

        return Ok(cfg);
    }

    public record AtualizarConfigRequest(
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

    [HttpPut]
    public async Task<IActionResult> Atualizar([FromBody] AtualizarConfigRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == lojaId);
        if (cfg is null)
        {
            cfg = new ConfiguracaoPrecoChacara { LojaId = lojaId.Value };
            db.ConfiguracoesPrecoChacara.Add(cfg);
        }

        cfg.MinimoPessoas = req.MinimoPessoas;
        cfg.LimitePessoasParaTaxaLimpeza = req.LimitePessoasParaTaxaLimpeza;
        cfg.ValorTaxaLimpeza = req.ValorTaxaLimpeza;
        cfg.ValorMultaNaoLimpeza = req.ValorMultaNaoLimpeza;
        cfg.PercentualEntradaMinimo = req.PercentualEntradaMinimo;

        if (req.PercentualTaxaCartao is < 0 or > 15)
            return BadRequest(new { erro = "A taxa do cartão deve ficar entre 0% e 15%." });
        if (req.ParcelasAteLimite is < 1 or > 12 || req.ParcelasAcimaLimite is < 1 or > 12)
            return BadRequest(new { erro = "O número de parcelas deve ficar entre 1 e 12." });
        if (req.PercentualTaxaCartao.HasValue) cfg.PercentualTaxaCartao = req.PercentualTaxaCartao.Value;
        if (req.ValorLimiteParcelas.HasValue) cfg.ValorLimiteParcelas = req.ValorLimiteParcelas.Value;
        if (req.ParcelasAteLimite.HasValue) cfg.ParcelasAteLimite = req.ParcelasAteLimite.Value;
        if (req.ParcelasAcimaLimite.HasValue) cfg.ParcelasAcimaLimite = req.ParcelasAcimaLimite.Value;

        await db.SaveChangesAsync();
        return Ok(cfg);
    }
}