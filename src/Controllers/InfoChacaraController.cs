using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/chacara/info")]
[Authorize]
public class InfoChacaraController(AppDbContext db) : ControllerBase
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

        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == lojaId);
        if (info is null)
        {
            info = new InfoChacara { LojaId = lojaId.Value };
            db.InfosChacara.Add(info);
            await db.SaveChangesAsync();
        }

        return Ok(info);
    }

    public record AtualizarInfoRequest(
        string Descricao, string Endereco, string Comodidades, string? ComodidadesExtras, string? MapaEmbedUrl,
        string? LocadorNome, string? LocadorRg, string? LocadorCpf, string? LocadorEndereco, string? LocadorTelefone, string? CidadeAssinatura
    );

    [HttpPut]
    public async Task<IActionResult> Atualizar([FromBody] AtualizarInfoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == lojaId);
        if (info is null)
        {
            info = new InfoChacara { LojaId = lojaId.Value };
            db.InfosChacara.Add(info);
        }

        info.Descricao = req.Descricao?.Trim() ?? "";
        info.Endereco = req.Endereco?.Trim() ?? "";
        info.Comodidades = req.Comodidades ?? "";
        info.ComodidadesExtras = string.IsNullOrWhiteSpace(req.ComodidadesExtras) ? null : req.ComodidadesExtras.Trim();
        info.MapaEmbedUrl = string.IsNullOrWhiteSpace(req.MapaEmbedUrl) ? null : req.MapaEmbedUrl.Trim();
        info.LocadorNome = string.IsNullOrWhiteSpace(req.LocadorNome) ? null : req.LocadorNome.Trim();
        info.LocadorRg = string.IsNullOrWhiteSpace(req.LocadorRg) ? null : req.LocadorRg.Trim();
        info.LocadorCpf = string.IsNullOrWhiteSpace(req.LocadorCpf) ? null : req.LocadorCpf.Trim();
        info.LocadorEndereco = string.IsNullOrWhiteSpace(req.LocadorEndereco) ? null : req.LocadorEndereco.Trim();
        info.LocadorTelefone = string.IsNullOrWhiteSpace(req.LocadorTelefone) ? null : req.LocadorTelefone.Trim();
        info.CidadeAssinatura = string.IsNullOrWhiteSpace(req.CidadeAssinatura) ? null : req.CidadeAssinatura.Trim();

        await db.SaveChangesAsync();
        return Ok(info);
    }
}