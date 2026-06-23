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
public class AuthController(AppDbContext db, TokenService tokenService, TenantService tenantService) : ControllerBase
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
            {
                var (ativa, motivo) = await tenantService.VerificarAcessoAsync(vinculo.LojaId);
                if (!ativa)
                    return Unauthorized(new { erro = motivo, bloqueado = true });
            }
        }

        var token = tokenService.GerarToken(usuario);
        return Ok(new LoginResponse(token, usuario.Nome, usuario.Email, usuario.Role));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nome = User.FindFirstValue(ClaimTypes.Name);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);
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

        // Conta lojas existentes para a promoção das 10 primeiras
        var totalLojas = await db.Lojas.CountAsync();
        bool ehPromocional = totalLojas < 10;

        var loja = new Loja
        {
            Nome = req.NomeLoja,
            Email = req.Email,
            Telefone = req.Telefone,
            CorPrimaria = "#c38228",
            MensalidadeDia = DateTime.UtcNow.Day,
            MensalidadeValor = ehPromocional ? 89.90m : 119.90m,
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

        // Aplica o perfil escolhido (categorias + tipo de tamanho)
        if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilGuid))
        {
            var perfil = await db.PerfisLoja
                .Include(p => p.Categorias)
                .FirstOrDefaultAsync(p => p.Id == perfilGuid);

            if (perfil != null)
            {
                foreach (var cat in perfil.Categorias.OrderBy(c => c.Ordem))
                    db.CategoriasLoja.Add(new CategoriaLoja
                    {
                        LojaId = loja.Id,
                        Nome = cat.Nome,
                        Ordem = cat.Ordem,
                        TipoTamanho = cat.TipoTamanho,
                    });

                await db.SaveChangesAsync();
            }
        }

        // Gera token e já loga
        var token = tokenService.GerarToken(usuario);
        return Ok(new LoginResponse(token, usuario.Nome, usuario.Email, usuario.Role));
    }
}