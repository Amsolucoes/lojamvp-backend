using LojaApi.Data;
using LojaApi.src.Models.OrdemServico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;

namespace LojaApi.src.Controllers.OrdemServico;

[ApiController]
[Route("api/ordemservico/patrimonio")]
[Authorize]
public class PatrimonioController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ══════════════ Itens de patrimônio (cadastro) ══════════════

    [HttpGet("itens")]
    public async Task<IActionResult> ListarItens([FromQuery] bool incluirInativos = false)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.ItensPatrimonio.Where(i => i.LojaId == lojaId);
        if (!incluirInativos) q = q.Where(i => i.Ativo);

        var lista = await q
            .OrderBy(i => i.Categoria).ThenBy(i => i.Nome)
            .Select(i => new
            {
                i.Id,
                i.Nome,
                i.Categoria,
                i.QuantidadeEsperada,
                i.ValorUnitario,
                i.Observacao,
                i.Ativo,
                valorTotal = i.QuantidadeEsperada * i.ValorUnitario,
            })
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarItemPatrimonioRequest(string Nome, string? Categoria, int QuantidadeEsperada, decimal ValorUnitario, string? Observacao, bool Ativo);

    [HttpPost("itens")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CriarItem([FromBody] SalvarItemPatrimonioRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Nome é obrigatório." });

        var item = new ItemPatrimonio
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Categoria = string.IsNullOrWhiteSpace(req.Categoria) ? null : req.Categoria.Trim(),
            QuantidadeEsperada = req.QuantidadeEsperada,
            ValorUnitario = req.ValorUnitario,
            Observacao = req.Observacao,
            Ativo = req.Ativo,
        };
        db.ItensPatrimonio.Add(item);
        await db.SaveChangesAsync();

        return Ok(new { item.Id, item.Nome, item.Categoria, item.QuantidadeEsperada, item.ValorUnitario, item.Ativo });
    }

    [HttpPut("itens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AtualizarItem(Guid id, [FromBody] SalvarItemPatrimonioRequest req)
    {
        var lojaId = await GetLojaId();
        var item = await db.ItensPatrimonio.FirstOrDefaultAsync(i => i.Id == id && i.LojaId == lojaId);
        if (item is null) return NotFound();

        item.Nome = req.Nome.Trim();
        item.Categoria = string.IsNullOrWhiteSpace(req.Categoria) ? null : req.Categoria.Trim();
        item.QuantidadeEsperada = req.QuantidadeEsperada;
        item.ValorUnitario = req.ValorUnitario;
        item.Observacao = req.Observacao;
        item.Ativo = req.Ativo;
        item.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { item.Id, item.Nome, item.Categoria, item.QuantidadeEsperada, item.ValorUnitario, item.Ativo });
    }

    [HttpDelete("itens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> ExcluirItem(Guid id)
    {
        var lojaId = await GetLojaId();
        var item = await db.ItensPatrimonio.FirstOrDefaultAsync(i => i.Id == id && i.LojaId == lojaId);
        if (item is null) return NotFound();

        var jaContado = await db.ItensContagemPatrimonio.AnyAsync(c => c.ItemPatrimonioId == id);
        if (jaContado)
        {
            item.Ativo = false;
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Item já usado em alguma contagem — foi desativado em vez de excluído." });
        }

        db.ItensPatrimonio.Remove(item);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Item excluído." });
    }

    // ══════════════ Contagens ══════════════

    [HttpGet("contagens")]
    public async Task<IActionResult> ListarContagens()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.ContagensPatrimonio
            .Where(c => c.LojaId == lojaId)
            .OrderByDescending(c => c.DataContagem)
            .Select(c => new
            {
                c.Id,
                c.DataContagem,
                c.Responsavel,
                c.Observacao,
                qtdItens = c.Itens.Count,
                qtdDivergentes = c.Itens.Count(i => i.QuantidadeContada != i.QuantidadeEsperadaNoMomento),
            })
            .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("contagens/{id:guid}")]
    public async Task<IActionResult> BuscarContagem(Guid id)
    {
        var lojaId = await GetLojaId();
        var contagem = await db.ContagensPatrimonio
            .Include(c => c.Itens).ThenInclude(i => i.ItemPatrimonio)
            .FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

        if (contagem is null) return NotFound();

        return Ok(new
        {
            contagem.Id,
            contagem.DataContagem,
            contagem.Responsavel,
            contagem.Observacao,
            itens = contagem.Itens.Select(i => new
            {
                i.Id,
                i.ItemPatrimonioId,
                nomeItem = i.ItemPatrimonio!.Nome,
                categoria = i.ItemPatrimonio.Categoria,
                i.QuantidadeEsperadaNoMomento,
                i.QuantidadeContada,
                diferenca = i.QuantidadeContada - i.QuantidadeEsperadaNoMomento,
                valorUnitario = i.ItemPatrimonio.ValorUnitario,
                i.Observacao,
            }),
        });
    }

    public record ItemContagemRequest(Guid ItemPatrimonioId, int QuantidadeContada, string? Observacao);
    public record CriarContagemRequest(DateTime? DataContagem, string? Responsavel, string? Observacao, List<ItemContagemRequest> Itens);

    [HttpPost("contagens")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> CriarContagem([FromBody] CriarContagemRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (req.Itens is null || req.Itens.Count == 0)
            return BadRequest(new { erro = "A contagem precisa ter ao menos um item." });

        var itemIds = req.Itens.Select(i => i.ItemPatrimonioId).ToList();
        var itensPatrimonio = await db.ItensPatrimonio
            .Where(i => itemIds.Contains(i.Id) && i.LojaId == lojaId)
            .ToListAsync();

        if (itensPatrimonio.Count != itemIds.Distinct().Count())
            return BadRequest(new { erro = "Algum item de patrimônio informado não foi encontrado." });

        var contagem = new ContagemPatrimonio
        {
            LojaId = lojaId.Value,
            DataContagem = req.DataContagem.HasValue ? DateTime.SpecifyKind(req.DataContagem.Value, DateTimeKind.Utc) : DateTime.UtcNow,
            Responsavel = req.Responsavel,
            Observacao = req.Observacao,
        };

        foreach (var i in req.Itens)
        {
            var itemPatrimonio = itensPatrimonio.First(x => x.Id == i.ItemPatrimonioId);
            contagem.Itens.Add(new ItemContagemPatrimonio
            {
                LojaId = lojaId.Value,
                ItemPatrimonioId = itemPatrimonio.Id,
                QuantidadeEsperadaNoMomento = itemPatrimonio.QuantidadeEsperada, // snapshot da época
                QuantidadeContada = i.QuantidadeContada,
                Observacao = i.Observacao,
            });
        }

        db.ContagensPatrimonio.Add(contagem);
        await db.SaveChangesAsync();

        return Ok(new { contagem.Id, contagem.DataContagem });
    }

    [HttpDelete("contagens/{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> ExcluirContagem(Guid id)
    {
        var lojaId = await GetLojaId();
        var contagem = await db.ContagensPatrimonio.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (contagem is null) return NotFound();

        db.ContagensPatrimonio.Remove(contagem); // cascade remove os itens junto
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Contagem excluída." });
    }

    // ══════════════ PDF ══════════════

    // PDF do cadastro atual (lista de itens + valor total de patrimônio)
    [HttpGet("relatorio-pdf")]
    public async Task<IActionResult> RelatorioPdf()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return NotFound();

        var loja = await db.Lojas.FindAsync(lojaId.Value);
        var itens = await db.ItensPatrimonio
            .Where(i => i.LojaId == lojaId && i.Ativo)
            .OrderBy(i => i.Categoria).ThenBy(i => i.Nome)
            .ToListAsync();

        var valorTotal = itens.Sum(i => i.QuantidadeEsperada * i.ValorUnitario);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Text($"Patrimônio — {loja?.Nome ?? ""}").FontSize(16).Bold();

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); // nome
                        c.RelativeColumn(2); // categoria
                        c.RelativeColumn(1); // qtd
                        c.RelativeColumn(1.5f); // valor unit
                        c.RelativeColumn(1.5f); // total
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Item").Bold();
                        h.Cell().Text("Categoria").Bold();
                        h.Cell().Text("Qtd").Bold();
                        h.Cell().Text("Valor unit.").Bold();
                        h.Cell().Text("Total").Bold();
                    });

                    foreach (var i in itens)
                    {
                        table.Cell().Text(i.Nome);
                        table.Cell().Text(i.Categoria ?? "—");
                        table.Cell().Text(i.QuantidadeEsperada.ToString());
                        table.Cell().Text($"R$ {i.ValorUnitario:N2}");
                        table.Cell().Text($"R$ {(i.QuantidadeEsperada * i.ValorUnitario):N2}");
                    }
                });

                page.Footer().AlignRight().Text($"Valor total do patrimônio: R$ {valorTotal:N2}").Bold();
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"patrimonio-{DateTime.UtcNow:yyyy-MM-dd}.pdf");
    }

    // PDF de uma contagem específica, com comparativo esperado x contado
    [HttpGet("contagens/{id:guid}/pdf")]
    public async Task<IActionResult> ContagemPdf(Guid id)
    {
        var lojaId = await GetLojaId();
        var contagem = await db.ContagensPatrimonio
            .Include(c => c.Itens).ThenInclude(i => i.ItemPatrimonio)
            .FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

        if (contagem is null) return NotFound();

        var loja = await db.Lojas.FindAsync(lojaId!.Value);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text($"Contagem de Patrimônio — {loja?.Nome ?? ""}").FontSize(16).Bold();
                    col.Item().Text($"Data: {contagem.DataContagem:dd/MM/yyyy HH:mm}" +
                        (string.IsNullOrEmpty(contagem.Responsavel) ? "" : $" · Responsável: {contagem.Responsavel}"))
                        .FontSize(10);
                });

                page.Content().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); // nome
                        c.RelativeColumn(1.2f); // esperado
                        c.RelativeColumn(1.2f); // contado
                        c.RelativeColumn(1.2f); // diferença
                        c.RelativeColumn(2.5f); // observação
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Item").Bold();
                        h.Cell().Text("Esperado").Bold();
                        h.Cell().Text("Contado").Bold();
                        h.Cell().Text("Diferença").Bold();
                        h.Cell().Text("Observação").Bold();
                    });

                    foreach (var i in contagem.Itens.OrderBy(i => i.ItemPatrimonio!.Nome))
                    {
                        var diferenca = i.QuantidadeContada - i.QuantidadeEsperadaNoMomento;
                        table.Cell().Text(i.ItemPatrimonio!.Nome);
                        table.Cell().Text(i.QuantidadeEsperadaNoMomento.ToString());
                        table.Cell().Text(i.QuantidadeContada.ToString());
                        table.Cell().Text(diferenca == 0 ? "OK" : (diferenca > 0 ? $"+{diferenca}" : diferenca.ToString()))
                            .FontColor(diferenca == 0 ? Colors.Black : Colors.Red.Medium);
                        table.Cell().Text(i.Observacao ?? "—");
                    }
                });

                var totalDivergencias = contagem.Itens.Count(i => i.QuantidadeContada != i.QuantidadeEsperadaNoMomento);
                page.Footer().AlignRight().Text($"{totalDivergencias} item(ns) com divergência de {contagem.Itens.Count} contado(s).").Bold();
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"contagem-patrimonio-{contagem.DataContagem:yyyy-MM-dd}.pdf");
    }
}