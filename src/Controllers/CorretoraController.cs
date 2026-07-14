using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/corretora")]
[Authorize]
public class CorretoraController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ══════════════════ SEGURADORAS ══════════════════

    [HttpGet("seguradoras")]
    public async Task<IActionResult> ListarSeguradoras([FromQuery] bool todas = false)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.Seguradoras.Where(s => s.LojaId == lojaId);
        if (!todas) q = q.Where(s => s.Ativa);

        var lista = await q.OrderBy(s => s.Nome)
            .Select(s => new { s.Id, s.Nome, s.Ativa })
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarSeguradoraRequest(string Nome);

    [HttpPost("seguradoras")]
    public async Task<IActionResult> CriarSeguradora([FromBody] SalvarSeguradoraRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var seg = new Seguradora { LojaId = lojaId.Value, Nome = req.Nome.Trim() };
        db.Seguradoras.Add(seg);
        await db.SaveChangesAsync();

        return Ok(new { seg.Id, seg.Nome });
    }

    [HttpPatch("seguradoras/{id:guid}/ativo")]
    public async Task<IActionResult> AlternarSeguradora(Guid id)
    {
        var lojaId = await GetLojaId();
        var seg = await db.Seguradoras.FirstOrDefaultAsync(s => s.Id == id && s.LojaId == lojaId);
        if (seg is null) return NotFound();

        seg.Ativa = !seg.Ativa;
        await db.SaveChangesAsync();
        return Ok(new { seg.Id, seg.Ativa });
    }

    // ══════════════════ FUNIL DE VENDAS ══════════════════

    [HttpGet("oportunidades")]
    public async Task<IActionResult> ListarOportunidades()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.Oportunidades
            .Where(o => o.LojaId == lojaId && o.Etapa != "perdido")
            .Include(o => o.Seguradora)
            .Join(db.Clientes, o => o.ClienteId, c => c.Id, (o, c) => new
            {
                o.Id,
                o.ClienteId,
                clienteNome = c.Nome,
                clienteTelefone = c.Telefone,
                seguradoraNome = o.Seguradora != null ? o.Seguradora.Nome : null,
                o.PlanoDesejado,
                o.ValorEstimado,
                o.Etapa,
                o.Ordem,
                o.Observacao,
                o.QuantidadeVidas,
                o.CriadoEm,
            })
            .OrderBy(x => x.Ordem)
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarOportunidadeRequest(Guid ClienteId, Guid? SeguradoraId, string? PlanoDesejado, decimal? ValorEstimado, string? Observacao, int? QuantidadeVidas);

    [HttpPost("oportunidades")]
    public async Task<IActionResult> CriarOportunidade([FromBody] SalvarOportunidadeRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var maxOrdem = await db.Oportunidades
            .Where(o => o.LojaId == lojaId && o.Etapa == "lead")
            .Select(o => (int?)o.Ordem)
            .MaxAsync() ?? 0;

        var op = new Oportunidade
        {
            LojaId = lojaId.Value,
            ClienteId = req.ClienteId,
            SeguradoraId = req.SeguradoraId,
            PlanoDesejado = req.PlanoDesejado,
            ValorEstimado = req.ValorEstimado,
            QuantidadeVidas = req.QuantidadeVidas,
            Observacao = req.Observacao,
            Ordem = maxOrdem + 1,
        };
        db.Oportunidades.Add(op);
        await db.SaveChangesAsync();

        return Ok(new { op.Id });
    }

    [HttpPut("oportunidades/{id:guid}")]
    public async Task<IActionResult> AtualizarOportunidade(Guid id, [FromBody] SalvarOportunidadeRequest req)
    {
        var lojaId = await GetLojaId();
        var op = await db.Oportunidades.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (op is null) return NotFound();

        op.ClienteId = req.ClienteId;
        op.SeguradoraId = req.SeguradoraId;
        op.PlanoDesejado = req.PlanoDesejado;
        op.ValorEstimado = req.ValorEstimado;
        op.QuantidadeVidas = req.QuantidadeVidas;
        op.Observacao = req.Observacao;
        op.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { op.Id });
    }

    // ── Mover card entre etapas (drag and drop) ─────────────────────
    public record MoverEtapaRequest(string Etapa, int Ordem, string? MotivoPerda, decimal? Valor, Guid? ContaBancariaId);

    [HttpPatch("oportunidades/{id:guid}/etapa")]
    public async Task<IActionResult> MoverEtapa(Guid id, [FromBody] MoverEtapaRequest req)
    {
        var lojaId = await GetLojaId();
        var op = await db.Oportunidades.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (op is null) return NotFound();

        var etapasValidas = new[] { "lead", "contato", "proposta", "negociacao", "ganho", "perdido" };
        if (!etapasValidas.Contains(req.Etapa))
            return BadRequest(new { erro = "Etapa inválida." });

        op.Etapa = req.Etapa;
        op.Ordem = req.Ordem;
        op.AtualizadoEm = DateTime.UtcNow;
        if (req.Etapa == "perdido") op.MotivoPerda = req.MotivoPerda;

        // Ao chegar em "ganho", gera automaticamente o lançamento de comissão no Financeiro
        // Ao chegar em "ganho", gera automaticamente o lançamento de comissão no Financeiro
        var comissaoLancada = false;
        if (req.Etapa == "ganho" && op.LancamentoFinanceiroId is null && req.ContaBancariaId.HasValue)
        {
            var cliente = await db.Clientes.FindAsync(op.ClienteId);
            var lancamento = new LancamentoFinanceiro
            {
                LojaId = lojaId!.Value,
                ContaBancariaId = req.ContaBancariaId.Value,
                Tipo = "receber",
                Modo = "avulsa",
                Descricao = $"Comissão — {op.PlanoDesejado ?? "Plano"} ({cliente?.Nome})",
                Valor = req.Valor ?? op.ValorEstimado ?? 0,
                Vencimento = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc).AddHours(12),
                Avisar = true,
            };
            db.LancamentosFinanceiros.Add(lancamento);
            await db.SaveChangesAsync();
            op.LancamentoFinanceiroId = lancamento.Id;
            comissaoLancada = true;
        }
        else if (op.LancamentoFinanceiroId.HasValue)
        {
            comissaoLancada = true; // já tinha sido lançada antes
        }

        await db.SaveChangesAsync();
        return Ok(new { op.Id, op.Etapa, comissaoLancada });
    }

    [HttpDelete("oportunidades/{id:guid}")]
    public async Task<IActionResult> ExcluirOportunidade(Guid id)
    {
        var lojaId = await GetLojaId();
        var op = await db.Oportunidades.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (op is null) return NotFound();

        db.Oportunidades.Remove(op);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Removido." });
    }

    // ══════════════════ APÓLICES (VIGÊNCIA) ══════════════════

    [HttpGet("apolices")]
    public async Task<IActionResult> ListarApolices()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var hoje = DateTime.UtcNow.Date;

        var lista = await db.Apolices
            .Where(a => a.LojaId == lojaId)
            .Include(a => a.Seguradora)
            .Join(db.Clientes, a => a.ClienteId, c => c.Id, (a, c) => new
            {
                a.Id,
                a.ClienteId,
                clienteNome = c.Nome,
                clienteTelefone = c.Telefone,
                seguradoraNome = a.Seguradora!.Nome,
                a.NomePlano,
                a.NumeroApolice,
                a.ValorPremio,
                a.ValorComissao,
                a.PercentualComissao,
                a.VigenciaInicio,
                a.VigenciaFim,
                a.Status,
                diasParaVencer = (a.VigenciaFim.Date - hoje).Days,
            })
            .OrderBy(x => x.VigenciaFim)
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarApoliceRequest(
        Guid ClienteId, Guid? OportunidadeId, Guid SeguradoraId, string NomePlano, string? NumeroApolice,
        decimal ValorPremio, decimal ValorComissao, decimal? PercentualComissao,
        DateTime VigenciaInicio, DateTime VigenciaFim, Guid? ContaBancariaId, bool GerarNoFinanceiro
    );

    [HttpPost("apolices")]
    public async Task<IActionResult> CriarApolice([FromBody] SalvarApoliceRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var apolice = new Apolice
        {
            LojaId = lojaId.Value,
            ClienteId = req.ClienteId,
            OportunidadeId = req.OportunidadeId,
            SeguradoraId = req.SeguradoraId,
            NomePlano = req.NomePlano.Trim(),
            NumeroApolice = req.NumeroApolice,
            ValorPremio = req.ValorPremio,
            ValorComissao = req.ValorComissao,
            PercentualComissao = req.PercentualComissao,
            VigenciaInicio = DateTime.SpecifyKind(req.VigenciaInicio.Date, DateTimeKind.Utc).AddHours(12),
            VigenciaFim = DateTime.SpecifyKind(req.VigenciaFim.Date, DateTimeKind.Utc).AddHours(12),
        };
        db.Apolices.Add(apolice);

        // Se veio de uma oportunidade, marca ela como "ganho"
        if (req.OportunidadeId.HasValue)
        {
            var op = await db.Oportunidades.FindAsync(req.OportunidadeId.Value);
            if (op != null) { op.Etapa = "ganho"; op.AtualizadoEm = DateTime.UtcNow; }
        }

        // Gera automaticamente um "a receber" no Financeiro com o valor da comissão
        if (req.GerarNoFinanceiro && req.ContaBancariaId.HasValue)
        {
            var cliente = await db.Clientes.FindAsync(req.ClienteId);
            var lancamento = new LancamentoFinanceiro
            {
                LojaId = lojaId.Value,
                ContaBancariaId = req.ContaBancariaId.Value,
                Tipo = "receber",
                Modo = "avulsa",
                Descricao = $"Comissão — {req.NomePlano} ({cliente?.Nome})",
                Valor = req.ValorComissao,
                Vencimento = apolice.VigenciaInicio,
                Avisar = true,
            };
            db.LancamentosFinanceiros.Add(lancamento);
            await db.SaveChangesAsync();
            apolice.LancamentoFinanceiroId = lancamento.Id;
        }

        await db.SaveChangesAsync();
        return Ok(new { apolice.Id });
    }

    [HttpPut("apolices/{id:guid}")]
    public async Task<IActionResult> AtualizarApolice(Guid id, [FromBody] SalvarApoliceRequest req)
    {
        var lojaId = await GetLojaId();
        var apolice = await db.Apolices.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (apolice is null) return NotFound();

        apolice.SeguradoraId = req.SeguradoraId;
        apolice.NomePlano = req.NomePlano.Trim();
        apolice.NumeroApolice = req.NumeroApolice;
        apolice.ValorPremio = req.ValorPremio;
        apolice.ValorComissao = req.ValorComissao;
        apolice.PercentualComissao = req.PercentualComissao;
        apolice.VigenciaInicio = DateTime.SpecifyKind(req.VigenciaInicio.Date, DateTimeKind.Utc).AddHours(12);
        apolice.VigenciaFim = DateTime.SpecifyKind(req.VigenciaFim.Date, DateTimeKind.Utc).AddHours(12);
        await db.SaveChangesAsync();

        return Ok(new { apolice.Id });
    }

    [HttpPatch("apolices/{id:guid}/status")]
    public async Task<IActionResult> AlterarStatusApolice(Guid id, [FromBody] SalvarSeguradoraRequest req) // reaproveita record simples: req.Nome = status
    {
        var lojaId = await GetLojaId();
        var apolice = await db.Apolices.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (apolice is null) return NotFound();

        var statusValidos = new[] { "ativa", "vencida", "renovada", "cancelada" };
        if (!statusValidos.Contains(req.Nome)) return BadRequest(new { erro = "Status inválido." });

        apolice.Status = req.Nome;
        await db.SaveChangesAsync();
        return Ok(new { apolice.Id, apolice.Status });
    }

    // ── Resumo financeiro: soma de vendas concluídas ────────────────
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });

        var apolicesAtivas = await db.Apolices
            .Where(a => a.LojaId == lojaId && a.Status != "cancelada")
            .ToListAsync();

        var totalPremios = apolicesAtivas.Sum(a => a.ValorPremio);
        var totalComissoes = apolicesAtivas.Sum(a => a.ValorComissao);

        var funil = await db.Oportunidades
            .Where(o => o.LojaId == lojaId)
            .GroupBy(o => o.Etapa)
            .Select(g => new { etapa = g.Key, qtd = g.Count() })
            .ToListAsync();

        var hoje = DateTime.UtcNow.Date;
        var vencendoEm30Dias = apolicesAtivas.Count(a => a.Status == "ativa" && (a.VigenciaFim.Date - hoje).Days <= 30 && (a.VigenciaFim.Date - hoje).Days >= 0);

        return Ok(new
        {
            totalApolices = apolicesAtivas.Count,
            totalPremios,
            totalComissoes,
            vencendoEm30Dias,
            funil,
        });
    }

    [HttpPut("seguradoras/{id:guid}")]
    public async Task<IActionResult> AtualizarSeguradora(Guid id, [FromBody] AtualizarSeguradoraRequest req)
    {
        var lojaId = await GetLojaId();
        var seg = await db.Seguradoras.FirstOrDefaultAsync(s => s.Id == id && s.LojaId == lojaId);
        if (seg is null) return NotFound();

        seg.Nome = req.Nome.Trim();
        await db.SaveChangesAsync();
        return Ok(new { seg.Id, seg.Nome });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int dias = 15)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });

        var desde = DateTime.UtcNow.AddDays(-dias);

        var oportunidades = await db.Oportunidades
            .Where(o => o.LojaId == lojaId && o.CriadoEm >= desde)
            .ToListAsync();

        var emAndamento = oportunidades.Where(o => o.Etapa != "ganho" && o.Etapa != "perdido").ToList();
        var implantadas = oportunidades.Where(o => o.Etapa == "ganho").ToList();

        return Ok(new
        {
            emAndamento = new
            {
                propostas = emAndamento.Count,
                vidas = emAndamento.Sum(o => o.QuantidadeVidas ?? 0),
                valorTotal = emAndamento.Sum(o => o.ValorEstimado ?? 0),
            },
            implantadas = new
            {
                propostas = implantadas.Count,
                vidas = implantadas.Sum(o => o.QuantidadeVidas ?? 0),
                valorTotal = implantadas.Sum(o => o.ValorEstimado ?? 0),
            },
        });
    }

    [HttpGet("oportunidades/verificar-cliente/{clienteId:guid}")]
    public async Task<IActionResult> VerificarClienteJaTemOportunidade(Guid clienteId)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { existe = false });

        var existentes = await db.Oportunidades
            .Where(o => o.LojaId == lojaId && o.ClienteId == clienteId && o.Etapa != "ganho" && o.Etapa != "perdido")
            .Select(o => new { o.Id, o.Etapa, o.PlanoDesejado })
            .ToListAsync();

        return Ok(new { existe = existentes.Count > 0, quantidade = existentes.Count, oportunidades = existentes });
    }

    public record AtualizarSeguradoraRequest(string Nome);
}