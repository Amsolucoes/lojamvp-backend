using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/chacara/horarios")]
[Authorize]
public partial class HorarioChacaraController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex HoraValidaRegex();

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.HorariosChacara
            .Where(h => h.LojaId == lojaId)
            .OrderBy(h => h.Tipo).ThenBy(h => h.Ordem).ThenBy(h => h.Hora)
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarHorarioRequest(string Tipo, string Hora, decimal Ajuste, int Ordem);

    private static IActionResult? ValidarHorario(SalvarHorarioRequest req)
    {
        if (req.Tipo != "entrada" && req.Tipo != "saida")
            return new BadRequestObjectResult(new { erro = "Tipo deve ser 'entrada' ou 'saida'." });
        if (!HoraValidaRegex().IsMatch(req.Hora))
            return new BadRequestObjectResult(new { erro = "Informe um horário válido (HH:mm)." });
        return null;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarHorarioRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var erroValidacao = ValidarHorario(req);
        if (erroValidacao != null) return erroValidacao;

        var jaExiste = await db.HorariosChacara.AnyAsync(h => h.LojaId == lojaId && h.Tipo == req.Tipo && h.Hora == req.Hora);
        if (jaExiste)
            return Conflict(new { erro = "Já existe esse horário cadastrado para esse tipo." });

        var horario = new HorarioChacara
        {
            LojaId = lojaId.Value,
            Tipo = req.Tipo,
            Hora = req.Hora,
            Ajuste = req.Ajuste,
            Ordem = req.Ordem,
        };
        db.HorariosChacara.Add(horario);
        await db.SaveChangesAsync();

        return Ok(horario);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarHorarioRequest req)
    {
        var lojaId = await GetLojaId();
        var horario = await db.HorariosChacara.FirstOrDefaultAsync(h => h.Id == id && h.LojaId == lojaId);
        if (horario is null) return NotFound();

        var erroValidacao = ValidarHorario(req);
        if (erroValidacao != null) return erroValidacao;

        var jaExiste = await db.HorariosChacara.AnyAsync(h => h.LojaId == lojaId && h.Tipo == req.Tipo && h.Hora == req.Hora && h.Id != id);
        if (jaExiste)
            return Conflict(new { erro = "Já existe esse horário cadastrado para esse tipo." });

        horario.Tipo = req.Tipo;
        horario.Hora = req.Hora;
        horario.Ajuste = req.Ajuste;
        horario.Ordem = req.Ordem;

        await db.SaveChangesAsync();
        return Ok(horario);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var lojaId = await GetLojaId();
        var horario = await db.HorariosChacara.FirstOrDefaultAsync(h => h.Id == id && h.LojaId == lojaId);
        if (horario is null) return NotFound();

        db.HorariosChacara.Remove(horario);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Horário excluído." });
    }
}
