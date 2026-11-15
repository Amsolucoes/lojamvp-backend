using LojaApi.Domain.Plataforma;
using LojaApi.Models;
using LojaApi.Shared.Application;
using Microsoft.Extensions.Logging;

namespace LojaApi.Application.Plataforma;

// Cadastro público (signup): cria a loja em trial de 7 dias, o usuário admin, o vínculo e a primeira fatura; aplica o perfil escolhido
// (categorias, serviços, tipo de plano e módulos), manda os e-mails de novo cadastro e já devolve o login.
public class CadastrarLojaCommandHandler(IPlataformaRepository repositorio, IGeradorDeToken geradorDeToken, INotificadorDeCadastro notificador,
    ILogger<CadastrarLojaCommandHandler> logger) : ICommandHandler<CadastrarLojaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CadastrarLojaCommand command)
    {
        var req = command.Req;

        if (string.IsNullOrWhiteSpace(req.NomeLoja) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Senha))
            return Resultado.Invalido("Preencha todos os campos obrigatórios.");

        if (req.Senha.Length < 6)
            return Resultado.Invalido("A senha deve ter pelo menos 6 caracteres.");

        if (await repositorio.EmailDeUsuarioExisteAsync(req.Email, null))
            return Resultado.Conflito("Este e-mail já está cadastrado.");

        if (await repositorio.EmailDeLojaExisteAsync(req.Email))
            return Resultado.Conflito("Este e-mail já está cadastrado.");

        // Perfil "financeiro puro": preço fixo, fora da promoção das 10 primeiras lojas (que é voltada ao plano Loja)
        string? tipoPlanoDoPerfil = null;
        if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilGuidCheck))
            tipoPlanoDoPerfil = await repositorio.ObterTipoPlanoDoPerfilAsync(perfilGuidCheck);
        var ehFinanceiroPuro = tipoPlanoDoPerfil == "financeiro";

        var ehPromocional = PlataformaRegras.EhPromocional(ehFinanceiroPuro, await repositorio.ContarLojasDaPromocaoAsync());

        // Módulos extras escolhidos no cadastro: só entram no preço para quem NÃO é financeiro puro
        var modulosExtrasValidos = PlataformaRegras.ModulosExtrasValidos(req.ModulosExtras);
        decimal valorModulosExtras = 0;
        if (!ehFinanceiroPuro && modulosExtrasValidos.Count > 0)
        {
            var precosModulos = await repositorio.ListarModulosDisponiveisAsync(modulosExtrasValidos);
            valorModulosExtras = precosModulos.Sum(m => m.Valor);
            // Tira o módulo que não existe ou não está disponível, para não gravar sujeira
            modulosExtrasValidos = precosModulos.Select(m => m.Chave).ToList();
        }

        var mensalidadeBase = PlataformaRegras.MensalidadeBase(ehFinanceiroPuro, ehPromocional);
        var agora = DateTime.UtcNow;

        var loja = new Loja
        {
            Nome = req.NomeLoja,
            Email = req.Email,
            Telefone = req.Telefone,
            CorPrimaria = "#c38228",
            MensalidadeDia = agora.Day,
            MensalidadeValor = mensalidadeBase + valorModulosExtras,
            Promocional = ehPromocional,
            ValorPromocional = ehPromocional ? PlataformaRegras.MensalidadePromocional : null,
            ValorPosPromocional = ehPromocional ? PlataformaRegras.MensalidadeNormal + valorModulosExtras : null,
            MesesPromocional = ehPromocional ? PlataformaRegras.MesesPromocionais : 0,
            Status = StatusLoja.Trial,
            TrialAte = DateTime.UtcNow.AddDays(PlataformaRegras.DiasDeTrial),
            SchemaNome = PlataformaRegras.GerarSchemaNome(req.NomeLoja, DateTime.UtcNow),
        };
        repositorio.Adicionar(loja);

        // Usuário admin da loja
        var usuario = new Usuario
        {
            Nome = req.NomeResponsavel,
            Email = req.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha),
            Role = "admin",
        };
        repositorio.Adicionar(usuario);
        repositorio.Adicionar(new UsuarioLoja { LojaId = loja.Id, UsuarioId = usuario.Id, Role = "admin" });

        // Primeira fatura (vence ao fim do trial)
        repositorio.Adicionar(new Pagamento { LojaId = loja.Id, Valor = loja.MensalidadeValor, Status = "pendente", Vencimento = loja.TrialAte });

        await repositorio.SalvarAsync();

        // Aplica o perfil escolhido (categorias, serviços, tipo de plano)
        if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilGuid))
        {
            var perfil = await repositorio.ObterPerfilComCategoriasEServicosAsync(perfilGuid);
            if (perfil != null)
            {
                foreach (var cat in perfil.Categorias.OrderBy(c => c.Ordem))
                    repositorio.Adicionar(new CategoriaLoja { LojaId = loja.Id, Nome = cat.Nome, Ordem = cat.Ordem, TipoTamanho = cat.TipoTamanho });

                foreach (var s in perfil.Servicos.OrderBy(s => s.Ordem))
                    repositorio.Adicionar(new Servico { LojaId = loja.Id, Nome = s.Nome, Categoria = s.Categoria, Preco = s.Preco, DuracaoMin = s.DuracaoMin, Ativo = true });

                loja.TipoPlano = perfil.TipoPlanoAplica;
                var modulosDoPerfil = PlataformaRegras.ModulosDoPerfil(perfil.TipoPlanoAplica, perfil.Nome);

                // Junta com os extras escolhidos no cadastro, sem duplicar o que o perfil já traz
                loja.ModulosAtivos = string.Join(",", modulosDoPerfil.Concat(modulosExtrasValidos).Distinct());
                await repositorio.SalvarAsync();
            }
        }
        else if (modulosExtrasValidos.Count > 0)
        {
            // Sem perfil ("Começar do zero"), mas com módulos extras marcados
            loja.ModulosAtivos = string.Join(",", modulosExtrasValidos);
            await repositorio.SalvarAsync();
        }

        // E-mails de novo cadastro: precisam ser aguardados, pois o banco é da requisição e já estaria descartado num envio solto
        try
        {
            PerfilLoja? perfilEscolhido = null;
            if (!string.IsNullOrEmpty(req.PerfilId) && Guid.TryParse(req.PerfilId, out var perfilEmailGuid))
                perfilEscolhido = await repositorio.ObterPerfilParaEmailAsync(perfilEmailGuid);
            await notificador.NotificarAsync(loja, usuario, perfilEscolhido);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao preparar e-mails de novo cadastro.");
        }

        return Resultado.Ok(new LoginResponse(geradorDeToken.Gerar(usuario), usuario.Nome, usuario.Email, usuario.Role));
    }
}
