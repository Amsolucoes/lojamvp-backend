using LojaApi.Data;
using LojaApi.src.Models.Etiquetas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.src.Controllers.Etiquetas;

[ApiController]
[Route("api/etiquetas/configuracao")]
[Authorize]
public class EtiquetasController(AppDbContext db) : ControllerBase
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

        var cfg = await db.ConfiguracoesEtiqueta.FirstOrDefaultAsync(c => c.LojaId == lojaId);
        if (cfg is null)
        {
            cfg = new ConfiguracaoEtiqueta { LojaId = lojaId.Value };
            db.ConfiguracoesEtiqueta.Add(cfg);
            await db.SaveChangesAsync();
        }

        return Ok(cfg);
    }

    public record AtualizarConfigRequest(
        bool IncluirLogo, bool UsarLogoPropria, string? LogoEtiquetaUrl,
        bool IncluirNomeMarca, bool IncluirNomeProduto, bool IncluirPreco, bool IncluirCodigoBarras,
        decimal LarguraMm, decimal AlturaMm
    );

    [HttpPut]
    public async Task<IActionResult> Atualizar([FromBody] AtualizarConfigRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (req.LarguraMm <= 0 || req.AlturaMm <= 0)
            return BadRequest(new { erro = "Largura e altura devem ser maiores que zero." });

        var cfg = await db.ConfiguracoesEtiqueta.FirstOrDefaultAsync(c => c.LojaId == lojaId);
        if (cfg is null)
        {
            cfg = new ConfiguracaoEtiqueta { LojaId = lojaId.Value };
            db.ConfiguracoesEtiqueta.Add(cfg);
        }

        cfg.IncluirLogo = req.IncluirLogo;
        cfg.UsarLogoPropria = req.UsarLogoPropria;
        cfg.LogoEtiquetaUrl = string.IsNullOrWhiteSpace(req.LogoEtiquetaUrl) ? null : req.LogoEtiquetaUrl.Trim();
        cfg.IncluirNomeMarca = req.IncluirNomeMarca;
        cfg.IncluirNomeProduto = req.IncluirNomeProduto;
        cfg.IncluirPreco = req.IncluirPreco;
        cfg.IncluirCodigoBarras = req.IncluirCodigoBarras;
        cfg.LarguraMm = req.LarguraMm;
        cfg.AlturaMm = req.AlturaMm;
        cfg.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(cfg);
    }
}