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
public class InfoChacaraController(AppDbContext db, LojaApi.Services.MercadoPagoService mp, IConfiguration config) : ControllerBase
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
        string? HoraEntrada, string? HoraSaida, string? AvisosUso,
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
        info.HoraEntrada = string.IsNullOrWhiteSpace(req.HoraEntrada) ? "08:00" : req.HoraEntrada.Trim();
        info.HoraSaida = string.IsNullOrWhiteSpace(req.HoraSaida) ? "22:00" : req.HoraSaida.Trim();
        info.AvisosUso = string.IsNullOrWhiteSpace(req.AvisosUso) ? null : req.AvisosUso.Trim();
        info.LocadorNome = string.IsNullOrWhiteSpace(req.LocadorNome) ? null : req.LocadorNome.Trim();
        info.LocadorRg = string.IsNullOrWhiteSpace(req.LocadorRg) ? null : req.LocadorRg.Trim();
        info.LocadorCpf = string.IsNullOrWhiteSpace(req.LocadorCpf) ? null : req.LocadorCpf.Trim();
        info.LocadorEndereco = string.IsNullOrWhiteSpace(req.LocadorEndereco) ? null : req.LocadorEndereco.Trim();
        info.LocadorTelefone = string.IsNullOrWhiteSpace(req.LocadorTelefone) ? null : req.LocadorTelefone.Trim();
        info.CidadeAssinatura = string.IsNullOrWhiteSpace(req.CidadeAssinatura) ? null : req.CidadeAssinatura.Trim();

        await db.SaveChangesAsync();
        return Ok(info);
    }

    // ── Pagamento online (Mercado Pago) da própria loja ───────────
    public record PagamentoReq(string AccessToken, string PublicKey);

    private async Task<InfoChacara> ObterOuCriarAsync(Guid lojaId)
    {
        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == lojaId);
        if (info is null)
        {
            info = new InfoChacara { LojaId = lojaId };
            db.InfosChacara.Add(info);
            await db.SaveChangesAsync();
        }
        return info;
    }

    [HttpGet("pagamento")]
    public async Task<IActionResult> ObterPagamento()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });
        var info = await ObterOuCriarAsync(lojaId.Value);
        var token = LojaApi.Services.SegredoService.Decifrar(info.MpAccessTokenCifrado, config);
        return Ok(new
        {
            configurado = token != null,
            contaPlataforma = info.MpContaPlataforma && token == null,
            tokenFinal = token != null && token.Length > 4 ? token[^4..] : null,
            publicKey = info.MpPublicKey,
        });
    }

    [HttpPut("pagamento")]
    public async Task<IActionResult> SalvarPagamento([FromBody] PagamentoReq req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });
        if (string.IsNullOrWhiteSpace(req.AccessToken) || string.IsNullOrWhiteSpace(req.PublicKey))
            return BadRequest(new { erro = "Informe o Access Token e a Public Key do Mercado Pago." });

        var token = req.AccessToken.Trim();
        var (ok, email) = await mp.ValidarToken(token);
        if (!ok) return BadRequest(new { erro = "O Mercado Pago não aceitou esse Access Token. Confira se copiou o de produção." });

        var info = await ObterOuCriarAsync(lojaId.Value);
        info.MpAccessTokenCifrado = LojaApi.Services.SegredoService.Cifrar(token, config);
        info.MpPublicKey = req.PublicKey.Trim();
        await db.SaveChangesAsync();
        return Ok(new { configurado = true, contaMercadoPago = email });
    }

    [HttpDelete("pagamento")]
    public async Task<IActionResult> RemoverPagamento()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });
        var info = await ObterOuCriarAsync(lojaId.Value);
        info.MpAccessTokenCifrado = null;
        info.MpPublicKey = null;
        await db.SaveChangesAsync();
        return Ok(new { configurado = false });
    }
}
