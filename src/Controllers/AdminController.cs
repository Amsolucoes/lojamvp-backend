using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;
using LojaApi.Services;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "superadmin")]
public class AdminController(AppDbContext db, TenantService tenantService, TokenService tokenService,
    MercadoPagoService mpService) : ControllerBase
{
    private Guid AdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Dashboard ─────────────────────────────────────────────────
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var lojas = await db.Lojas.Include(l => l.Pagamentos).Where(l => !l.EhTeste).ToListAsync();
        var agora = DateTime.UtcNow;

        var atrasadas = lojas
            .Where(l => l.ProximoVencimento.HasValue &&
                        (agora - l.ProximoVencimento.Value).TotalDays > 0 &&
                        l.Status != StatusLoja.Cancelado)
            .Select(l => ToResumoDto(l, agora))
            .ToList();

        var ultimosPagamentos = await db.Pagamentos
            .Include(p => p.Loja)
            .Where(p => p.Status == "pago" && !p.Loja!.EhTeste)
            .OrderByDescending(p => p.PagoEm)
            .Take(10)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(new DashboardAdminDto(
            TotalLojas:        lojas.Count,
            LojasAtivas:       lojas.Count(l => l.Status == StatusLoja.Ativo),
            LojasTrial:        lojas.Count(l => l.Status == StatusLoja.Trial),
            LojasBloqueadas:   lojas.Count(l => l.Status == StatusLoja.Bloqueado),
            LojasEmAtraso:     atrasadas.Count,
            ReceitaMensal:     lojas.Where(l => l.Status == StatusLoja.Ativo).Sum(l => l.MensalidadeValor),
            ReceitaTotal:      lojas.SelectMany(l => l.Pagamentos).Where(p => p.Status == "pago").Sum(p => p.Valor),
            LojasAtrasadas:    atrasadas,
            UltimosPagamentos: ultimosPagamentos
        ));
    }

    // ── Listar lojas ──────────────────────────────────────────────
    [HttpGet("lojas")]
    public async Task<IActionResult> Listar([FromQuery] string? status, [FromQuery] string? busca)
    {
        var q = db.Lojas.Include(l => l.Pagamentos).Include(l => l.Usuarios).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusLoja>(status, true, out var s))
            q = q.Where(l => l.Status == s);

        if (!string.IsNullOrEmpty(busca))
            q = q.Where(l => l.Nome.ToLower().Contains(busca.ToLower()) ||
                              l.Email.Contains(busca) ||
                              (l.Cnpj != null && l.Cnpj.Contains(busca)));

        var agora = DateTime.UtcNow;
        var lista = await q.OrderByDescending(l => l.CriadoEm).ToListAsync();
        return Ok(lista.Select(l => ToLojaDto(l, agora)));
    }

    // ── Buscar loja ───────────────────────────────────────────────
    [HttpGet("lojas/{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id)
    {
        var loja = await db.Lojas
            .Include(l => l.Pagamentos)
            .Include(l => l.Usuarios).ThenInclude(u => u.Usuario)
            .FirstOrDefaultAsync(l => l.Id == id);

        return loja is null ? NotFound() : Ok(ToLojaDto(loja, DateTime.UtcNow));
    }

    // ── Criar loja ────────────────────────────────────────────────
    [HttpPost("lojas")]
    public async Task<IActionResult> Criar([FromBody] CriarLojaRequest req)
    {
        if (await db.Lojas.AnyAsync(l => l.Email.ToLower() == req.Email.ToLower()))
            return Conflict(new { erro = "E-mail já cadastrado." });

        if (!string.IsNullOrEmpty(req.Cnpj) && await db.Lojas.AnyAsync(l => l.Cnpj == req.Cnpj))
            return Conflict(new { erro = "CNPJ já cadastrado." });

        var loja = new Loja
        {
            Nome             = req.Nome,
            Email            = req.Email,
            Cnpj             = req.Cnpj,
            Cpf              = req.Cpf,
            Telefone         = req.Telefone,
            Endereco         = req.Endereco,
            CorPrimaria      = req.CorPrimaria,
            MensalidadeDia   = req.MensalidadeDia,
            MensalidadeValor = req.MensalidadeValor,
            Status           = StatusLoja.Trial,
            TrialAte         = DateTime.UtcNow.AddDays(7),
            SchemaNome       = TenantService.GerarSchemaNome(req.Nome),
        };
        db.Lojas.Add(loja);

        // Usuário admin da loja
        var usuario = new Usuario
        {
            Nome      = req.AdminNome,
            Email     = req.AdminEmail,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.AdminSenha),
            Role      = "admin",
        };
        db.Usuarios.Add(usuario);

        db.UsuariosLoja.Add(new UsuarioLoja
        {
            LojaId    = loja.Id,
            UsuarioId = usuario.Id,
            Role      = "admin",
        });

        // Primeira fatura (vence ao fim do trial)
        db.Pagamentos.Add(new Pagamento
        {
            LojaId     = loja.Id,
            Valor      = req.MensalidadeValor,
            Status     = "pendente",
            Vencimento = loja.TrialAte,
        });

        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Buscar), new { id = loja.Id }, ToLojaDto(loja, DateTime.UtcNow));
    }

    // ── Backup completo da loja (JSON) ────────────────────────────
    [HttpGet("lojas/{id:guid}/backup")]
    public async Task<IActionResult> Backup(Guid id)
    {
        var loja = await db.Lojas.FindAsync(id);
        if (loja is null) return NotFound();

        var produtos = await db.Produtos.Where(p => p.LojaId == id)
            .Include(p => p.Variacoes).ToListAsync();
        var clientes = await db.Clientes.Where(c => c.LojaId == id).ToListAsync();
        var vendas = await db.Vendas.Where(v => v.LojaId == id)
            .Include(v => v.Itens).ToListAsync();
        var trocas = await db.Trocas.Where(t => t.LojaId == id)
            .Include(t => t.Itens).ToListAsync();
        var movimentos = await db.Movimentos.Where(m => m.LojaId == id).ToListAsync();
        var categorias = await db.CategoriasLoja.Where(c => c.LojaId == id).ToListAsync();
        var pagamentos = await db.Pagamentos.Where(p => p.LojaId == id).ToListAsync();

        var backup = new
        {
            geradoEm = DateTime.UtcNow,
            loja = new { loja.Id, loja.Nome, loja.Email, loja.Cnpj, loja.Cpf, loja.Telefone, loja.Endereco, loja.Status, loja.CriadoEm },
            categorias,
            produtos,
            clientes,
            vendas,
            trocas,
            movimentos,
            pagamentos,
            totais = new
            {
                produtos = produtos.Count,
                clientes = clientes.Count,
                vendas = vendas.Count,
                trocas = trocas.Count,
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(backup, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        });

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var nomeArquivo = $"backup-{loja.Nome.Replace(" ", "-").ToLower()}-{DateTime.UtcNow:yyyy-MM-dd}.json";
        return File(bytes, "application/json", nomeArquivo);
    }

    // ── Deletar loja (e todos os dados) ───────────────────────────
    [HttpDelete("lojas/{id:guid}")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var loja = await db.Lojas.FindAsync(id);
        if (loja is null) return NotFound();

        // Deleta dados vinculados que não são cascade automático
        var produtos = await db.Produtos.Where(p => p.LojaId == id).Select(p => p.Id).ToListAsync();

        // Variações e movimentos dos produtos
        await db.ProdutoVariacoes.Where(v => produtos.Contains(v.ProdutoId)).ExecuteDeleteAsync();
        await db.Movimentos.Where(m => m.LojaId == id).ExecuteDeleteAsync();

        // Itens de venda e vendas
        var vendas = await db.Vendas.Where(v => v.LojaId == id).Select(v => v.Id).ToListAsync();
        await db.ItensVenda.Where(i => vendas.Contains(i.VendaId)).ExecuteDeleteAsync();
        await db.Vendas.Where(v => v.LojaId == id).ExecuteDeleteAsync();

        // Itens de troca e trocas
        var trocas = await db.Trocas.Where(t => t.LojaId == id).Select(t => t.Id).ToListAsync();
        await db.ItensTroca.Where(i => trocas.Contains(i.TrocaId)).ExecuteDeleteAsync();
        await db.Trocas.Where(t => t.LojaId == id).ExecuteDeleteAsync();

        // Produtos, clientes, categorias
        await db.Produtos.Where(p => p.LojaId == id).ExecuteDeleteAsync();
        await db.Clientes.Where(c => c.LojaId == id).ExecuteDeleteAsync();
        await db.CategoriasLoja.Where(c => c.LojaId == id).ExecuteDeleteAsync();
        await db.CamposExtrasLoja.Where(c => c.LojaId == id).ExecuteDeleteAsync();

        // Usuários vinculados (pega os ids antes)
        var usuarioIds = await db.UsuariosLoja.Where(ul => ul.LojaId == id).Select(ul => ul.UsuarioId).ToListAsync();
        await db.UsuariosLoja.Where(ul => ul.LojaId == id).ExecuteDeleteAsync();
        await db.Usuarios.Where(u => usuarioIds.Contains(u.Id)).ExecuteDeleteAsync();

        // Pagamentos e a loja (pagamentos é cascade, mas deletamos explícito por segurança)
        await db.Pagamentos.Where(p => p.LojaId == id).ExecuteDeleteAsync();
        await db.Lojas.Where(l => l.Id == id).ExecuteDeleteAsync();

        return Ok(new { mensagem = "Loja e todos os dados foram removidos." });
    }

    // ── Acessar loja como suporte (gera token do admin da loja) ───
    [HttpPost("lojas/{id:guid}/acessar")]
    public async Task<IActionResult> AcessarComoSuporte(Guid id)
    {
        var loja = await db.Lojas.FindAsync(id);
        if (loja is null) return NotFound(new { erro = "Loja não encontrada." });

        // Pega o usuário admin da loja
        var vinculo = await db.UsuariosLoja
            .Include(ul => ul.Usuario)
            .FirstOrDefaultAsync(ul => ul.LojaId == id && ul.Role == "admin" && ul.Ativo);

        if (vinculo is null || vinculo.Usuario is null)
            return NotFound(new { erro = "Esta loja não tem um administrador ativo." });

        var token = tokenService.GerarToken(vinculo.Usuario);

        return Ok(new
        {
            token,
            nome = vinculo.Usuario.Nome,
            email = vinculo.Usuario.Email,
            role = vinculo.Usuario.Role,
            nomeLoja = loja.Nome,
        });
    }

    // ── Atualizar loja ────────────────────────────────────────────
    [HttpPut("lojas/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarLojaRequest req)
    {
        var loja = await db.Lojas.FindAsync(id);
        if (loja is null) return NotFound();

        loja.Nome             = req.Nome;
        loja.Email            = req.Email;
        loja.Cnpj             = req.Cnpj;
        loja.Cpf              = req.Cpf;
        loja.Telefone         = req.Telefone;
        loja.Endereco         = req.Endereco;
        loja.CorPrimaria      = req.CorPrimaria;
        loja.LogoUrl          = req.LogoUrl;
        loja.MensalidadeDia   = req.MensalidadeDia;
        loja.MensalidadeValor = req.MensalidadeValor;
        if (req.TipoPlano is not null) loja.TipoPlano = req.TipoPlano;
        if (req.ModulosAtivos is not null) loja.ModulosAtivos = req.ModulosAtivos;
        if (req.EhTeste is not null) loja.EhTeste = req.EhTeste.Value;
        loja.AtualizadoEm     = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToLojaDto(loja, DateTime.UtcNow));
    }

    // ── Alterar status (bloquear/desbloquear) ─────────────────────
    [HttpPatch("lojas/{id:guid}/status")]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusLojaRequest req)
    {
        var loja = await db.Lojas.FindAsync(id);
        if (loja is null) return NotFound();

        if (!Enum.TryParse<StatusLoja>(req.Status, true, out var novoStatus))
            return BadRequest(new { erro = "Status inválido." });

        loja.Status       = novoStatus;
        loja.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { mensagem = $"Status alterado para {novoStatus}." });
    }

    // ── Registrar pagamento manual ────────────────────────────────
    [HttpPost("pagamentos")]
    public async Task<IActionResult> RegistrarPagamento([FromBody] RegistrarPagamentoManualRequest req)
    {
        var ok = await tenantService.RegistrarPagamentoAsync(
            req.LojaId, req.Valor, req.Vencimento,
            req.PagoEm, req.FormaPagamento,
            req.Observacao, AdminId);

        return ok
            ? Ok(new { mensagem = "Pagamento registrado e loja reativada." })
            : NotFound(new { erro = "Loja não encontrada." });
    }

    // ── Listar pagamentos de uma loja ─────────────────────────────
    [HttpGet("lojas/{id:guid}/pagamentos")]
    public async Task<IActionResult> Pagamentos(Guid id)
    {
        var lista = await db.Pagamentos
            .Include(p => p.Loja)
            .Where(p => p.LojaId == id)
            .OrderByDescending(p => p.Vencimento)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(lista);
    }

    [HttpPatch("lojas/{id}/plano")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AtualizarPlano(Guid id, [FromBody] AtualizarPlanoRequest req)
    {
        var loja = await db.Lojas.FindAsync(id);
        if (loja is null) return NotFound();

        loja.TipoPlano = req.TipoPlano;
        loja.ModulosAtivos = req.ModulosAtivos ?? "";
        loja.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { loja.Id, loja.TipoPlano, loja.ModulosAtivos });
    }

    // ── Trocar e-mail (login do usuário e/ou contato da loja) ─────
    [HttpPatch("lojas/{id:guid}/email")]
    public async Task<IActionResult> TrocarEmail(Guid id, [FromBody] TrocarEmailRequest req)
    {
        var loja = await db.Lojas.FindAsync(id);
        if (loja is null) return NotFound(new { erro = "Loja não encontrada." });

        var novoEmail = req.NovoEmail?.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(novoEmail) || !novoEmail.Contains('@'))
            return BadRequest(new { erro = "E-mail inválido." });

        if (!req.TrocarLogin && !req.TrocarLoja)
            return BadRequest(new { erro = "Escolha ao menos um e-mail para trocar." });

        // Descobre o usuário admin da loja
        var vinculo = await db.UsuariosLoja
            .Include(ul => ul.Usuario)
            .FirstOrDefaultAsync(ul => ul.LojaId == id && ul.Role == "admin");

        // Valida duplicidade do e-mail de login
        if (req.TrocarLogin)
        {
            if (vinculo?.Usuario is null)
                return BadRequest(new { erro = "Usuário admin da loja não encontrado." });

            var emailEmUso = await db.Usuarios
                .AnyAsync(u => u.Email.ToLower() == novoEmail && u.Id != vinculo.Usuario.Id);
            if (emailEmUso)
                return Conflict(new { erro = "Este e-mail já está em uso por outro usuário." });
        }

        // Valida duplicidade do e-mail da loja
        if (req.TrocarLoja)
        {
            var lojaEmailEmUso = await db.Lojas
                .AnyAsync(l => l.Email.ToLower() == novoEmail && l.Id != id);
            if (lojaEmailEmUso)
                return Conflict(new { erro = "Este e-mail já está em uso por outra loja." });
        }

        // Aplica
        if (req.TrocarLogin && vinculo?.Usuario is not null)
            vinculo.Usuario.Email = novoEmail;

        if (req.TrocarLoja)
            loja.Email = novoEmail;

        loja.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "E-mail atualizado com sucesso." });
    }

    // ── Atualizar valor da mensalidade (e opcionalmente a assinatura) ─────
    [HttpPatch("lojas/{id:guid}/valor")]
    public async Task<IActionResult> AtualizarValor(Guid id, [FromBody] AtualizarValorRequest req)
    {
        var loja = await db.Lojas.FindAsync(id);
        if (loja is null) return NotFound(new { erro = "Loja não encontrada." });

        if (req.NovoValor <= 0)
            return BadRequest(new { erro = "Valor inválido." });

        // Atualiza o valor da loja (vale para próximas faturas)
        loja.MensalidadeValor = req.NovoValor;

        // Se pediu para sincronizar E a loja tem assinatura ativa no MP
        bool sincronizou = false;
        string? avisoMp = null;

        if (req.SincronizarAssinatura)
        {
            if (string.IsNullOrEmpty(loja.MpPreapprovalId) || loja.AssinaturaStatus != "authorized")
            {
                avisoMp = "A loja não tem assinatura ativa para sincronizar. Só o valor das próximas faturas foi alterado.";
            }
            else
            {
                sincronizou = await mpService.AtualizarValorAssinatura(loja.MpPreapprovalId, req.NovoValor);
                if (!sincronizou)
                    avisoMp = "O valor da loja foi alterado, mas não foi possível atualizar a assinatura no Mercado Pago. Tente novamente ou verifique manualmente.";
            }
        }

        loja.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new
        {
            mensagem = sincronizou
                ? "Valor atualizado na loja e na assinatura recorrente."
                : "Valor atualizado.",
            aviso = avisoMp,
            sincronizou,
        });
    }

    // ── Mappers ───────────────────────────────────────────────────
    private static LojaDto ToLojaDto(Loja l, DateTime agora)
    {
        var dias = l.ProximoVencimento.HasValue
       ? Math.Max(0, (int)(agora - l.ProximoVencimento.Value).TotalDays)
       : 0;
        var (fase, diasRest) = TenantService.CalcularSituacao(l);
        return new(
            l.Id, l.Nome, l.Email, l.Cnpj, l.Cpf, l.Telefone, l.Endereco,
            l.CorPrimaria, l.LogoUrl, l.Status.ToString(),
            l.TrialAte, l.MensalidadeDia, l.MensalidadeValor,
            l.ProximoVencimento, l.UltimaCobranca,
            l.SchemaNome, l.CriadoEm,
            l.Usuarios.Count,
            l.Pagamentos.Where(p => p.Status == "pago").Sum(p => p.Valor),
            EmAtraso: dias > 0, DiasAtraso: dias,
            Promocional: l.Promocional,
            Fase: fase, DiasRestantes: diasRest,
            TipoPlano: l.TipoPlano, ModulosAtivos: l.ModulosAtivos,
            EhTeste: l.EhTeste,
            AssinaturaStatus: l.AssinaturaStatus,
            AssinaturaCartaoFinal: l.AssinaturaCartaoFinal
        );
    }

    private static LojaResumoDto ToResumoDto(Loja l, DateTime agora)
    {
        var dias = l.ProximoVencimento.HasValue
            ? Math.Max(0, (int)(agora - l.ProximoVencimento.Value).TotalDays) : 0;
        return new(l.Id, l.Nome, l.Email, l.Status.ToString(),
            l.ProximoVencimento, l.MensalidadeValor, dias > 0, dias);
    }

    private static PagamentoDto ToDto(Pagamento p) => new(
        p.Id, p.LojaId, p.Loja?.Nome ?? "",
        p.Valor, p.Status, p.Vencimento, p.PagoEm,
        p.FormaPagamento, p.Observacao,
        p.MpQrCode, p.MpQrCodeBase64,
        p.MpBoletoUrl, p.MpBoletoBarcode,
        p.CriadoEm
    );
}
