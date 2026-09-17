using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/fornecedores")]
[Authorize]
public class FornecedoresController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca)
    {
        var lojaId = await GetLojaId();
        var q = db.Fornecedores.Include(f => f.Produtos).AsQueryable();

        if (lojaId.HasValue)
            q = q.Where(f => f.LojaId == lojaId);

        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(f =>
                f.Nome.ToLower().Contains(busca.ToLower()) ||
                (f.CnpjCpf != null && f.CnpjCpf.Contains(busca)) ||
                (f.Telefone != null && f.Telefone.Contains(busca)));

        var lista = await q.OrderBy(f => f.Nome)
            .Select(f => new FornecedorDto(
                f.Id, f.Nome, f.CnpjCpf, f.Telefone, f.Email,
                f.Endereco, f.Observacoes, f.Ativo, f.CriadoEm,
                f.Produtos.Count(p => p.Ativo)
            )).ToListAsync();

        return Ok(lista);
    }

    // ── Lista simplificada (id + nome), usada no seletor do cadastro de produto ──
    [HttpGet("ativos")]
    public async Task<IActionResult> ListarAtivos()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.Fornecedores
            .Where(f => f.LojaId == lojaId && f.Ativo)
            .OrderBy(f => f.Nome)
            .Select(f => new { f.Id, f.Nome })
            .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id)
    {
        var lojaId = await GetLojaId();
        var f = await db.Fornecedores.Include(x => x.Produtos).FirstOrDefaultAsync(x => x.Id == id);
        if (f is null || (lojaId.HasValue && f.LojaId != lojaId)) return NotFound();

        return Ok(new FornecedorDto(
            f.Id, f.Nome, f.CnpjCpf, f.Telefone, f.Email,
            f.Endereco, f.Observacoes, f.Ativo, f.CriadoEm,
            f.Produtos.Count(p => p.Ativo)
        ));
    }

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarFornecedorRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Nome é obrigatório." });

        var fornecedor = new Fornecedor
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            CnpjCpf = req.CnpjCpf,
            Telefone = req.Telefone,
            Email = req.Email,
            Endereco = req.Endereco,
            Observacoes = req.Observacoes,
            Ativo = req.Ativo,
        };
        db.Fornecedores.Add(fornecedor);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Buscar), new { id = fornecedor.Id },
            new FornecedorDto(fornecedor.Id, fornecedor.Nome, fornecedor.CnpjCpf, fornecedor.Telefone,
                fornecedor.Email, fornecedor.Endereco, fornecedor.Observacoes, fornecedor.Ativo,
                fornecedor.CriadoEm, 0));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarFornecedorRequest req)
    {
        var lojaId = await GetLojaId();
        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id && (!lojaId.HasValue || f.LojaId == lojaId));
        if (fornecedor is null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Nome é obrigatório." });

        fornecedor.Nome = req.Nome.Trim();
        fornecedor.CnpjCpf = req.CnpjCpf;
        fornecedor.Telefone = req.Telefone;
        fornecedor.Email = req.Email;
        fornecedor.Endereco = req.Endereco;
        fornecedor.Observacoes = req.Observacoes;
        fornecedor.Ativo = req.Ativo;

        await db.SaveChangesAsync();

        var qtdProdutos = await db.Produtos.CountAsync(p => p.FornecedorId == id && p.Ativo);
        return Ok(new FornecedorDto(fornecedor.Id, fornecedor.Nome, fornecedor.CnpjCpf, fornecedor.Telefone,
            fornecedor.Email, fornecedor.Endereco, fornecedor.Observacoes, fornecedor.Ativo,
            fornecedor.CriadoEm, qtdProdutos));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id && (!lojaId.HasValue || f.LojaId == lojaId));
        if (fornecedor is null) return NotFound();

        var qtdProdutos = await db.Produtos.CountAsync(p => p.FornecedorId == id);
        if (qtdProdutos > 0)
        {
            fornecedor.Ativo = false; // em uso — desativa em vez de excluir
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Fornecedor em uso — foi desativado em vez de excluído." });
        }

        db.Fornecedores.Remove(fornecedor);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Fornecedor excluído." });
    }
}
