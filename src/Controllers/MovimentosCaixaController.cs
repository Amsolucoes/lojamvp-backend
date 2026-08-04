using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/movimentos-caixa")]
[Authorize]
public class MovimentosCaixaController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateTime? de, [FromQuery] DateTime? ate)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.MovimentosCaixa.Where(m => m.LojaId == lojaId);
        if (de.HasValue) q = q.Where(m => m.Data >= de.Value.Date);
        if (ate.HasValue) q = q.Where(m => m.Data <= ate.Value.Date.AddDays(1));

        var lista = await q.OrderByDescending(m => m.Data).ThenByDescending(m => m.CriadoEm)
            .Select(m => new
            {
                m.Id,
                m.Tipo,
                m.Valor,
                m.Data,
                m.OrigemNome,
                m.Observacao,
                m.CriadoEm,
            })
            .ToListAsync();

        return Ok(lista);
    }

    public record CriarMovimentoRequest(string Tipo, decimal Valor, DateTime Data, Guid? OrigemVendaId, string? Observacao, Guid? ContaBancariaId);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarMovimentoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (req.Tipo != "entrada" && req.Tipo != "saida")
            return BadRequest(new { erro = "Tipo inválido." });

        if (req.Valor <= 0)
            return BadRequest(new { erro = "Informe um valor maior que zero." });

        var hoje = DateTime.UtcNow.Date;
        if (req.Data.Date > hoje)
            return BadRequest(new { erro = "A data não pode ser no futuro." });

        string? origemNome = null;
        if (req.OrigemVendaId.HasValue)
        {
            var origem = await db.OrigensVenda.FirstOrDefaultAsync(o => o.Id == req.OrigemVendaId.Value && o.LojaId == lojaId);
            origemNome = origem?.Nome;
        }

        var dataUtc = DateTime.SpecifyKind(req.Data.Date, DateTimeKind.Utc).AddHours(12);

        var movimento = new MovimentoCaixa
        {
            LojaId = lojaId.Value,
            Tipo = req.Tipo,
            Valor = req.Valor,
            Data = dataUtc,
            OrigemVendaId = req.OrigemVendaId,
            OrigemNome = origemNome,
            Observacao = req.Observacao,
            ContaBancariaId = req.ContaBancariaId,
        };

        // Se a loja tem Financeiro ativo e uma conta foi escolhida, espelha como ajuste de saldo
        if (req.ContaBancariaId.HasValue)
        {
            var conta = await db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == req.ContaBancariaId.Value && c.LojaId == lojaId);
            if (conta != null)
            {
                var ajuste = new AjusteContaBancaria
                {
                    LojaId = lojaId.Value,
                    ContaBancariaId = conta.Id,
                    Tipo = req.Tipo, // "entrada" ou "saida", mesmo vocabulário
                    Valor = req.Valor,
                    Observacao = $"{(req.Tipo == "entrada" ? "Reforço de caixa" : "Sangria de caixa")}" + (origemNome != null ? $" — {origemNome}" : "") + (req.Observacao != null ? $" ({req.Observacao})" : ""),
                    CriadoEm = dataUtc,
                };
                db.AjustesContaBancaria.Add(ajuste);
                await db.SaveChangesAsync();
                movimento.AjusteContaBancariaId = ajuste.Id;
            }
        }

        db.MovimentosCaixa.Add(movimento);
        await db.SaveChangesAsync();

        return Ok(movimento);
    }

    public record EditarMovimentoRequest(string Tipo, decimal Valor, DateTime Data, Guid? OrigemVendaId, string? Observacao, Guid? ContaBancariaId);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarMovimentoRequest req)
    {
        var lojaId = await GetLojaId();
        var movimento = await db.MovimentosCaixa.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);
        if (movimento is null) return NotFound();

        if (req.Tipo != "entrada" && req.Tipo != "saida")
            return BadRequest(new { erro = "Tipo inválido." });
        if (req.Valor <= 0)
            return BadRequest(new { erro = "Informe um valor maior que zero." });

        var hoje = DateTime.UtcNow.Date;
        if (req.Data.Date > hoje)
            return BadRequest(new { erro = "A data não pode ser no futuro." });

        string? origemNome = null;
        if (req.OrigemVendaId.HasValue)
        {
            var origem = await db.OrigensVenda.FirstOrDefaultAsync(o => o.Id == req.OrigemVendaId.Value && o.LojaId == lojaId);
            origemNome = origem?.Nome;
        }

        var dataUtc = DateTime.SpecifyKind(req.Data.Date, DateTimeKind.Utc).AddHours(12);

        movimento.Tipo = req.Tipo;
        movimento.Valor = req.Valor;
        movimento.Data = dataUtc;
        movimento.OrigemVendaId = req.OrigemVendaId;
        movimento.OrigemNome = origemNome;
        movimento.Observacao = req.Observacao;

        // Remove o espelho antigo (se existir) e recria do zero — mais simples e seguro
        // do que tentar ajustar o ajuste existente pra cada combinação de mudança possível
        if (movimento.AjusteContaBancariaId.HasValue)
        {
            var ajusteAntigo = await db.AjustesContaBancaria.FindAsync(movimento.AjusteContaBancariaId.Value);
            if (ajusteAntigo != null) db.AjustesContaBancaria.Remove(ajusteAntigo);
            movimento.AjusteContaBancariaId = null;
        }

        if (req.ContaBancariaId.HasValue)
        {
            var conta = await db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == req.ContaBancariaId.Value && c.LojaId == lojaId);
            if (conta != null)
            {
                var ajuste = new AjusteContaBancaria
                {
                    LojaId = lojaId.Value,
                    ContaBancariaId = conta.Id,
                    Tipo = req.Tipo,
                    Valor = req.Valor,
                    Observacao = $"{(req.Tipo == "entrada" ? "Reforço de caixa" : "Sangria de caixa")}" + (origemNome != null ? $" — {origemNome}" : "") + (req.Observacao != null ? $" ({req.Observacao})" : ""),
                    CriadoEm = dataUtc,
                };
                db.AjustesContaBancaria.Add(ajuste);
                await db.SaveChangesAsync();
                movimento.ContaBancariaId = conta.Id;
                movimento.AjusteContaBancariaId = ajuste.Id;
            }
        }
        else
        {
            movimento.ContaBancariaId = null;
        }

        await db.SaveChangesAsync();
        return Ok(movimento);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var movimento = await db.MovimentosCaixa.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);
        if (movimento is null) return NotFound();

        // Remove o espelho no Financeiro também, se existir
        if (movimento.AjusteContaBancariaId.HasValue)
        {
            var ajuste = await db.AjustesContaBancaria.FindAsync(movimento.AjusteContaBancariaId.Value);
            if (ajuste != null) db.AjustesContaBancaria.Remove(ajuste);
        }

        db.MovimentosCaixa.Remove(movimento);
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Movimento excluído." });
    }
}