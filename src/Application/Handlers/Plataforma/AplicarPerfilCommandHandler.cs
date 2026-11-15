using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Aplica um perfil à loja (copia categorias e campos extras). Só vale uma vez: depois de definido, só pelo suporte.
public class AplicarPerfilCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AplicarPerfilCommand>
{
    public async Task<Resultado<object>> HandleAsync(AplicarPerfilCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado("Loja não encontrada.");

        if (await repositorio.LojaTemCategoriasAsync(lojaId))
            return Resultado.Proibido("O perfil da sua loja já foi definido. Para alterar, fale com o suporte da AldevSoftware.");

        var perfil = await repositorio.ObterPerfilComCategoriasECamposAsync(req.PerfilLojaId);
        if (perfil is null) return Resultado.NaoEncontrado("Perfil não encontrado.");

        await repositorio.RemoverCategoriasECamposDaLojaAsync(lojaId);

        foreach (var cat in perfil.Categorias)
            repositorio.Adicionar(new CategoriaLoja { LojaId = lojaId, Nome = cat.Nome, Ordem = cat.Ordem, TipoTamanho = cat.TipoTamanho });

        foreach (var campo in perfil.CamposExtras)
            repositorio.Adicionar(new CampoExtraLoja
            {
                LojaId = lojaId,
                Chave = campo.Chave,
                Label = campo.Label,
                Tipo = campo.Tipo,
                Opcoes = campo.Opcoes,
                Obrigatorio = campo.Obrigatorio,
                Ordem = campo.Ordem,
            });

        await repositorio.SalvarAsync();
        return Resultado.Ok(new MensagemDto($"Perfil '{perfil.Nome}' aplicado!"));
    }
}
