using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;
using LojaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokenService, TenantService tenantService, Resend.IResend resend, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var usuario = await db.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == req.Email.ToLower() && u.Ativo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(req.Senha, usuario.SenhaHash))
            return Unauthorized(new { erro = "E-mail ou senha incorretos." });

        if (usuario.Role != "superadmin")
        {
            var vinculo = await db.UsuariosLoja
                .Include(ul => ul.Loja)
                .FirstOrDefaultAsync(ul => ul.UsuarioId == usuario.Id && ul.Ativo);

            if (vinculo != null)
                await tenantService.VerificarAcessoAsync(vinculo.LojaId);
        }

        usuario.UltimoLoginEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var token = tokenService.GerarToken(usuario);
        return Ok(new LoginResponse(token, usuario.Nome, usuario.Email, usuario.Role));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nome = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var ehAcessoSuporte = User.FindFirstValue("acesso_suporte") == "true";

        // Atualiza o último acesso ao retomar sessão — mas NÃO conta acesso de suporte (superadmin logado como o cliente)
        if (!ehAcessoSuporte && Guid.TryParse(id, out var usuarioId))
        {
            var usuario = await db.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                usuario.UltimoLoginEm = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        return Ok(new { id, nome, email, role });
    }

    [HttpGet("gerar-hash/{senha}")]
    [AllowAnonymous]
    public IActionResult GerarHash(string senha)
    {
        return Ok(new { hash = BCrypt.Net.BCrypt.HashPassword(senha) });
    }

    // ── Auto-cadastro (signup público) ────────────────────────────
    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SignupRequest req)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(req.NomeLoja) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Senha))
            return BadRequest(new { erro = "Preencha todos os campos obrigatórios." });

        if (req.Senha.Length < 6)
            return BadRequest(new { erro = "A senha deve ter pelo menos 6 caracteres." });

        if (await db.Usuarios.AnyAsync(u => u.Email.ToLower() == req.Email.ToLower()))
            return Conflict(new { erro = "Este e-mail já está cadastrado." });

        if (await db.Lojas.AnyAsync(l => l.Email.ToLower() == req.Email.ToLower()))
            return Conflict(new { erro = "Este e-mail já está cadastrado." });

        // Descobre se o perfil escolhido é "financeiro puro" — nesse caso o preço é fixo
        // e NÃO entra na promoção das 10 primeiras lojas (que é voltada pro plano Loja)
        string? tipoPlanoDoPerfil = null;
        if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilGuidCheck))
        {
            tipoPlanoDoPerfil = await db.PerfisLoja
                .Where(p => p.Id == perfilGuidCheck)
                .Select(p => p.TipoPlanoAplica)
                .FirstOrDefaultAsync();
        }
        bool ehFinanceiroPuro = tipoPlanoDoPerfil == "financeiro";

        // Conta lojas existentes para a promoção das 10 primeiras (não conta financeiro puro, que tem preço fixo à parte)
        var totalLojas = await db.Lojas.CountAsync(l => !l.EhTeste && l.TipoPlano != "financeiro");
        bool ehPromocional = !ehFinanceiroPuro && totalLojas < 10;

        var loja = new Loja
        {
            Nome = req.NomeLoja,
            Email = req.Email,
            Telefone = req.Telefone,
            CorPrimaria = "#c38228",
            MensalidadeDia = DateTime.UtcNow.Day,
            MensalidadeValor = ehFinanceiroPuro ? 39.90m : (ehPromocional ? 89.90m : 119.90m),
            Promocional = ehPromocional,
            ValorPromocional = ehPromocional ? 89.90m : null,
            ValorPosPromocional = ehPromocional ? 119.90m : null,
            MesesPromocional = ehPromocional ? 3 : 0,
            Status = StatusLoja.Trial,
            TrialAte = DateTime.UtcNow.AddDays(7),
            SchemaNome = TenantService.GerarSchemaNome(req.NomeLoja),
        };
        db.Lojas.Add(loja);

        // Usuário admin da loja
        var usuario = new Usuario
        {
            Nome = req.NomeResponsavel,
            Email = req.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha),
            Role = "admin",
        };
        db.Usuarios.Add(usuario);

        db.UsuariosLoja.Add(new UsuarioLoja
        {
            LojaId = loja.Id,
            UsuarioId = usuario.Id,
            Role = "admin",
        });

        // Primeira fatura (vence ao fim do trial)
        db.Pagamentos.Add(new Pagamento
        {
            LojaId = loja.Id,
            Valor = loja.MensalidadeValor,
            Status = "pendente",
            Vencimento = loja.TrialAte,
        });

        await db.SaveChangesAsync();

        // Aplica o perfil escolhido (categorias, serviços, tipo de plano)
        if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilGuid))
        {
            var perfil = await db.PerfisLoja
                .Include(p => p.Categorias)
                .Include(p => p.Servicos)
                .FirstOrDefaultAsync(p => p.Id == perfilGuid);
            if (perfil != null)
            {
                // Categorias de produto
                foreach (var cat in perfil.Categorias.OrderBy(c => c.Ordem))
                    db.CategoriasLoja.Add(new CategoriaLoja
                    {
                        LojaId = loja.Id,
                        Nome = cat.Nome,
                        Ordem = cat.Ordem,
                        TipoTamanho = cat.TipoTamanho,
                    });

                // Serviços pré-definidos
                foreach (var s in perfil.Servicos.OrderBy(s => s.Ordem))
                    db.Servicos.Add(new Servico
                    {
                        LojaId = loja.Id,
                        Nome = s.Nome,
                        Categoria = s.Categoria,
                        Preco = s.Preco,
                        DuracaoMin = s.DuracaoMin,
                        Ativo = true,
                    });

                // Tipo de plano e módulos conforme o perfil
                loja.TipoPlano = perfil.TipoPlanoAplica;
                if (perfil.TipoPlanoAplica == "servicos" || perfil.TipoPlanoAplica == "loja_modulos")
                    loja.ModulosAtivos = "servicos";
                else if (perfil.Nome == "Corretora")
                    loja.ModulosAtivos = "corretora";
                else if (perfil.Nome.StartsWith("Pilates"))
                    loja.ModulosAtivos = "turmas";

                await db.SaveChangesAsync();
            }
        }

        // Avisa por e-mail que uma nova loja se cadastrou
        _ = EnviarAvisoNovoCadastroAsync(loja, usuario, req.PerfilId);

        // Gera token e já loga
        var token = tokenService.GerarToken(usuario);
        return Ok(new LoginResponse(token, usuario.Nome, usuario.Email, usuario.Role));
    }

    private async Task EnviarAvisoNovoCadastroAsync(Loja loja, Usuario usuario, string? perfilId)
    {
        try
        {
            string? nomePerfil = null;
            if (!string.IsNullOrEmpty(perfilId) && Guid.TryParse(perfilId, out var perfilGuid))
            {
                nomePerfil = await db.PerfisLoja
                    .Where(p => p.Id == perfilGuid)
                    .Select(p => p.Nome)
                    .FirstOrDefaultAsync();
            }

            var html = $@"
                <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                    <h2 style='color:#c38228'>🎉 Nova loja cadastrada!</h2>
                    <table style='width:100%;border-collapse:collapse;margin-top:12px'>
                        <tr><td style='padding:6px 10px;border-bottom:1px solid #eee;color:#888'>Nome da loja</td><td style='padding:6px 10px;border-bottom:1px solid #eee'><strong>{loja.Nome}</strong></td></tr>
                        <tr><td style='padding:6px 10px;border-bottom:1px solid #eee;color:#888'>Perfil escolhido</td><td style='padding:6px 10px;border-bottom:1px solid #eee'>{nomePerfil ?? "Começar do zero"}</td></tr>
                        <tr><td style='padding:6px 10px;border-bottom:1px solid #eee;color:#888'>Tipo de plano</td><td style='padding:6px 10px;border-bottom:1px solid #eee'>{loja.TipoPlano}</td></tr>
                        <tr><td style='padding:6px 10px;border-bottom:1px solid #eee;color:#888'>Mensalidade</td><td style='padding:6px 10px;border-bottom:1px solid #eee'>R$ {loja.MensalidadeValor:N2}{(loja.Promocional ? " (promocional)" : "")}</td></tr>
                        <tr><td style='padding:6px 10px;border-bottom:1px solid #eee;color:#888'>Responsável</td><td style='padding:6px 10px;border-bottom:1px solid #eee'>{usuario.Nome}</td></tr>
                        <tr><td style='padding:6px 10px;border-bottom:1px solid #eee;color:#888'>E-mail</td><td style='padding:6px 10px;border-bottom:1px solid #eee'>{loja.Email}</td></tr>
                        <tr><td style='padding:6px 10px;border-bottom:1px solid #eee;color:#888'>Telefone</td><td style='padding:6px 10px;border-bottom:1px solid #eee'>{loja.Telefone ?? "—"}</td></tr>
                        <tr><td style='padding:6px 10px;color:#888'>Trial até</td><td style='padding:6px 10px'>{loja.TrialAte:dd/MM/yyyy}</td></tr>
                    </table>
                </div>";

            var msg = new Resend.EmailMessage
            {
                From = "AldevSoftware <financeiro@aldevsoftware.com.br>",
                Subject = $"🎉 Novo cadastro: {loja.Nome}",
                HtmlBody = html,
            };
            msg.To.Add("andre.ivarras@gmail.com");
            await resend.EmailSendAsync(msg);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar aviso de novo cadastro.");
        }
    }

    // ── Trocar senha (usuário logado) ─────────────────────────────
    [HttpPost("trocar-senha")]
    [Authorize]
    public async Task<IActionResult> TrocarSenha([FromBody] TrocarSenhaRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await db.Usuarios.FindAsync(userId);
        if (usuario is null) return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(req.SenhaAtual, usuario.SenhaHash))
            return BadRequest(new { erro = "Senha atual incorreta." });

        if (req.NovaSenha.Length < 8)
            return BadRequest(new { erro = "A nova senha deve ter pelo menos 8 caracteres." });

        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.NovaSenha);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Senha alterada com sucesso." });
    }
}