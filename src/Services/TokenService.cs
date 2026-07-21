using LojaApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class TokenService(IConfiguration config)
{
    public string GerarToken(Usuario usuario, bool ehSuporte = false)
    {
        var chave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
        var listaClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Role, usuario.Role),
        };

        // Marca o token como "acesso de suporte" — usado pra não contar como login real do cliente
        if (ehSuporte)
            listaClaims.Add(new Claim("acesso_suporte", "true"));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: listaClaims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256)
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}