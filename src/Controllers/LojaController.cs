using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Services;
using LojaApi.DTOs;


namespace LojaApi.Controllers;

[ApiController]
[Route("api/loja")]
[Authorize]
public class LojaController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet("situacao")]
    public async Task<IActionResult> Situacao()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { fase = "superadmin", diasRestantes = 0 });

        var loja = await db.Lojas.FindAsync(lojaId.Value);
        if (loja is null) return NotFound();

        var (fase, dias) = TenantService.CalcularSituacao(loja);

        return Ok(new
        {
            fase,                       // trial | carencia | ativo | bloqueado | cancelado
            diasRestantes = dias,
            status = loja.Status.ToString(),
            mensalidadeValor = loja.MensalidadeValor,
            trialAte = loja.TrialAte,
            proximoVencimento = loja.ProximoVencimento,
            nomeLoja = loja.Nome,
            tipoPlano = loja.TipoPlano,
            modulosAtivos = loja.ModulosAtivos
             .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            agendaHoraInicio = loja.AgendaHoraInicio,
            agendaHoraFim = loja.AgendaHoraFim,
            assinaturaStatus = loja.AssinaturaStatus,
            assinaturaCartaoFinal = loja.AssinaturaCartaoFinal,
            agendamentoOnlineAtivo = loja.AgendamentoOnlineAtivo,
            agendamentoOnlineConfirmacao = string.IsNullOrEmpty(loja.AgendamentoOnlineConfirmacao) ? "aprovacao" : loja.AgendamentoOnlineConfirmacao,
            slug = loja.Slug,
            modulosAlteradoEm = loja.ModulosAlteradoEm, // propriedade NotMapped, serializa ok no retorno
        });
    }

    [HttpPatch("agenda-horario")]
    public async Task<IActionResult> AtualizarAgendaHorario([FromBody] AgendaHorarioRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return NotFound();

        var loja = await db.Lojas.FindAsync(lojaId.Value);
        if (loja is null) return NotFound();

        loja.AgendaHoraInicio = Math.Clamp(req.HoraInicio, 0, 23);
        loja.AgendaHoraFim = Math.Clamp(req.HoraFim, 1, 24);
        loja.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { loja.AgendaHoraInicio, loja.AgendaHoraFim });
    }

    [HttpPatch("agendamento-online")]
    public async Task<IActionResult> ConfigAgendamentoOnline([FromBody] AgendamentoOnlineConfigRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return NotFound();

        var loja = await db.Lojas.FindAsync(lojaId.Value);
        if (loja is null) return NotFound();

        // Normaliza e valida o slug (se enviado)
        if (!string.IsNullOrWhiteSpace(req.Slug))
        {
            var slug = GerarSlug(req.Slug);
            if (slug.Length < 3)
                return BadRequest(new { erro = "O link deve ter ao menos 3 caracteres válidos." });

            // Verifica se já está em uso por outra loja
            var emUso = await db.Lojas.AnyAsync(l => l.Slug == slug && l.Id != loja.Id);
            if (emUso)
                return Conflict(new { erro = "Este link já está em uso. Escolha outro." });

            loja.Slug = slug;
        }

        loja.AgendamentoOnlineAtivo = req.Ativo;
        if (!string.IsNullOrWhiteSpace(req.Confirmacao))
            loja.AgendamentoOnlineConfirmacao = req.Confirmacao == "automatico" ? "automatico" : "aprovacao";

        // Se está ativando, precisa ter slug
        if (loja.AgendamentoOnlineAtivo && string.IsNullOrWhiteSpace(loja.Slug))
            return BadRequest(new { erro = "Defina um link (slug) antes de ativar o agendamento online." });

        loja.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new
        {
            loja.AgendamentoOnlineAtivo,
            loja.AgendamentoOnlineConfirmacao,
            loja.Slug,
        });
    }

    // ── Cliente ativa/desativa módulo sozinho ───────────────────────
    public record AlternarModuloRequest(string Chave, bool Ativar);

    [HttpPatch("modulos")]
    public async Task<IActionResult> AlternarModulo([FromBody] AlternarModuloRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return NotFound(new { erro = "Loja não encontrada." });

        var loja = await db.Lojas.FindAsync(lojaId.Value);
        if (loja is null) return NotFound();

        var moduloPreco = await db.ModulosPreco.FirstOrDefaultAsync(m => m.Chave == req.Chave);
        if (moduloPreco is null || !moduloPreco.DisponivelParaAtivar)
            return BadRequest(new { erro = "Módulo indisponível." });

        // Cooldown de 30 dias por módulo
        const int CooldownDias = 30;
        if (loja.ModulosAlteradoEm.TryGetValue(req.Chave, out var ultimaAlteracao))
        {
            var diasPassados = (DateTime.UtcNow - ultimaAlteracao).TotalDays;
            if (diasPassados < CooldownDias)
            {
                var diasRestantes = (int)Math.Ceiling(CooldownDias - diasPassados);
                return BadRequest(new
                {
                    erro = $"Este módulo foi alterado recentemente. Aguarde {diasRestantes} dia(s) para alterar novamente.",
                    cooldown = true,
                    diasRestantes
                });
            }
        }

        var lista = (loja.ModulosAtivos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        if (req.Ativar)
        {
            if (!lista.Contains(req.Chave)) lista.Add(req.Chave);
            loja.MensalidadeValor += moduloPreco.Valor;
        }
        else
        {
            if (lista.Contains(req.Chave))
            {
                lista.Remove(req.Chave);
                loja.MensalidadeValor = Math.Max(0, loja.MensalidadeValor - moduloPreco.Valor);
            }
        }

        loja.ModulosAtivos = string.Join(",", lista);

        // Atualiza o dicionário e reatribui para forçar o EF a rastrear a mudança no campo json
        var alterados = loja.ModulosAlteradoEm;
        alterados[req.Chave] = DateTime.UtcNow;
        loja.ModulosAlteradoEm = alterados;

        loja.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { loja.ModulosAtivos, loja.MensalidadeValor });
    }

    // Normaliza texto para slug de URL (minúsculo, sem acento, hífens)
    private static string GerarSlug(string texto)
    {
        var normalizado = texto.Trim().ToLowerInvariant();
        var semAcento = new string(normalizado
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
        var slug = new string(semAcento.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        // Remove hífens duplicados e das pontas
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}