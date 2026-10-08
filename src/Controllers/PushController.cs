using LojaApi.Data;
using LojaApi.Models;
using LojaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/push")]
[Authorize]
public class PushController(AppDbContext db, PushService push) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public record AssinarRequest(string Endpoint, string ChavePublica, string ChaveAuth);
    public record CancelarRequest(string Endpoint);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // Chave pública que o navegador precisa para se inscrever
    [HttpGet("chave-publica")]
    public IActionResult ChavePublica()
    {
        if (!push.Configurado) return StatusCode(503, new { erro = "Notificações push não configuradas no servidor." });
        return Ok(new { chave = push.ChavePublica });
    }

    // Quantos aparelhos deste usuário estão ativos (para a tela mostrar o estado)
    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] string? endpoint)
    {
        var usuarioId = UsuarioId;
        var ativo = !string.IsNullOrEmpty(endpoint)
            && await db.AssinaturasPush.AnyAsync(a => a.UsuarioId == usuarioId && a.Endpoint == endpoint);
        return Ok(new { configurado = push.Configurado, ativo });
    }

    [HttpPost("assinar")]
    public async Task<IActionResult> Assinar([FromBody] AssinarRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Endpoint) || string.IsNullOrWhiteSpace(req.ChavePublica) || string.IsNullOrWhiteSpace(req.ChaveAuth))
            return BadRequest(new { erro = "Assinatura inválida." });

        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        // O mesmo aparelho pode trocar de usuário/loja: atualiza em vez de duplicar
        var existente = await db.AssinaturasPush.FirstOrDefaultAsync(a => a.Endpoint == req.Endpoint);
        if (existente is null)
        {
            db.AssinaturasPush.Add(new AssinaturaPush
            {
                UsuarioId = UsuarioId,
                LojaId = lojaId.Value,
                Endpoint = req.Endpoint,
                ChavePublica = req.ChavePublica,
                ChaveAuth = req.ChaveAuth,
            });
        }
        else
        {
            existente.UsuarioId = UsuarioId;
            existente.LojaId = lojaId.Value;
            existente.ChavePublica = req.ChavePublica;
            existente.ChaveAuth = req.ChaveAuth;
            existente.UltimoAvisoEm = null;
        }
        await db.SaveChangesAsync();
        return Ok(new { ativo = true });
    }

    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar([FromBody] CancelarRequest req)
    {
        var usuarioId = UsuarioId;
        var existente = await db.AssinaturasPush
            .FirstOrDefaultAsync(a => a.Endpoint == req.Endpoint && a.UsuarioId == usuarioId);
        if (existente is not null)
        {
            db.AssinaturasPush.Remove(existente);
            await db.SaveChangesAsync();
        }
        return Ok(new { ativo = false });
    }

    // Envia uma notificação de teste para os aparelhos do usuário logado
    [HttpPost("testar")]
    public async Task<IActionResult> Testar()
    {
        if (!push.Configurado) return StatusCode(503, new { erro = "Notificações push não configuradas no servidor." });

        var usuarioId = UsuarioId;
        var assinaturas = await db.AssinaturasPush.Where(a => a.UsuarioId == usuarioId).ToListAsync();
        if (assinaturas.Count == 0) return BadRequest(new { erro = "Nenhum aparelho com avisos ativos." });

        int enviados = 0;
        foreach (var a in assinaturas)
        {
            var r = await push.EnviarAsync(a, "Avisos ativados ✅", "Você vai receber os vencimentos do dia às 7h.", "/financeiro?aba=pagar", "teste");
            if (r == PushService.Resultado.Enviado) enviados++;
            else if (r == PushService.Resultado.AssinaturaInvalida) db.AssinaturasPush.Remove(a);
        }
        await db.SaveChangesAsync();
        return Ok(new { enviados });
    }
}
