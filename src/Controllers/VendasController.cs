using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/vendas")]
[Authorize]
public class VendasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        [FromQuery] Guid? clienteId)
    {
        var q = db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .AsQueryable();

        if (de.HasValue)        q = q.Where(v => v.CriadaEm >= de.Value);
        if (ate.HasValue)       q = q.Where(v => v.CriadaEm <= ate.Value.AddDays(1));
        if (clienteId.HasValue) q = q.Where(v => v.ClienteId == clienteId);

        var lista = await q.OrderByDescending(v => v.CriadaEm).Select(v => ToDto(v)).ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id)
    {
        var v = await db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == id);

        return v is null ? NotFound() : Ok(ToDto(v));
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] CriarVendaRequest req)
    {
        if (!req.Itens.Any())
            return BadRequest(new { erro = "A venda deve ter ao menos um item." });

        // Validar estoque de todos os produtos antes de qualquer alteração
        foreach (var item in req.Itens)
        {
            var produto = await db.Produtos.FindAsync(item.ProdutoId);
            if (produto is null)
                return BadRequest(new { erro = $"Produto {item.ProdutoId} não encontrado." });
            if (!produto.Ativo)
                return BadRequest(new { erro = $"Produto '{produto.Nome}' está inativo." });
            if (produto.Estoque < item.Quantidade)
                return BadRequest(new { erro = $"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.Estoque}." });
        }

        // Calcular totais
        decimal total = req.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
        decimal totalFinal = total - req.Desconto;

        var venda = new Venda
        {
            ClienteId      = req.ClienteId,
            Total          = total,
            Desconto       = req.Desconto,
            TotalFinal     = totalFinal,
            FormaPagamento = req.FormaPagamento,
            Troco          = req.Troco,
        };

        db.Vendas.Add(venda);

        // Adicionar itens e baixar estoque
        foreach (var item in req.Itens)
        {
            var produto = await db.Produtos.FindAsync(item.ProdutoId);

            db.ItensVenda.Add(new ItemVenda
            {
                VendaId        = venda.Id,
                ProdutoId      = item.ProdutoId,
                Quantidade     = item.Quantidade,
                PrecoUnitario  = item.PrecoUnitario,
                Subtotal       = item.Quantidade * item.PrecoUnitario,
            });

            // Baixa no estoque
            produto!.Estoque -= item.Quantidade;
            produto.AtualizadoEm = DateTime.UtcNow;

            // Registra movimento
            db.Movimentos.Add(new MovimentoEstoque
            {
                ProdutoId  = item.ProdutoId,
                Tipo       = "saida",
                Quantidade = item.Quantidade,
                Observacao = $"Venda #{venda.Id.ToString()[..8]}",
            });
        }

        await db.SaveChangesAsync();

        // Recarrega com navegação para retornar completo
        var vendaSalva = await db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstAsync(v => v.Id == venda.Id);

        return CreatedAtAction(nameof(Buscar), new { id = venda.Id }, ToDto(vendaSalva));
    }

    private static VendaDto ToDto(Venda v) => new(
        v.Id,
        v.ClienteId, v.Cliente?.Nome,
        v.Total, v.Desconto, v.TotalFinal,
        v.FormaPagamento, v.Troco,
        v.CriadaEm,
        v.Itens.Select(i => new ItemVendaDto(
            i.Id, i.ProdutoId, i.Produto?.Nome ?? "",
            i.Quantidade, i.PrecoUnitario, i.Subtotal
        )).ToList()
    );
}
