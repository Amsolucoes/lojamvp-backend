using LojaApi.Models;

namespace LojaApi.Application.Plataforma;

// Porta: gera o token de login (JWT) do usuário. Implementada na Infrastructure com o TokenService.
public interface IGeradorDeToken
{
    // acessoSuporte marca o token de um superadmin que entrou como o cliente (não conta como login real)
    string Gerar(Usuario usuario, bool acessoSuporte = false);
}
