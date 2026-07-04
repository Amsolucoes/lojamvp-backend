using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/planos")]
[Authorize]
public class PlanosController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ── Listar planos da loja ─────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var planos = await db.Planos
            .Where(p => p.LojaId == lojaId)
            .OrderByDescending(p => p.Ativo).ThenBy(p => p.Nome)
            .Select(p => new { p.Id, p.Nome, p.Valor, p.ServicosIds, p.Ativo })
            .ToListAsync();

        return Ok(planos);
    }

    // ── Criar plano ───────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarPlanoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome) || req.Valor <= 0)
            return BadRequest(new { erro = "Informe nome e valor válidos." });

        var plano = new Plano
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Valor = req.Valor,
            ServicosIds = req.ServicosIds,
            Ativo = true,
        };
        db.Planos.Add(plano);
        await db.SaveChangesAsync();

        return Ok(new { plano.Id, plano.Nome, plano.Valor, plano.ServicosIds, plano.Ativo });
    }

    // ── Atualizar plano ───────────────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarPlanoRequest req)
    {
        var lojaId = await GetLojaId();
        var plano = await db.Planos.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (plano is null) return NotFound();

        plano.Nome = req.Nome.Trim();
        plano.Valor = req.Valor;
        plano.ServicosIds = req.ServicosIds;
        await db.SaveChangesAsync();

        return Ok(new { plano.Id, plano.Nome, plano.Valor, plano.ServicosIds, plano.Ativo });
    }

    // ── Ativar/desativar plano ────────────────────────────────────
    [HttpPatch("{id:guid}/ativo")]
    public async Task<IActionResult> AlternarAtivo(Guid id)
    {
        var lojaId = await GetLojaId();
        var plano = await db.Planos.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (plano is null) return NotFound();

        plano.Ativo = !plano.Ativo;
        await db.SaveChangesAsync();
        return Ok(new { plano.Id, plano.Ativo });
    }

    // ── Excluir plano ─────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var plano = await db.Planos.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (plano is null) return NotFound();

        // Não deixa excluir se tem clientes vinculados (ativos)
        var temAssinantes = await db.AssinaturasCliente
            .AnyAsync(a => a.PlanoId == id && a.Status == "ativa");
        if (temAssinantes)
            return BadRequest(new { erro = "Este plano tem clientes ativos. Desative-o em vez de excluir." });

        db.Planos.Remove(plano);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Plano excluído." });
    }

    // ══════════════════════════════════════════════════════════════
    //  ASSINATURAS (cliente vinculado a um plano)
    // ══════════════════════════════════════════════════════════════

    // ── Listar assinantes (clientes com plano) ────────────────────
    [HttpGet("assinantes")]
    public async Task<IActionResult> Assinantes()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        // Junta assinatura + dados do cliente + nome do plano
        var assinaturas = await db.AssinaturasCliente
            .Where(a => a.LojaId == lojaId && a.Status == "ativa")
            .ToListAsync();

        var clienteIds = assinaturas.Select(a => a.ClienteId).ToList();
        var planoIds = assinaturas.Select(a => a.PlanoId).ToList();

        var clientes = await db.Clientes
            .Where(c => clienteIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Nome, c.Telefone })
            .ToListAsync();

        var planos = await db.Planos
            .Where(p => planoIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nome, p.Valor })
            .ToListAsync();

        // Mês de referência atual (primeiro dia do mês, UTC)
        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var pagamentosMes = await db.PagamentosPlano
            .Where(pg => pg.LojaId == lojaId && pg.MesReferencia == mesAtual)
            .ToListAsync();

        var resultado = assinaturas.Select(a =>
        {
            var cli = clientes.FirstOrDefault(c => c.Id == a.ClienteId);
            var pl = planos.FirstOrDefault(p => p.Id == a.PlanoId);
            var pgMes = pagamentosMes.FirstOrDefault(pg => pg.AssinaturaId == a.Id);
            return new
            {
                assinaturaId = a.Id,
                clienteId = a.ClienteId,
                clienteNome = cli?.Nome ?? "(cliente removido)",
                clienteTelefone = cli?.Telefone,
                planoId = a.PlanoId,
                planoNome = pl?.Nome ?? "(plano removido)",
                valor = pl?.Valor ?? 0,
                diaVencimento = a.DiaVencimento,
                pagoNoMes = pgMes != null && pgMes.Status == "pago",
            };
        }).OrderBy(x => x.clienteNome).ToList();

        return Ok(resultado);
    }

    // ── Vincular cliente a um plano ───────────────────────────────
    [HttpPost("assinantes")]
    public async Task<IActionResult> VincularCliente([FromBody] VincularPlanoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var plano = await db.Planos.FirstOrDefaultAsync(p => p.Id == req.PlanoId && p.LojaId == lojaId);
        if (plano is null) return BadRequest(new { erro = "Plano não encontrado." });

        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == req.ClienteId && c.LojaId == lojaId);
        if (cliente is null) return BadRequest(new { erro = "Cliente não encontrado." });

        // Se já tem assinatura ativa, não duplica (1 plano por cliente)
        var jaTem = await db.AssinaturasCliente
            .AnyAsync(a => a.ClienteId == req.ClienteId && a.Status == "ativa");
        if (jaTem)
            return Conflict(new { erro = "Este cliente já tem um plano ativo." });

        var assinatura = new AssinaturaCliente
        {
            LojaId = lojaId.Value,
            ClienteId = req.ClienteId,
            PlanoId = req.PlanoId,
            DiaVencimento = req.DiaVencimento is >= 1 and <= 28 ? req.DiaVencimento : 10,
            DataInicio = DateTime.UtcNow,
            Status = "ativa",
        };
        db.AssinaturasCliente.Add(assinatura);
        await db.SaveChangesAsync();

        return Ok(new { assinatura.Id });
    }

    // ── Cancelar assinatura ───────────────────────────────────────
    [HttpPatch("assinantes/{id:guid}/cancelar")]
    public async Task<IActionResult> CancelarAssinatura(Guid id)
    {
        var lojaId = await GetLojaId();
        var a = await db.AssinaturasCliente.FirstOrDefaultAsync(x => x.Id == id && x.LojaId == lojaId);
        if (a is null) return NotFound();

        a.Status = "cancelada";
        await db.SaveChangesAsync();
        return Ok(new { a.Id, a.Status });
    }

    // ── Marcar mensalidade do mês como paga/pendente (manual) ─────
    [HttpPost("assinantes/{id:guid}/pagamento")]
    public async Task<IActionResult> MarcarPagamento(Guid id, [FromBody] MarcarPagamentoPlanoRequest req)
    {
        var lojaId = await GetLojaId();
        var assinatura = await db.AssinaturasCliente.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (assinatura is null) return NotFound();

        var plano = await db.Planos.FindAsync(assinatura.PlanoId);
        var valor = plano?.Valor ?? 0;
        var nomePlano = plano?.Nome ?? "Plano";

        // Mês de referência (primeiro dia do mês atual, UTC)
        var agora = DateTime.UtcNow;
        var mesRef = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var pg = await db.PagamentosPlano
            .FirstOrDefaultAsync(p => p.AssinaturaId == id && p.MesReferencia == mesRef);

        if (pg is null)
        {
            pg = new PagamentoPlano
            {
                AssinaturaId = id,
                LojaId = lojaId.Value,
                MesReferencia = mesRef,
                Valor = valor,
                Status = req.Pago ? "pago" : "pendente",
                PagoEm = req.Pago ? agora : null,
            };
            db.PagamentosPlano.Add(pg);
        }
        else
        {
            pg.Status = req.Pago ? "pago" : "pendente";
            pg.PagoEm = req.Pago ? agora : null;
        }

        // ── Ao marcar PAGO: cria uma venda para entrar no fluxo de caixa ──
        if (req.Pago && string.IsNullOrEmpty(pg.VendaId?.ToString()))
        {
            var venda = new Venda
            {
                LojaId = lojaId,
                ClienteId = assinatura.ClienteId,
                Total = valor,
                Desconto = 0,
                TotalFinal = valor,
                FormaPagamento = "pix",
                FormasPagamento = null,
                Troco = null,
            };
            db.Vendas.Add(venda);

            db.ItensVenda.Add(new ItemVenda
            {
                VendaId = venda.Id,
                ProdutoId = null,
                ServicoId = null,
                NomeProduto = $"Mensalidade - {nomePlano}",
                Quantidade = 1,
                PrecoUnitario = valor,
                Subtotal = valor,
            });

            pg.VendaId = venda.Id;
        }

        await db.SaveChangesAsync();
        return Ok(new { pg.Id, pg.Status });
    }

    public record VincularPlanoRequest(
    Guid ClienteId,
    Guid PlanoId,
    int DiaVencimento
);

public record MarcarPagamentoPlanoRequest(bool Pago);


public record SalvarPlanoRequest(
    string Nome,
    decimal Valor,
    string? ServicosIds
);