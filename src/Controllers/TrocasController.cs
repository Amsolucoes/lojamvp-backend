using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.Models;
using LojaApi.DTOs;
using System.Security.Claims;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrocasController(AppDbContext db) : ControllerBase
{
    private async Task<Guid?> GetLojaId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return null;
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == Guid.Parse(userId));
        return vinculo?.LojaId;
    }

    // ── Registrar troca ───────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] CriarTrocaRequest req)
    {
        var lojaId = await GetLojaId();

        var cliente = await db.Clientes.FindAsync(req.ClienteId);
        if (cliente is null) return BadRequest(new { erro = "Cliente não encontrado." });

        if (!req.Devolvidos.Any() || !req.Novos.Any())
            return BadRequest(new { erro = "A troca precisa de ao menos um produto devolvido e um novo." });

        // Valida estoque dos novos
        foreach (var item in req.Novos)
        {
            if (item.VariacaoId.HasValue)
            {
                var variacao = await db.ProdutoVariacoes.FindAsync(item.VariacaoId.Value);
                if (variacao is null || variacao.Estoque < item.Quantidade)
                    return BadRequest(new { erro = $"Estoque insuficiente para '{item.NomeProduto}'." });
            }
            else
            {
                var produto = await db.Produtos.FindAsync(item.ProdutoId);
                if (produto is null || produto.Estoque < item.Quantidade)
                    return BadRequest(new { erro = $"Estoque insuficiente para '{item.NomeProduto}'." });
            }
        }

        decimal totalDevolvido = req.Devolvidos.Sum(i => i.Quantidade * i.PrecoUnitario);
        decimal totalNovo = req.Novos.Sum(i => i.Quantidade * i.PrecoUnitario);
        decimal diferenca = totalNovo - totalDevolvido;
        decimal creditoGerado = diferenca < 0 ? Math.Abs(diferenca) : 0;

        var troca = new Troca
        {
            ClienteId = req.ClienteId,
            TotalDevolvido = totalDevolvido,
            TotalNovo = totalNovo,
            Diferenca = diferenca,
            CreditoGerado = creditoGerado,
            FormaPagamento = diferenca > 0 ? req.FormaPagamento : null,
            LojaId = lojaId,
        };
        db.Trocas.Add(troca);

        // Devolvidos — volta estoque se marcado
        foreach (var item in req.Devolvidos)
        {
            db.ItensTroca.Add(new ItemTroca
            {
                TrocaId = troca.Id,
                ProdutoId = item.ProdutoId,
                NomeProduto = item.NomeProduto,
                VariacaoId = item.VariacaoId,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                Tipo = "devolvido",
                VoltaEstoque = item.VoltaEstoque,
            });

            if (item.VoltaEstoque)
            {
                if (item.VariacaoId.HasValue)
                {
                    var variacao = await db.ProdutoVariacoes.FindAsync(item.VariacaoId.Value);
                    if (variacao != null) { variacao.Estoque += item.Quantidade; variacao.AtualizadoEm = DateTime.UtcNow; }
                }
                else
                {
                    var produto = await db.Produtos.FindAsync(item.ProdutoId);
                    if (produto != null) { produto.Estoque += item.Quantidade; produto.AtualizadoEm = DateTime.UtcNow; }
                }

                db.Movimentos.Add(new MovimentoEstoque
                {
                    ProdutoId = item.ProdutoId,
                    Tipo = "entrada",
                    Quantidade = item.Quantidade,
                    Observacao = $"Troca - devolução {item.NomeProduto}",
                    LojaId = lojaId,
                });
            }
        }

        // Novos — baixa estoque
        foreach (var item in req.Novos)
        {
            db.ItensTroca.Add(new ItemTroca
            {
                TrocaId = troca.Id,
                ProdutoId = item.ProdutoId,
                NomeProduto = item.NomeProduto,
                VariacaoId = item.VariacaoId,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                Tipo = "novo",
            });

            if (item.VariacaoId.HasValue)
            {
                var variacao = await db.ProdutoVariacoes.FindAsync(item.VariacaoId.Value);
                if (variacao != null) { variacao.Estoque -= item.Quantidade; variacao.AtualizadoEm = DateTime.UtcNow; }
            }
            else
            {
                var produto = await db.Produtos.FindAsync(item.ProdutoId);
                if (produto != null) { produto.Estoque -= item.Quantidade; produto.AtualizadoEm = DateTime.UtcNow; }
            }

            db.Movimentos.Add(new MovimentoEstoque
            {
                ProdutoId = item.ProdutoId,
                Tipo = "saida",
                Quantidade = item.Quantidade,
                Observacao = $"Troca - saída {item.NomeProduto}",
                LojaId = lojaId,
            });
        }

        // Crédito gerado vai pro cliente
        if (creditoGerado > 0)
            cliente.CreditoLoja += creditoGerado;

        await db.SaveChangesAsync();

        return Ok(new
        {
            id = troca.Id,
            totalDevolvido,
            totalNovo,
            diferenca,
            creditoGerado,
            creditoTotalCliente = cliente.CreditoLoja,
            mensagem = diferenca > 0
                ? $"Cliente deve pagar a diferença de {diferenca:C}"
                : diferenca < 0
                    ? $"Crédito de {creditoGerado:C} gerado para o cliente"
                    : "Troca realizada sem diferença"
        });
    }

    // ── Listar trocas ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        var q = db.Trocas.Include(t => t.Cliente).Include(t => t.Itens).AsQueryable();
        if (lojaId.HasValue) q = q.Where(t => t.LojaId == lojaId);

        var trocas = await q.OrderByDescending(t => t.CriadaEm).Take(50)
            .Select(t => new
            {
                t.Id,
                t.ClienteId,
                nomeCliente = t.Cliente.Nome,
                t.TotalDevolvido,
                t.TotalNovo,
                t.Diferenca,
                t.CreditoGerado,
                t.FormaPagamento,
                t.CriadaEm,
                itens = t.Itens.Select(i => new
                {
                    i.NomeProduto,
                    i.Quantidade,
                    i.PrecoUnitario,
                    i.Tipo,
                    i.VoltaEstoque
                })
            })
            .ToListAsync();

        return Ok(trocas);
    }
}