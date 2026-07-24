using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/chacara/faixas-preco")]
[Authorize]
public class FaixaPrecoChacaraController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.FaixasPrecoChacara
            .Where(f => f.LojaId == lojaId)
            .OrderBy(f => f.PessoasAte)
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarFaixaRequest(int PessoasAte, decimal ValorDiariaSemana, decimal ValorDiariaFimSemana, decimal ValorPacote2DiasFimSemana);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarFaixaRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (req.PessoasAte <= 0)
            return BadRequest(new { erro = "Informe um limite de pessoas válido." });

        var jaExiste = await db.FaixasPrecoChacara.AnyAsync(f => f.LojaId == lojaId && f.PessoasAte == req.PessoasAte);
        if (jaExiste)
            return Conflict(new { erro = "Já existe uma faixa com esse limite de pessoas." });

        var faixa = new FaixaPrecoChacara
        {
            LojaId = lojaId.Value,
            PessoasAte = req.PessoasAte,
            ValorDiariaSemana = req.ValorDiariaSemana,
            ValorDiariaFimSemana = req.ValorDiariaFimSemana,
            ValorPacote2DiasFimSemana = req.ValorPacote2DiasFimSemana,
        };
        db.FaixasPrecoChacara.Add(faixa);
        await db.SaveChangesAsync();

        return Ok(faixa);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarFaixaRequest req)
    {
        var lojaId = await GetLojaId();
        var faixa = await db.FaixasPrecoChacara.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (faixa is null) return NotFound();

        var jaExiste = await db.FaixasPrecoChacara.AnyAsync(f => f.LojaId == lojaId && f.PessoasAte == req.PessoasAte && f.Id != id);
        if (jaExiste)
            return Conflict(new { erro = "Já existe uma faixa com esse limite de pessoas." });

        faixa.PessoasAte = req.PessoasAte;
        faixa.ValorDiariaSemana = req.ValorDiariaSemana;
        faixa.ValorDiariaFimSemana = req.ValorDiariaFimSemana;
        faixa.ValorPacote2DiasFimSemana = req.ValorPacote2DiasFimSemana;

        await db.SaveChangesAsync();
        return Ok(faixa);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var lojaId = await GetLojaId();
        var faixa = await db.FaixasPrecoChacara.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (faixa is null) return NotFound();

        db.FaixasPrecoChacara.Remove(faixa);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Faixa excluída." });
    }
}