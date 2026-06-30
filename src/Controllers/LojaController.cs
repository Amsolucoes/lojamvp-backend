using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Services;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/loja")]
[Authorize]
public class LojaController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet("situacao")]
    public async Task<IActionResult> Situacao()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { fase = "superadmin", diasRestantes = 0 });

        var loja = await db.Lojas.FindAsync(lojaId.Value);
        if (loja is null) return NotFound();

        var (fase, dias) = TenantService.CalcularSituacao(loja);

        return Ok(new
        {
            fase,                       // trial | carencia | ativo | bloqueado | cancelado
            diasRestantes = dias,
            status = loja.Status.ToString(),
            mensalidadeValor = loja.MensalidadeValor,
            trialAte = loja.TrialAte,
            proximoVencimento = loja.ProximoVencimento,
            nomeLoja = loja.Nome,
            tipoPlano = loja.TipoPlano,
            modulosAtivos = loja.ModulosAtivos
             .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        });
    }
}