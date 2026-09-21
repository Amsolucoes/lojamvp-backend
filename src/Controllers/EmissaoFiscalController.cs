using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.src.Models.Fiscal;
using LojaApi.src.Services.Fiscal;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/fiscal")]
[Authorize]
public class EmissaoFiscalController(AppDbContext db, IProvedorFiscal provedorFiscal) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    public record ConfiguracaoFiscalDto(
        bool Ativo, string? Provedor, string Ambiente, string? InscricaoEstadual,
        string? RegimeTributario, string SerieNfce, int ProximoNumeroNfce, bool CredencialConfigurada);

    private static ConfiguracaoFiscalDto ToDto(ConfiguracaoFiscal c) => new(
        c.Ativo, c.Provedor, c.Ambiente, c.InscricaoEstadual, c.RegimeTributario,
        c.SerieNfce, c.ProximoNumeroNfce, !string.IsNullOrEmpty(c.CredencialToken));

    public record EmissaoFiscalDto(
        Guid Id, Guid VendaId, string Tipo, string Status, string? Provedor,
        string? ChaveAcesso, string? NumeroNota, string? SerieNota, string? Protocolo,
        string? UrlDanfe, string? UrlXml, string? MotivoErro, DateTime SolicitadaEm, DateTime? ProcessadaEm);

    private static EmissaoFiscalDto ToDto(EmissaoFiscal e) => new(
        e.Id, e.VendaId, e.Tipo, e.Status, e.Provedor, e.ChaveAcesso, e.NumeroNota,
        e.SerieNota, e.Protocolo, e.UrlDanfe, e.UrlXml, e.MotivoErro, e.SolicitadaEm, e.ProcessadaEm);

    // ── Configuração fiscal da loja ──────────────────────────────────────
    [HttpGet("configuracao")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> BuscarConfiguracao()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var config = await db.ConfiguracoesFiscais.FirstOrDefaultAsync(c => c.LojaId == lojaId);
        config ??= new ConfiguracaoFiscal { LojaId = lojaId.Value };

        return Ok(ToDto(config));
    }

    public record SalvarConfiguracaoFiscalRequest(
        string? Provedor, string Ambiente, string? CredencialToken,
        string? InscricaoEstadual, string? RegimeTributario, string SerieNfce);

    [HttpPut("configuracao")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> SalvarConfiguracao([FromBody] SalvarConfiguracaoFiscalRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (req.Ambiente != "homologacao" && req.Ambiente != "producao")
            return BadRequest(new { erro = "Ambiente inválido." });

        var config = await db.ConfiguracoesFiscais.FirstOrDefaultAsync(c => c.LojaId == lojaId);
        if (config is null)
        {
            config = new ConfiguracaoFiscal { LojaId = lojaId.Value };
            db.ConfiguracoesFiscais.Add(config);
        }

        config.Provedor = req.Provedor;
        config.Ambiente = req.Ambiente;
        if (!string.IsNullOrWhiteSpace(req.CredencialToken)) config.CredencialToken = req.CredencialToken;
        config.InscricaoEstadual = req.InscricaoEstadual;
        config.RegimeTributario = req.RegimeTributario;
        config.SerieNfce = string.IsNullOrWhiteSpace(req.SerieNfce) ? "1" : req.SerieNfce.Trim();
        // Só fica "ativo" quando houver de fato um provedor e credencial configurados —
        // enquanto isso, a emissão continua bloqueada com uma mensagem clara.
        config.Ativo = !string.IsNullOrWhiteSpace(config.Provedor) && !string.IsNullOrWhiteSpace(config.CredencialToken);
        config.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToDto(config));
    }

    // ── Emissão ───────────────────────────────────────────────────────────
    [HttpPost("vendas/{vendaId:guid}/emitir")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Emitir(Guid vendaId)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var loja = await db.Lojas.FindAsync(lojaId.Value);
        if (loja is null) return NotFound();

        var modulosAtivos = loja.ModulosAtivos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!modulosAtivos.Contains("nfce"))
            return BadRequest(new { erro = "O módulo de emissão fiscal (NFC-e) não está ativo para esta loja." });

        var venda = await db.Vendas.FirstOrDefaultAsync(v => v.Id == vendaId && v.LojaId == lojaId);
        if (venda is null) return NotFound(new { erro = "Venda não encontrada." });

        var config = await db.ConfiguracoesFiscais.FirstOrDefaultAsync(c => c.LojaId == lojaId);
        if (config is null || !config.Ativo)
            return BadRequest(new { erro = "Emissão fiscal ainda não está configurada. Contrate e configure um provedor antes de emitir." });

        var emissao = new EmissaoFiscal
        {
            LojaId = lojaId.Value,
            VendaId = venda.Id,
            Tipo = "nfce",
            Status = "processando",
            Provedor = config.Provedor,
            SolicitadaPorUsuarioId = UsuarioId,
        };
        db.EmissoesFiscais.Add(emissao);

        var resultado = await provedorFiscal.EmitirNfceAsync(venda, config);

        emissao.Status = resultado.Status;
        emissao.ChaveAcesso = resultado.ChaveAcesso;
        emissao.NumeroNota = resultado.NumeroNota;
        emissao.SerieNota = resultado.SerieNota;
        emissao.Protocolo = resultado.Protocolo;
        emissao.UrlDanfe = resultado.UrlDanfe;
        emissao.UrlXml = resultado.UrlXml;
        emissao.MotivoErro = resultado.MotivoErro;
        emissao.ProcessadaEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToDto(emissao));
    }

    [HttpGet("vendas/{vendaId:guid}/emissoes")]
    public async Task<IActionResult> ListarEmissoes(Guid vendaId)
    {
        var lojaId = await GetLojaId();
        var q = db.EmissoesFiscais.Where(e => e.VendaId == vendaId);
        if (lojaId.HasValue) q = q.Where(e => e.LojaId == lojaId);

        var lista = await q.OrderByDescending(e => e.SolicitadaEm).Select(e => ToDto(e)).ToListAsync();
        return Ok(lista);
    }

    public record CancelarEmissaoRequest(string Justificativa);

    [HttpPost("emissoes/{emissaoId:guid}/cancelar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Cancelar(Guid emissaoId, [FromBody] CancelarEmissaoRequest req)
    {
        var lojaId = await GetLojaId();
        var emissao = await db.EmissoesFiscais.FirstOrDefaultAsync(e => e.Id == emissaoId && (!lojaId.HasValue || e.LojaId == lojaId));
        if (emissao is null) return NotFound();

        if (emissao.Status != "autorizada")
            return BadRequest(new { erro = "Só é possível cancelar uma nota autorizada." });

        if (string.IsNullOrWhiteSpace(req.Justificativa) || req.Justificativa.Trim().Length < 15)
            return BadRequest(new { erro = "Justificativa deve ter pelo menos 15 caracteres." });

        var config = await db.ConfiguracoesFiscais.FirstOrDefaultAsync(c => c.LojaId == emissao.LojaId);
        if (config is null) return BadRequest(new { erro = "Configuração fiscal não encontrada." });

        var resultado = await provedorFiscal.CancelarNfceAsync(emissao, req.Justificativa.Trim(), config);
        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.MotivoErro ?? "Não foi possível cancelar a nota." });

        emissao.Status = "cancelada";
        emissao.ProcessadaEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ToDto(emissao));
    }
}
