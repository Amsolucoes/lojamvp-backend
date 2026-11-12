using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;
using LojaApi.Modules.Carteira.Application;
using LojaApi.Modules.Carteira.Domain;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController(AppDbContext db, CarteiraService carteira) : ControllerBase
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
        var q = db.Clientes.AsQueryable();

        if (lojaId.HasValue)
            q = q.Where(c => c.LojaId == lojaId);

        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(c =>
                c.Nome.ToLower().Contains(busca.ToLower()) ||
                c.Telefone.Contains(busca) ||
                (c.Cpf != null && c.Cpf.Contains(busca)));

        var lista = await q.OrderBy(c => c.Nome)
            .Select(c => new ClienteDto(
                c.Id, c.Nome, c.Telefone, c.Cpf, c.Email,
                c.Endereco, c.Observacoes, c.CriadoEm,
                c.Vendas.Count, c.Vendas.Sum(v => v.TotalFinal),
                c.DataNascimento, c.CreditoLoja,
                db.MovimentosCarteira.Where(m => m.ClienteId == c.Id)
                    .Sum(m => m.Tipo == TipoMovimentoCarteira.Compra ? m.Valor : -m.Valor),
                c.LimiteCarteira
            )).ToListAsync();

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id)
    {
        var lojaId = await GetLojaId();
        var c = await db.Clientes
            .Include(c => c.Vendas).ThenInclude(v => v.Itens)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (c is null || (lojaId.HasValue && c.LojaId != lojaId)) return NotFound();

        return Ok(new ClienteDto(
            c.Id, c.Nome, c.Telefone, c.Cpf, c.Email,
            c.Endereco, c.Observacoes, c.CriadoEm,
            c.Vendas.Count, c.Vendas.Sum(v => v.TotalFinal),
            c.DataNascimento, c.CreditoLoja,
            await carteira.SaldoPorClienteAsync(c.Id), c.LimiteCarteira
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarClienteRequest req)
    {
        var lojaId = await GetLojaId();

        if (!string.IsNullOrWhiteSpace(req.Cpf))
        {
            var cpfExiste = await db.Clientes.AnyAsync(c =>
                c.Cpf == req.Cpf && (!lojaId.HasValue || c.LojaId == lojaId));
            if (cpfExiste) return Conflict(new { erro = "CPF já cadastrado." });
        }

        var cliente = new Cliente
        {
            Nome = req.Nome,
            Telefone = req.Telefone,
            Cpf = req.Cpf,
            Email = req.Email,
            Endereco = req.Endereco,
            Observacoes = req.Observacoes,
            DataNascimento = req.DataNascimento,
            LojaId = lojaId,
        };

        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Buscar), new { id = cliente.Id },
            new ClienteDto(cliente.Id, cliente.Nome, cliente.Telefone, cliente.Cpf,
                cliente.Email, cliente.Endereco, cliente.Observacoes, cliente.CriadoEm, 0, 0, cliente.DataNascimento,
                cliente.CreditoLoja, 0, cliente.LimiteCarteira));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarClienteRequest req)
    {
        var lojaId = await GetLojaId();
        var cliente = await db.Clientes.FindAsync(id);
        if (cliente is null || (lojaId.HasValue && cliente.LojaId != lojaId)) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Cpf) && req.Cpf != cliente.Cpf)
        {
            var cpfExiste = await db.Clientes.AnyAsync(c =>
                c.Cpf == req.Cpf && c.Id != id &&
                (!lojaId.HasValue || c.LojaId == lojaId));
            if (cpfExiste) return Conflict(new { erro = "CPF já cadastrado." });
        }

        cliente.Nome = req.Nome; cliente.Telefone = req.Telefone;
        cliente.Cpf = req.Cpf; cliente.Email = req.Email;
        cliente.Endereco = req.Endereco; cliente.Observacoes = req.Observacoes;
        cliente.DataNascimento = req.DataNascimento;

        await db.SaveChangesAsync();

        var compras = await db.Vendas.Where(v => v.ClienteId == id).ToListAsync();
        return Ok(new ClienteDto(cliente.Id, cliente.Nome, cliente.Telefone, cliente.Cpf,
            cliente.Email, cliente.Endereco, cliente.Observacoes, cliente.CriadoEm,
            compras.Count, compras.Sum(v => v.TotalFinal), cliente.DataNascimento,
            cliente.CreditoLoja, await carteira.SaldoPorClienteAsync(cliente.Id), cliente.LimiteCarteira));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var lojaId = await GetLojaId();
        var cliente = await db.Clientes.FindAsync(id);
        if (cliente is null || (lojaId.HasValue && cliente.LojaId != lojaId)) return NotFound();

        if (await carteira.SaldoPorClienteAsync(cliente.Id) > 0)
            return BadRequest(new { erro = "Este cliente tem saldo a pagar na carteira. Receba o pagamento antes de excluir o cadastro." });

        db.Clientes.Remove(cliente);
        await db.SaveChangesAsync();
        return NoContent();
    }
}