using LojaApi.Domain.Plataforma;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// O superadmin cria uma loja: trial de 7 dias, usuário admin, vínculo e primeira fatura; se escolheu um perfil, copia categorias e serviços
// (e só define o tipo de plano do perfil quando não informou módulos).
public class CriarLojaAdminCommandHandler(IPlataformaRepository plataforma, IAdminRepository admin) : ICommandHandler<CriarLojaAdminCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarLojaAdminCommand command)
    {
        var req = command.Req;

        if (await plataforma.EmailDeLojaExisteAsync(req.Email))
            return Resultado.Conflito("E-mail já cadastrado.");

        if (!string.IsNullOrEmpty(req.Cnpj) && await admin.CnpjDeLojaExisteAsync(req.Cnpj))
            return Resultado.Conflito("CNPJ já cadastrado.");

        var loja = new Loja
        {
            Nome = req.Nome,
            Email = req.Email,
            Cnpj = req.Cnpj,
            Cpf = req.Cpf,
            Telefone = req.Telefone,
            Endereco = req.Endereco,
            CorPrimaria = req.CorPrimaria,
            MensalidadeDia = req.MensalidadeDia,
            MensalidadeValor = req.MensalidadeValor,
            TipoPlano = req.TipoPlano ?? "loja",
            ModulosAtivos = req.ModulosAtivos ?? "",
            EhTeste = req.EhTeste,
            Status = StatusLoja.Trial,
            TrialAte = DateTime.UtcNow.AddDays(PlataformaRegras.DiasDeTrial),
            SchemaNome = PlataformaRegras.GerarSchemaNome(req.Nome, DateTime.UtcNow),
        };
        plataforma.Adicionar(loja);

        var usuario = new Usuario
        {
            Nome = req.AdminNome,
            Email = req.AdminEmail,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.AdminSenha),
            Role = "admin",
        };
        plataforma.Adicionar(usuario);
        plataforma.Adicionar(new UsuarioLoja { LojaId = loja.Id, UsuarioId = usuario.Id, Role = "admin" });

        // Primeira fatura (vence ao fim do trial)
        plataforma.Adicionar(new Pagamento { LojaId = loja.Id, Valor = req.MensalidadeValor, Status = "pendente", Vencimento = loja.TrialAte });

        await plataforma.SalvarAsync();

        if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilGuid))
        {
            var perfil = await plataforma.ObterPerfilComCategoriasEServicosAsync(perfilGuid);
            if (perfil != null)
            {
                foreach (var cat in perfil.Categorias.OrderBy(c => c.Ordem))
                    plataforma.Adicionar(new CategoriaLoja { LojaId = loja.Id, Nome = cat.Nome, Ordem = cat.Ordem, TipoTamanho = cat.TipoTamanho });

                foreach (var s in perfil.Servicos.OrderBy(s => s.Ordem))
                    plataforma.Adicionar(new Servico { LojaId = loja.Id, Nome = s.Nome, Categoria = s.Categoria, Preco = s.Preco, DuracaoMin = s.DuracaoMin, Ativo = true });

                // Só aplica o tipo do perfil se o admin não definiu módulos manualmente
                if (string.IsNullOrEmpty(loja.ModulosAtivos))
                {
                    loja.TipoPlano = perfil.TipoPlanoAplica;
                    if (perfil.TipoPlanoAplica == "servicos" || perfil.TipoPlanoAplica == "loja_modulos")
                        loja.ModulosAtivos = "servicos";
                }

                await plataforma.SalvarAsync();
            }
        }

        return Resultado.Ok(PlataformaApoio.ParaLojaDto(loja, DateTime.UtcNow));
    }
}
