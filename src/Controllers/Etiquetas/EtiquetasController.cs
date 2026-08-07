using LojaApi.Data;
using LojaApi.src.Models.Etiquetas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.src.Controllers.Etiquetas;

[ApiController]
[Route("api/etiquetas/modelos")]
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

    // ── Lista todos os modelos da loja — cria um "Padrão" automaticamente na primeira vez ──
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var modelos = await db.ConfiguracoesEtiqueta
            .Where(c => c.LojaId == lojaId)
            .OrderByDescending(c => c.Padrao).ThenBy(c => c.Nome)
            .ToListAsync();

        if (modelos.Count == 0)
        {
            var padrao = new ConfiguracaoEtiqueta { LojaId = lojaId.Value, Nome = "Padrão", Padrao = true };
            db.ConfiguracoesEtiqueta.Add(padrao);
            await db.SaveChangesAsync();
            modelos.Add(padrao);
        }

        return Ok(modelos);
    }

    public record SalvarModeloRequest(
        string Nome,
        bool IncluirLogo, bool UsarLogoPropria, string? LogoEtiquetaUrl,
        bool IncluirNomeMarca, bool IncluirNomeProduto, bool IncluirPreco, bool IncluirCodigoBarras,
        decimal LarguraMm, decimal AlturaMm,
        string? CorTexto, string? CorFundo, string? FonteFamilia, int? EscalaFonte
    );

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarModeloRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Digite um nome para o modelo." });
        if (req.LarguraMm <= 0 || req.AlturaMm <= 0)
            return BadRequest(new { erro = "Largura e altura devem ser maiores que zero." });

        var modelo = new ConfiguracaoEtiqueta { LojaId = lojaId.Value };
        PreencherModelo(modelo, req);

        db.ConfiguracoesEtiqueta.Add(modelo);
        await db.SaveChangesAsync();
        return Ok(modelo);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarModeloRequest req)
    {
        var lojaId = await GetLojaId();
        var modelo = await db.ConfiguracoesEtiqueta.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (modelo is null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Digite um nome para o modelo." });
        if (req.LarguraMm <= 0 || req.AlturaMm <= 0)
            return BadRequest(new { erro = "Largura e altura devem ser maiores que zero." });

        PreencherModelo(modelo, req);
        await db.SaveChangesAsync();
        return Ok(modelo);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var modelo = await db.ConfiguracoesEtiqueta.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (modelo is null) return NotFound();

        var totalModelos = await db.ConfiguracoesEtiqueta.CountAsync(c => c.LojaId == lojaId);
        if (totalModelos <= 1)
            return BadRequest(new { erro = "Você precisa ter ao menos um modelo de etiqueta." });

        var eraPadrao = modelo.Padrao;
        db.ConfiguracoesEtiqueta.Remove(modelo);
        await db.SaveChangesAsync();

        if (eraPadrao)
        {
            var outro = await db.ConfiguracoesEtiqueta.FirstOrDefaultAsync(c => c.LojaId == lojaId);
            if (outro != null) { outro.Padrao = true; await db.SaveChangesAsync(); }
        }

        return Ok(new { mensagem = "Modelo excluído." });
    }

    [HttpPatch("{id:guid}/padrao")]
    public async Task<IActionResult> MarcarPadrao(Guid id)
    {
        var lojaId = await GetLojaId();
        var modelo = await db.ConfiguracoesEtiqueta.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (modelo is null) return NotFound();

        var todos = await db.ConfiguracoesEtiqueta.Where(c => c.LojaId == lojaId).ToListAsync();
        foreach (var m in todos) m.Padrao = m.Id == id;
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Modelo padrão atualizado." });
    }

    private static void PreencherModelo(ConfiguracaoEtiqueta modelo, SalvarModeloRequest req)
    {
        modelo.Nome = req.Nome.Trim();
        modelo.IncluirLogo = req.IncluirLogo;
        modelo.UsarLogoPropria = req.UsarLogoPropria;
        modelo.LogoEtiquetaUrl = string.IsNullOrWhiteSpace(req.LogoEtiquetaUrl) ? null : req.LogoEtiquetaUrl.Trim();
        modelo.IncluirNomeMarca = req.IncluirNomeMarca;
        modelo.IncluirNomeProduto = req.IncluirNomeProduto;
        modelo.IncluirPreco = req.IncluirPreco;
        modelo.IncluirCodigoBarras = req.IncluirCodigoBarras;
        modelo.LarguraMm = req.LarguraMm;
        modelo.AlturaMm = req.AlturaMm;
        modelo.CorTexto = string.IsNullOrWhiteSpace(req.CorTexto) ? "#000000" : req.CorTexto.Trim();
        modelo.CorFundo = string.IsNullOrWhiteSpace(req.CorFundo) ? "#FFFFFF" : req.CorFundo.Trim();
        modelo.FonteFamilia = string.IsNullOrWhiteSpace(req.FonteFamilia) ? "Arial, sans-serif" : req.FonteFamilia.Trim();
        modelo.EscalaFonte = req.EscalaFonte is >= 50 and <= 300 ? req.EscalaFonte.Value : 100;
        modelo.AtualizadoEm = DateTime.UtcNow;
    }
}