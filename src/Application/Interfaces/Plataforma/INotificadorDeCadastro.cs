using LojaApi.Models;

namespace LojaApi.Application.Plataforma;

// Porta: e-mails de novo cadastro (aviso interno + boas-vindas). Implementada na Infrastructure com o Resend.
// Falha ao enviar é registrada e não derruba o cadastro.
public interface INotificadorDeCadastro
{
    Task NotificarAsync(Loja loja, Usuario usuario, PerfilLoja? perfil);
}
