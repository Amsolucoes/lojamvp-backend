using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Services;

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
}