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

        var assinaturas = await db.AssinaturasCliente
            .Where(a => a.LojaId == lojaId && a.Status == "ativa")
            .ToListAsync();

        var clienteIds = assinaturas.Select(a => a.ClienteId).ToList();
        var planoIds = assinaturas.Select(a => a.PlanoId).ToList();
        var assinaturaIds = assinaturas.Select(a => a.Id).ToList();

        var clientes = await db.Clientes
            .Where(c => clienteIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Nome, c.Telefone })
            .ToListAsync();

        var planos = await db.Planos
            .Where(p => planoIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nome, p.Valor })
            .ToListAsync();

        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Todos os pagamentos dessas assinaturas (não só o mês atual)
        var todosPagamentos = await db.PagamentosPlano
            .Where(pg => assinaturaIds.Contains(pg.AssinaturaId))
            .ToListAsync();

        var resultado = assinaturas.Select(a =>
        {
            var cli = clientes.FirstOrDefault(c => c.Id == a.ClienteId);
            var pl = planos.FirstOrDefault(p => p.Id == a.PlanoId);
            var doAssinante = todosPagamentos.Where(pg => pg.AssinaturaId == a.Id).ToList();
            var pgMes = doAssinante.FirstOrDefault(pg => pg.MesReferencia == mesAtual);
            var pendentes = doAssinante.Where(pg =>
                pg.Status == "pendente" &&
                (pg.MesReferencia < mesAtual || (pg.MesReferencia == mesAtual && agora.Day > a.DiaVencimento))
            ).ToList();

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
                mesesEmAtraso = pendentes.Count,
                valorTotalAtraso = pendentes.Sum(p => p.Valor),
                mesInicioCobranca = a.MesInicioCobranca,
                aindaNaoIniciou = a.MesInicioCobranca > mesAtual,
            };
        })
        .OrderByDescending(x => x.mesesEmAtraso)
        .ThenBy(x => x.clienteNome)
        .ToList();

        return Ok(resultado);
    }

    // ── Quitar todos os meses pendentes de uma assinatura ──────────
    [HttpPost("assinantes/{id:guid}/quitar-atraso")]
    public async Task<IActionResult> QuitarAtraso(Guid id)
    {
        var lojaId = await GetLojaId();
        var assinatura = await db.AssinaturasCliente.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (assinatura is null) return NotFound();

        var plano = await db.Planos.FindAsync(assinatura.PlanoId);
        var valor = plano?.Valor ?? 0;
        var nomePlano = plano?.Nome ?? "Plano";
        var agora = DateTime.UtcNow;

        var pendentes = await db.PagamentosPlano
            .Where(p => p.AssinaturaId == id && p.Status == "pendente")
            .ToListAsync();

        if (pendentes.Count == 0)
            return BadRequest(new { erro = "Não há meses pendentes para essa assinatura." });

        foreach (var pg in pendentes)
        {
            pg.Status = "pago";
            pg.PagoEm = agora;
            await CriarVendaMensalidadeAsync(pg, lojaId.Value, assinatura.ClienteId, nomePlano, valor);
        }

        await db.SaveChangesAsync();
        return Ok(new { quitados = pendentes.Count, valorTotal = pendentes.Sum(p => p.Valor) });
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

        var agoraVinculo = DateTime.UtcNow;
        var mesAtualVinculo = new DateTime(agoraVinculo.Year, agoraVinculo.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var mesInicio = req.IniciarProximoMes ? mesAtualVinculo.AddMonths(1) : mesAtualVinculo;

        var assinatura = new AssinaturaCliente
        {
            LojaId = lojaId.Value,
            ClienteId = req.ClienteId,
            PlanoId = req.PlanoId,
            DiaVencimento = req.DiaVencimento is >= 1 and <= 28 ? req.DiaVencimento : 10,
            DataInicio = agoraVinculo,
            MesInicioCobranca = mesInicio,
            Status = "ativa",
        };
        db.AssinaturasCliente.Add(assinatura);
        await db.SaveChangesAsync();

        return Ok(new { assinatura.Id });
    }

    // ── Atualizar assinatura (trocar plano ou dia de vencimento) ──
    [HttpPut("assinantes/{id:guid}")]
    public async Task<IActionResult> AtualizarAssinatura(Guid id, [FromBody] AtualizarAssinaturaRequest req)
    {
        var lojaId = await GetLojaId();
        var assinatura = await db.AssinaturasCliente
            .FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId && a.Status == "ativa");
        if (assinatura is null) return NotFound();

        if (req.PlanoId.HasValue)
        {
            var novoPlano = await db.Planos.FirstOrDefaultAsync(p => p.Id == req.PlanoId.Value && p.LojaId == lojaId);
            if (novoPlano is null) return BadRequest(new { erro = "Plano não encontrado." });
            assinatura.PlanoId = req.PlanoId.Value;
        }

        if (req.DiaVencimento is >= 1 and <= 28)
            assinatura.DiaVencimento = req.DiaVencimento.Value;

        await db.SaveChangesAsync();
        return Ok(new { assinatura.Id, assinatura.PlanoId, assinatura.DiaVencimento });
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

        if (req.Pago)
            await CriarVendaMensalidadeAsync(pg, lojaId.Value, assinatura.ClienteId, nomePlano, valor);
        else
            await RemoverVendaAsync(pg);

        await db.SaveChangesAsync();
        return Ok(new { pg.Id, pg.Status });
    }

    // ── Info do plano de um cliente (usado no Caixa) ───────────────
    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<IActionResult> AssinaturaDoCliente(Guid clienteId)
    {
        var lojaId = await GetLojaId();
        var assinatura = await db.AssinaturasCliente
            .FirstOrDefaultAsync(a => a.ClienteId == clienteId && a.LojaId == lojaId && a.Status == "ativa");

        if (assinatura is null) return Ok(new { temPlano = false });

        var plano = await db.Planos.FindAsync(assinatura.PlanoId);
        if (plano is null) return Ok(new { temPlano = false });

        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var temAtraso = await db.PagamentosPlano.AnyAsync(p =>
            p.AssinaturaId == assinatura.Id &&
            p.Status == "pendente" &&
            (p.MesReferencia < mesAtual || (p.MesReferencia == mesAtual && agora.Day > assinatura.DiaVencimento)));

        var servicosIncluidos = (plano.ServicosIds ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        return Ok(new
        {
            temPlano = true,
            planoId = plano.Id,
            planoNome = plano.Nome,
            servicosIncluidos,
            emDia = !temAtraso,
        });
    }

    // ── Histórico completo de pagamentos de uma assinatura ─────────
    [HttpGet("assinantes/{id:guid}/historico")]
    public async Task<IActionResult> HistoricoPagamentos(Guid id)
    {
        var lojaId = await GetLojaId();
        var assinatura = await db.AssinaturasCliente.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (assinatura is null) return NotFound();

        var historico = await db.PagamentosPlano
            .Where(p => p.AssinaturaId == id)
            .OrderByDescending(p => p.MesReferencia)
            .Select(p => new
            {
                p.Id,
                p.MesReferencia,
                p.Valor,
                p.Status,
                p.PagoEm,
            })
            .ToListAsync();

        return Ok(historico);
    }

    // ── Helpers de venda (fluxo de caixa) ──────────────────────────
    private async Task CriarVendaMensalidadeAsync(PagamentoPlano pg, Guid lojaId, Guid clienteId, string nomePlano, decimal valor)
    {
        if (pg.VendaId is not null) return;

        var venda = new Venda
        {
            LojaId = lojaId,
            ClienteId = clienteId,
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

    private async Task RemoverVendaAsync(PagamentoPlano pg)
    {
        if (pg.VendaId is null) return;

        var vendaAntiga = await db.Vendas
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == pg.VendaId.Value);
        if (vendaAntiga is not null)
        {
            db.ItensVenda.RemoveRange(vendaAntiga.Itens);
            db.Vendas.Remove(vendaAntiga);
        }
        pg.VendaId = null;
    }

    public record AtualizarAssinaturaRequest(Guid? PlanoId, int? DiaVencimento);

    public record VincularPlanoRequest(
        Guid ClienteId,
        Guid PlanoId,
        int DiaVencimento,
        bool IniciarProximoMes = false
    );

    public record MarcarPagamentoPlanoRequest(bool Pago);


    public record SalvarPlanoRequest(
        string Nome,
        decimal Valor,
        string? ServicosIds
    );
}