using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/produtos")]
[Authorize]
public class ProdutosController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] string? categoria,
        [FromQuery] bool? ativo)
    {
        var lojaId = await GetLojaId();
        var q = db.Produtos.AsQueryable();

        if (lojaId.HasValue)
            q = q.Where(p => p.LojaId == lojaId);

        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(p => p.Nome.ToLower().Contains(busca.ToLower()) ||
                              (p.CodigoBarras != null && p.CodigoBarras.Contains(busca)));
        if (!string.IsNullOrWhiteSpace(categoria))
            q = q.Where(p => p.Categoria == categoria);
        if (ativo.HasValue)
            q = q.Where(p => p.Ativo == ativo.Value);

        var lista = await q.OrderBy(p => p.Nome).Select(p => ToDto(p)).ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id)
    {
        var lojaId = await GetLojaId();
        var p = await db.Produtos.FindAsync(id);
        if (p is null || (lojaId.HasValue && p.LojaId != lojaId)) return NotFound();
        return Ok(ToDto(p));
    }

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarProdutoRequest req)
    {
        var lojaId = await GetLojaId();

        var produto = new Produto
        {
            Nome = req.Nome,
            Descricao = req.Descricao,
            Categoria = req.Categoria,
            PrecoCusto = req.PrecoCusto,
            PrecoVenda = req.PrecoVenda,
            Estoque = req.Estoque,
            EstoqueMinimo = req.EstoqueMinimo,
            CodigoBarras = req.CodigoBarras,
            Ativo = req.Ativo,
            LojaId = lojaId,
        };
        db.Produtos.Add(produto);

        if (req.Estoque > 0)
            db.Movimentos.Add(new MovimentoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = "entrada",
                Quantidade = req.Estoque,
                Observacao = "Estoque inicial",
                LojaId = lojaId,
            });

        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Buscar), new { id = produto.Id }, ToDto(produto));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarProdutoRequest req)
    {
        var lojaId = await GetLojaId();
        var produto = await db.Produtos.FindAsync(id);
        if (produto is null || (lojaId.HasValue && produto.LojaId != lojaId)) return NotFound();

        produto.Nome = req.Nome; produto.Descricao = req.Descricao;
        produto.Categoria = req.Categoria; produto.PrecoCusto = req.PrecoCusto;
        produto.PrecoVenda = req.PrecoVenda; produto.EstoqueMinimo = req.EstoqueMinimo;
        produto.CodigoBarras = req.CodigoBarras; produto.Ativo = req.Ativo;
        produto.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToDto(produto));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var lojaId = await GetLojaId();
        var produto = await db.Produtos.FindAsync(id);
        if (produto is null || (lojaId.HasValue && produto.LojaId != lojaId)) return NotFound();

        produto.Ativo = false;
        produto.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ProdutoDto ToDto(Produto p) => new(
        p.Id, p.Nome, p.Descricao, p.Categoria,
        p.PrecoCusto, p.PrecoVenda, p.Estoque, p.EstoqueMinimo,
        p.CodigoBarras, p.Ativo, p.CriadoEm, p.AtualizadoEm
    );
}