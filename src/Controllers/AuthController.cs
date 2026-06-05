using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Services;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var usuario = await db.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == req.Email.ToLower() && u.Ativo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(req.Senha, usuario.SenhaHash))
            return Unauthorized(new { erro = "E-mail ou senha incorretos." });

        var token = tokenService.GerarToken(usuario);

        return Ok(new LoginResponse(token, usuario.Nome, usuario.Email, usuario.Role));
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Me()
    {
        var id    = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var nome  = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role  = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return Ok(new { id, nome, email, role });
    }
}
