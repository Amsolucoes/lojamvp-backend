using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using System.Security.Claims;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/loja/modulos")]
[Authorize]
public class LojaModulosController(AppDbContext db) : ControllerBase
{
    public record AtualizarModulosRequest(string ModulosAtivos);

    public record AtualizarModulosResponse(
        string ModulosAtivos,
        decimal NovaMensalidade
    );

    [HttpPatch]
    public async Task<IActionResult> Atualizar([FromBody] AtualizarModulosRequest req)
    {
        var lojaIdClaim = User.FindFirstValue("lojaId");
        if (!Guid.TryParse(lojaIdClaim, out var lojaId))
            return Unauthorized();

        var loja = await db.Lojas.FindAsync(lojaId);
        if (loja is null) return NotFound();

        // Calcula nova mensalidade: base do plano + soma dos módulos ativos
        var basePlano = loja.TipoPlano switch
        {
            "loja" => 89.90m,
            "loja_modulos" => 89.90m,
            "servicos" => 79.90m,
            "financeiro" => 39.90m,
            _ => 0m
        };

        var chavesAtivas = (req.ModulosAtivos ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();

        var modulosPreco = await db.ModulosPreco
            .Where(m => chavesAtivas.Contains(m.Chave) && m.DisponivelParaAtivar)
            .ToListAsync();

        var somaModulos = modulosPreco.Sum(m => m.Valor);
        var novaMensalidade = Math.Round(basePlano + somaModulos, 2);

        // Filtra só os módulos que existem e estão disponíveis
        var chavesValidas = modulosPreco.Select(m => m.Chave);
        loja.ModulosAtivos = string.Join(",", chavesValidas);
        loja.MensalidadeValor = novaMensalidade;
        loja.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new AtualizarModulosResponse(loja.ModulosAtivos, novaMensalidade));
    }

    [HttpGet("preco-simulado")]
    public async Task<IActionResult> SimularPreco([FromQuery] string modulosAtivos)
    {
        var lojaIdClaim = User.FindFirstValue("lojaId");
        if (!Guid.TryParse(lojaIdClaim, out var lojaId))
            return Unauthorized();

        var loja = await db.Lojas.FindAsync(lojaId);
        if (loja is null) return NotFound();

        var basePlano = loja.TipoPlano switch
        {
            "loja" => 89.90m,
            "loja_modulos" => 89.90m,
            "servicos" => 79.90m,
            "financeiro" => 39.90m,
            _ => 0m
        };

        var chaves = (modulosAtivos ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();

        var soma = await db.ModulosPreco
            .Where(m => chaves.Contains(m.Chave) && m.DisponivelParaAtivar)
            .SumAsync(m => m.Valor);

        return Ok(new { novaMensalidade = Math.Round(basePlano + soma, 2) });
    }
}