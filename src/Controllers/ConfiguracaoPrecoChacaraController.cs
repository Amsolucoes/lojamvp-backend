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
        decimal ValorDiariaSemana,
        decimal ValorDiariaFimSemana,
        decimal ValorDiariaFimSemanaGrande,
        decimal ValorPacote2DiasFimSemana,
        decimal ValorPacote2DiasFimSemanaGrande,
        int LimitePessoasPacotePequeno,
        int MinimoPessoas,
        decimal ValorTaxaLimpeza,
        decimal ValorMultaNaoLimpeza,
        decimal PercentualEntradaMinimo
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

        cfg.ValorDiariaSemana = req.ValorDiariaSemana;
        cfg.ValorDiariaFimSemana = req.ValorDiariaFimSemana;
        cfg.ValorDiariaFimSemanaGrande = req.ValorDiariaFimSemanaGrande;
        cfg.ValorPacote2DiasFimSemana = req.ValorPacote2DiasFimSemana;
        cfg.ValorPacote2DiasFimSemanaGrande = req.ValorPacote2DiasFimSemanaGrande;
        cfg.LimitePessoasPacotePequeno = req.LimitePessoasPacotePequeno;
        cfg.MinimoPessoas = req.MinimoPessoas;
        cfg.ValorTaxaLimpeza = req.ValorTaxaLimpeza;
        cfg.ValorMultaNaoLimpeza = req.ValorMultaNaoLimpeza;
        cfg.PercentualEntradaMinimo = req.PercentualEntradaMinimo;

        await db.SaveChangesAsync();
        return Ok(cfg);
    }
}