using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/estoque")]
[Authorize]
public class EstoqueController(AppDbContext db) : ControllerBase
{
    [HttpGet("movimentos")]
    public async Task<IActionResult> Movimentos(
        [FromQuery] Guid? produtoId,
        [FromQuery] string? tipo,
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate)
    {
        var q = db.Movimentos.Include(m => m.Produto).AsQueryable();

        if (produtoId.HasValue) q = q.Where(m => m.ProdutoId == produtoId);
        if (!string.IsNullOrEmpty(tipo)) q = q.Where(m => m.Tipo == tipo);
        if (de.HasValue)  q = q.Where(m => m.CriadoEm >= de.Value);
        if (ate.HasValue) q = q.Where(m => m.CriadoEm <= ate.Value.AddDays(1));

        var lista = await q
            .OrderByDescending(m => m.CriadoEm)
            .Select(m => new MovimentoDto(
                m.Id, m.ProdutoId, m.Produto.Nome,
                m.Tipo, m.Quantidade, m.Observacao, m.CriadoEm
            ))
            .ToListAsync();

        return Ok(lista);
    }

    [HttpPost("ajuste")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Ajuste([FromBody] AjusteEstoqueRequest req)
    {
        var produto = await db.Produtos.FindAsync(req.ProdutoId);
        if (produto is null) return NotFound(new { erro = "Produto não encontrado." });

        int novoEstoque;
        int qtdMovimento;

        if (req.Tipo == "entrada")
        {
            novoEstoque   = produto.Estoque + req.Quantidade;
            qtdMovimento  = req.Quantidade;
        }
        else // ajuste = valor absoluto
        {
            qtdMovimento  = req.Quantidade;
            novoEstoque   = req.Quantidade;
        }

        produto.Estoque      = novoEstoque;
        produto.AtualizadoEm = DateTime.UtcNow;

        db.Movimentos.Add(new MovimentoEstoque
        {
            ProdutoId  = req.ProdutoId,
            Tipo       = req.Tipo,
            Quantidade = qtdMovimento,
            Observacao = req.Observacao,
        });

        await db.SaveChangesAsync();

        return Ok(new
        {
            produtoId    = produto.Id,
            nome         = produto.Nome,
            estoqueAntes = req.Tipo == "entrada" ? produto.Estoque - qtdMovimento : 0,
            estoqueAgora = produto.Estoque,
        });
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas()
    {
        var alertas = await db.Produtos
            .Where(p => p.Ativo && p.Estoque <= p.EstoqueMinimo)
            .Select(p => new
            {
                p.Id, p.Nome, p.Categoria,
                p.Estoque, p.EstoqueMinimo,
                Zerado = p.Estoque == 0,
            })
            .ToListAsync();

        return Ok(alertas);
    }
}
