using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca)
    {
        var q = db.Clientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(c =>
                c.Nome.ToLower().Contains(busca.ToLower()) ||
                c.Telefone.Contains(busca) ||
                (c.Cpf != null && c.Cpf.Contains(busca)));

        var lista = await q
            .OrderBy(c => c.Nome)
            .Select(c => new ClienteDto(
                c.Id, c.Nome, c.Telefone, c.Cpf, c.Email,
                c.Endereco, c.Observacoes, c.CriadoEm,
                c.Vendas.Count,
                c.Vendas.Sum(v => v.TotalFinal)
            ))
            .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id)
    {
        var c = await db.Clientes
            .Include(c => c.Vendas).ThenInclude(v => v.Itens)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (c is null) return NotFound();

        return Ok(new ClienteDto(
            c.Id, c.Nome, c.Telefone, c.Cpf, c.Email,
            c.Endereco, c.Observacoes, c.CriadoEm,
            c.Vendas.Count, c.Vendas.Sum(v => v.TotalFinal)
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarClienteRequest req)
    {
        // CPF único
        if (!string.IsNullOrWhiteSpace(req.Cpf))
        {
            var cpfExiste = await db.Clientes.AnyAsync(c => c.Cpf == req.Cpf);
            if (cpfExiste) return Conflict(new { erro = "CPF já cadastrado." });
        }

        var cliente = new Cliente
        {
            Nome        = req.Nome,
            Telefone    = req.Telefone,
            Cpf         = req.Cpf,
            Email       = req.Email,
            Endereco    = req.Endereco,
            Observacoes = req.Observacoes,
        };

        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Buscar), new { id = cliente.Id },
            new ClienteDto(cliente.Id, cliente.Nome, cliente.Telefone, cliente.Cpf,
                cliente.Email, cliente.Endereco, cliente.Observacoes, cliente.CriadoEm, 0, 0));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarClienteRequest req)
    {
        var cliente = await db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Cpf) && req.Cpf != cliente.Cpf)
        {
            var cpfExiste = await db.Clientes.AnyAsync(c => c.Cpf == req.Cpf && c.Id != id);
            if (cpfExiste) return Conflict(new { erro = "CPF já cadastrado." });
        }

        cliente.Nome        = req.Nome;
        cliente.Telefone    = req.Telefone;
        cliente.Cpf         = req.Cpf;
        cliente.Email       = req.Email;
        cliente.Endereco    = req.Endereco;
        cliente.Observacoes = req.Observacoes;

        await db.SaveChangesAsync();

        var compras = await db.Vendas.Where(v => v.ClienteId == id).ToListAsync();
        return Ok(new ClienteDto(cliente.Id, cliente.Nome, cliente.Telefone, cliente.Cpf,
            cliente.Email, cliente.Endereco, cliente.Observacoes, cliente.CriadoEm,
            compras.Count, compras.Sum(v => v.TotalFinal)));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var cliente = await db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();
        db.Clientes.Remove(cliente);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
