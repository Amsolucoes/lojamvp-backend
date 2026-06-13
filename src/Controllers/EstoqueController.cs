using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/estoque")]
[Authorize]
public class EstoqueController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet("movimentos")]
    public async Task<IActionResult> Movimentos(
        [FromQuery] Guid? produtoId,
        [FromQuery] string? tipo,
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate)
    {
        var lojaId = await GetLojaId();
        var q = db.Movimentos.Include(m => m.Produto).AsQueryable();

        if (lojaId.HasValue) q = q.Where(m => m.LojaId == lojaId);
        if (produtoId.HasValue) q = q.Where(m => m.ProdutoId == produtoId);
        if (!string.IsNullOrEmpty(tipo)) q = q.Where(m => m.Tipo == tipo);
        if (de.HasValue) q = q.Where(m => m.CriadoEm >= de.Value);
        if (ate.HasValue) q = q.Where(m => m.CriadoEm <= ate.Value.AddDays(1));

        var lista = await q.OrderByDescending(m => m.CriadoEm)
            .Select(m => new MovimentoDto(
                m.Id, m.ProdutoId, m.Produto.Nome,
                m.Tipo, m.Quantidade, m.Observacao, m.CriadoEm))
            .ToListAsync();

        return Ok(lista);
    }

    [HttpPost("ajuste")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Ajuste([FromBody] AjusteEstoqueRequest req)
    {
        var lojaId = await GetLojaId();
        var produto = await db.Produtos.FindAsync(req.ProdutoId);
        if (produto is null || (lojaId.HasValue && produto.LojaId != lojaId))
            return NotFound(new { erro = "Produto não encontrado." });

        var novoEstoque = req.Tipo == "entrada"
            ? produto.Estoque + req.Quantidade
            : req.Quantidade;

        produto.Estoque = novoEstoque;
        produto.AtualizadoEm = DateTime.UtcNow;

        db.Movimentos.Add(new MovimentoEstoque
        {
            ProdutoId = req.ProdutoId,
            Tipo = req.Tipo,
            Quantidade = req.Quantidade,
            Observacao = req.Observacao,
            LojaId = lojaId,
        });

        await db.SaveChangesAsync();

        return Ok(new
        {
            produtoId = produto.Id,
            nome = produto.Nome,
            estoqueAgora = produto.Estoque,
        });
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas()
    {
        var lojaId = await GetLojaId();
        var q = db.Produtos.Where(p => p.Ativo && p.Estoque <= p.EstoqueMinimo);
        if (lojaId.HasValue) q = q.Where(p => p.LojaId == lojaId);

        var alertas = await q.Select(p => new
        {
            p.Id,
            p.Nome,
            p.Categoria,
            p.Estoque,
            p.EstoqueMinimo,
            Zerado = p.Estoque == 0,
        }).ToListAsync();

        return Ok(alertas);
    }

    [HttpPost("ajuste-variacao")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AjusteVariacao([FromBody] AjusteVariacaoRequest req)
    {
        var variacao = await db.ProdutoVariacoes
            .Include(v => v.Produto)
            .FirstOrDefaultAsync(v => v.Id == req.VariacaoId && v.ProdutoId == req.ProdutoId);

        if (variacao is null) return NotFound(new { erro = "Variação não encontrada." });

        variacao.Estoque = req.Tipo == "entrada"
            ? variacao.Estoque + req.Quantidade
            : req.Quantidade;
        variacao.AtualizadoEm = DateTime.UtcNow;

        var label = string.Join(" / ", new[] { variacao.Tamanho, variacao.Cor }.Where(s => !string.IsNullOrWhiteSpace(s)));

        db.Movimentos.Add(new MovimentoEstoque
        {
            ProdutoId = req.ProdutoId,
            Tipo = req.Tipo,
            Quantidade = req.Quantidade,
            Observacao = $"{req.Observacao ?? (req.Tipo == "entrada" ? "Entrada" : "Ajuste")} - {label}",
            LojaId = await GetLojaId(),
        });

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Estoque atualizado.", estoqueAgora = variacao.Estoque });
    }
}