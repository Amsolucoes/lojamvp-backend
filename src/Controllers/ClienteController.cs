using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;
using LojaApi.Services;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/cliente")]
[Authorize]
public class ClienteController(AppDbContext db, TenantService tenantService) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Loja?> GetMinhaLoja()
    {
        var vinculo = await db.UsuariosLoja
            .Include(ul => ul.Loja).ThenInclude(l => l.Pagamentos)
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.Loja;
    }

    // ── Dashboard do cliente ──────────────────────────────────────
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var loja = await GetMinhaLoja();
        if (loja is null) return NotFound(new { erro = "Loja não encontrada." });

        tenantService.AtualizarStatusLoja(loja);
        await db.SaveChangesAsync();

        var agora       = DateTime.UtcNow;
        var diasAtraso  = loja.ProximoVencimento.HasValue
            ? Math.Max(0, (int)(agora - loja.ProximoVencimento.Value).TotalDays)
            : 0;

        var faturaPendente = loja.Pagamentos
            .Where(p => p.Status == "pendente" || p.Status == "atrasado")
            .OrderBy(p => p.Vencimento)
            .FirstOrDefault();

        var historico = loja.Pagamentos
            .OrderByDescending(p => p.Vencimento)
            .Take(12)
            .Select(ToDto)
            .ToList();

        return Ok(new DashboardClienteDto(
            NomeLoja:         loja.Nome,
            Status:           loja.Status.ToString(),
            TrialAte:         loja.Status == StatusLoja.Trial ? loja.TrialAte : null,
            ProximoVencimento: loja.ProximoVencimento,
            MensalidadeValor: loja.MensalidadeValor,
            EmAtraso:         diasAtraso > 0,
            DiasAtraso:       diasAtraso,
            FaturaPendente:   faturaPendente != null ? ToDto(faturaPendente) : null,
            HistoricoFaturas: historico,
            AssinaturaStatus: loja.AssinaturaStatus,
            AssinaturaCartaoFinal: loja.AssinaturaCartaoFinal
        ));
    }

    // ── Configurações da loja ─────────────────────────────────────
    [HttpGet("config")]
    public async Task<IActionResult> Config()
    {
        var loja = await GetMinhaLoja();
        if (loja is null) return NotFound();

        return Ok(new LojaConfigDto(
            loja.Id, loja.Nome, loja.CorPrimaria,
            loja.LogoUrl, loja.Status.ToString(),
            loja.Status == StatusLoja.Bloqueado
                ? "Acesso bloqueado por inadimplência."
                : null
        ));
    }

    // ── Atualizar configurações visuais ───────────────────────────
    [HttpPatch("config")]
    public async Task<IActionResult> AtualizarConfig([FromBody] AtualizarConfigRequest req)
    {
        var loja = await GetMinhaLoja();
        if (loja is null) return NotFound();

        loja.Nome        = req.Nome ?? loja.Nome;
        loja.CorPrimaria = req.CorPrimaria ?? loja.CorPrimaria;
        loja.LogoUrl     = req.LogoUrl ?? loja.LogoUrl;
        loja.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Configurações atualizadas." });
    }

    // ── Listar faturas ────────────────────────────────────────────
    [HttpGet("faturas")]
    public async Task<IActionResult> Faturas()
    {
        var loja = await GetMinhaLoja();
        if (loja is null) return NotFound();

        var faturas = loja.Pagamentos
            .OrderByDescending(p => p.Vencimento)
            .Select(ToDto)
            .ToList();

        return Ok(faturas);
    }

    private static PagamentoDto ToDto(Pagamento p) => new(
        p.Id, p.LojaId, p.Loja?.Nome ?? "",
        p.Valor, p.Status,
        p.Vencimento, p.PagoEm,
        p.FormaPagamento, p.Observacao,
        p.MpQrCode, p.MpQrCodeBase64,
        p.MpBoletoUrl, p.MpBoletoBarcode,
        p.CriadoEm
    );
}

// DTO local
public record AtualizarConfigRequest(string? Nome, string? CorPrimaria, string? LogoUrl);
