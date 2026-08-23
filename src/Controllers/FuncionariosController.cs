using LojaApi.Data;
using LojaApi.Models;
using LojaApi.Services;
using LojaApi.src.Models.Funcionarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/funcionarios")]
[Authorize]
public class FuncionariosController(AppDbContext db, FinanceiroService financeiroService) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    private async Task<Guid> ObterOuCriarCategoriaSalarioAsync(Guid lojaId)
    {
        var categoria = await db.CategoriasFinanceiras
            .FirstOrDefaultAsync(c => c.LojaId == lojaId && c.Nome == "Salários de Funcionários");
        if (categoria != null) return categoria.Id;

        categoria = new CategoriaFinanceira
        {
            LojaId = lojaId,
            Nome = "Salários de Funcionários",
            Tipo = "pagar",
            Icone = "💰",
        };
        db.CategoriasFinanceiras.Add(categoria);
        await db.SaveChangesAsync();
        return categoria.Id;
    }

    // ── Listar profissionais (com comissão padrão e exceções) ──────
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.Profissionais
            .Where(p => p.LojaId == lojaId)
            .Include(p => p.ComissoesPorServico)
            .OrderBy(p => p.Nome)
            .Select(p => new
            {
                p.Id,
                p.Nome,
                p.Ativo,
                p.ComissaoPadraoPercentual,
                p.DiaPagamentoPadrao,
                p.TipoRemuneracao,
                p.SalarioFixo,
                p.Telefone,
                p.Cep,
                p.Endereco,
                p.ComissaoBaseCalculo,
                comissoesPorServico = p.ComissoesPorServico.Select(c => new { c.Id, c.ServicoId, c.ComissaoPercentual }),
            })
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarProfissionalRequest(
        string Nome,
        decimal? ComissaoPadraoPercentual,
        bool Ativo,
        int? DiaPagamentoPadrao = null,
        string TipoRemuneracao = "comissao",
        decimal? SalarioFixo = null,
        Guid? ContaBancariaId = null,
        string? Telefone = null,
        string? Cep = null,
        string? Endereco = null,
        string ComissaoBaseCalculo = "total"
    );

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarProfissionalRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Nome é obrigatório." });

        if (req.TipoRemuneracao == "salario_fixo" && (!req.SalarioFixo.HasValue || req.SalarioFixo <= 0))
            return BadRequest(new { erro = "Informe o valor do salário fixo." });

        var profissional = new Profissional
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            // Não zera mais em salario_fixo — um funcionário pode ter salário fixo E ainda
            // ganhar comissão em cima de Ordem de Serviço/Agendamento (ex: Anderson).
            ComissaoPadraoPercentual = req.ComissaoPadraoPercentual,
            DiaPagamentoPadrao = req.DiaPagamentoPadrao is >= 1 and <= 28 ? req.DiaPagamentoPadrao : null,
            Ativo = req.Ativo,
            TipoRemuneracao = req.TipoRemuneracao,
            SalarioFixo = req.TipoRemuneracao == "salario_fixo" ? req.SalarioFixo : null,
            Telefone = req.Telefone,
            Cep = req.Cep,
            Endereco = req.Endereco,
            ComissaoBaseCalculo = req.ComissaoBaseCalculo == "servico" ? "servico" : "total",
        };
        db.Profissionais.Add(profissional);
        await db.SaveChangesAsync();

        if (req.TipoRemuneracao == "salario_fixo" && req.ContaBancariaId.HasValue)
        {
            var categoriaId = await ObterOuCriarCategoriaSalarioAsync(lojaId.Value);
            var fixo = new LancamentoFixo
            {
                LojaId = lojaId.Value,
                ContaBancariaId = req.ContaBancariaId.Value,
                Tipo = "pagar",
                Descricao = $"Salário — {profissional.Nome}",
                CategoriaId = categoriaId,
                Valor = req.SalarioFixo!.Value,
                DiaVencimento = req.DiaPagamentoPadrao is >= 1 and <= 28 ? req.DiaPagamentoPadrao.Value : 5,
            };
            db.LancamentosFixos.Add(fixo);
            await db.SaveChangesAsync();

            var agora = DateTime.UtcNow;
            var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            await financeiroService.GerarLoteFixoAsync(fixo, mesAtual);
            await db.SaveChangesAsync();

            profissional.LancamentoFixoId = fixo.Id;
            await db.SaveChangesAsync();
        }

        return Ok(new
        {
            profissional.Id,
            profissional.Nome,
            profissional.Ativo,
            profissional.ComissaoPadraoPercentual,
            profissional.DiaPagamentoPadrao,
            profissional.TipoRemuneracao,
            profissional.SalarioFixo,
            profissional.Telefone,
            profissional.Cep,
            profissional.Endereco,
            profissional.ComissaoBaseCalculo,
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarProfissionalRequest req)
    {
        var lojaId = await GetLojaId();
        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (profissional is null) return NotFound();

        if (req.TipoRemuneracao == "salario_fixo" && (!req.SalarioFixo.HasValue || req.SalarioFixo <= 0))
            return BadRequest(new { erro = "Informe o valor do salário fixo." });

        var tipoAnterior = profissional.TipoRemuneracao;

        profissional.Nome = req.Nome.Trim();
        profissional.ComissaoPadraoPercentual = req.ComissaoPadraoPercentual;
        profissional.DiaPagamentoPadrao = req.DiaPagamentoPadrao is >= 1 and <= 28 ? req.DiaPagamentoPadrao : null;
        profissional.Ativo = req.Ativo;
        profissional.TipoRemuneracao = req.TipoRemuneracao;
        profissional.SalarioFixo = req.TipoRemuneracao == "salario_fixo" ? req.SalarioFixo : null;
        profissional.Telefone = req.Telefone;
        profissional.Cep = req.Cep;
        profissional.Endereco = req.Endereco;
        profissional.ComissaoBaseCalculo = req.ComissaoBaseCalculo == "servico" ? "servico" : "total";

        // Saiu do salário fixo — desativa o lançamento fixo antigo, se existir
        if (tipoAnterior == "salario_fixo" && req.TipoRemuneracao != "salario_fixo" && profissional.LancamentoFixoId.HasValue)
        {
            var fixoAntigo = await db.LancamentosFixos.FindAsync(profissional.LancamentoFixoId.Value);
            if (fixoAntigo != null) fixoAntigo.Ativa = false;
            profissional.LancamentoFixoId = null;
        }
        // Continua ou entrou em salário fixo — cria ou atualiza o lançamento fixo
        else if (req.TipoRemuneracao == "salario_fixo" && req.ContaBancariaId.HasValue)
        {
            if (profissional.LancamentoFixoId.HasValue)
            {
                var fixo = await db.LancamentosFixos.FindAsync(profissional.LancamentoFixoId.Value);
                if (fixo != null)
                {
                    fixo.ContaBancariaId = req.ContaBancariaId.Value;
                    fixo.Valor = req.SalarioFixo!.Value;
                    fixo.DiaVencimento = req.DiaPagamentoPadrao is >= 1 and <= 28 ? req.DiaPagamentoPadrao.Value : fixo.DiaVencimento;
                    fixo.Ativa = true;

                    await financeiroService.LimparFuturosAsync(fixo.Id);
                    var agora = DateTime.UtcNow;
                    var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    await financeiroService.GerarLoteFixoAsync(fixo, mesAtual);
                }
            }
            else
            {
                var categoriaId = await ObterOuCriarCategoriaSalarioAsync(lojaId!.Value);
                var fixo = new LancamentoFixo
                {
                    LojaId = lojaId.Value,
                    ContaBancariaId = req.ContaBancariaId.Value,
                    Tipo = "pagar",
                    Descricao = $"Salário — {profissional.Nome}",
                    CategoriaId = categoriaId,
                    Valor = req.SalarioFixo!.Value,
                    DiaVencimento = req.DiaPagamentoPadrao is >= 1 and <= 28 ? req.DiaPagamentoPadrao.Value : 5,
                };
                db.LancamentosFixos.Add(fixo);
                await db.SaveChangesAsync();

                var agora = DateTime.UtcNow;
                var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                await financeiroService.GerarLoteFixoAsync(fixo, mesAtual);

                profissional.LancamentoFixoId = fixo.Id;
            }
        }

        await db.SaveChangesAsync();

        return Ok(new
        {
            profissional.Id,
            profissional.Nome,
            profissional.Ativo,
            profissional.ComissaoPadraoPercentual,
            profissional.DiaPagamentoPadrao,
            profissional.TipoRemuneracao,
            profissional.SalarioFixo,
            profissional.Telefone,
            profissional.Cep,
            profissional.Endereco,
            profissional.ComissaoBaseCalculo,
        });
    }

    [HttpPatch("{id:guid}/ativo")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AlternarAtivo(Guid id)
    {
        var lojaId = await GetLojaId();
        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (profissional is null) return NotFound();

        profissional.Ativo = !profissional.Ativo;

        if (profissional.LancamentoFixoId.HasValue)
        {
            var fixo = await db.LancamentosFixos.FindAsync(profissional.LancamentoFixoId.Value);
            if (fixo != null) fixo.Ativa = profissional.Ativo;
        }

        await db.SaveChangesAsync();
        return Ok(new { profissional.Id, profissional.Ativo });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (profissional is null) return NotFound();

        var temAgendamentos = await db.Agendamentos.AnyAsync(a => a.ProfissionalId == id);
        var temComissoes = await db.ComissoesFuncionario.AnyAsync(c => c.ProfissionalId == id);
        if (temAgendamentos || temComissoes)
        {
            profissional.Ativo = false; // desativa em vez de excluir, se já usado
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Profissional em uso — foi desativado em vez de excluído." });
        }

        db.Profissionais.Remove(profissional);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Profissional excluído." });
    }

    // ── Comissão por serviço (exceção ao padrão) ───────────────────
    public record SalvarComissaoServicoRequest(Guid ServicoId, decimal ComissaoPercentual);

    [HttpPost("{id:guid}/comissoes-servico")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> DefinirComissaoServico(Guid id, [FromBody] SalvarComissaoServicoRequest req)
    {
        var lojaId = await GetLojaId();
        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (profissional is null) return NotFound();

        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == req.ServicoId && s.LojaId == lojaId);
        if (servico is null) return BadRequest(new { erro = "Serviço não encontrado." });

        var existente = await db.ComissoesServicoProfissional
            .FirstOrDefaultAsync(c => c.ProfissionalId == id && c.ServicoId == req.ServicoId);

        if (existente != null)
        {
            existente.ComissaoPercentual = req.ComissaoPercentual;
        }
        else
        {
            db.ComissoesServicoProfissional.Add(new ComissaoServicoProfissional
            {
                LojaId = lojaId!.Value,
                ProfissionalId = id,
                ServicoId = req.ServicoId,
                ComissaoPercentual = req.ComissaoPercentual,
            });
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Comissão do serviço definida." });
    }

    [HttpDelete("comissoes-servico/{comissaoId:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> RemoverComissaoServico(Guid comissaoId)
    {
        var lojaId = await GetLojaId();
        var comissao = await db.ComissoesServicoProfissional.FirstOrDefaultAsync(c => c.Id == comissaoId && c.LojaId == lojaId);
        if (comissao is null) return NotFound();

        db.ComissoesServicoProfissional.Remove(comissao);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Exceção removida — volta a usar a comissão padrão do profissional." });
    }

    // ── Lista simplificada (id + nome), usada em seletores de outras telas ──
    [HttpGet("ativos")]
    public async Task<IActionResult> ListarAtivos()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.Profissionais
            .Where(p => p.LojaId == lojaId && p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new { p.Id, p.Nome })
            .ToListAsync();

        return Ok(lista);
    }
}