using LojaApi.Domain.Plataforma;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Define o link (slug) da loja: normalizado, com 3+ caracteres válidos e que nenhuma outra loja use.
public class DefinirSlugCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<DefinirSlugCommand>
{
    public async Task<Resultado<object>> HandleAsync(DefinirSlugCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();

        var loja = await repositorio.ObterLojaAsync(lojaId.Value);
        if (loja is null) return Resultado.NaoEncontrado();

        var slug = PlataformaRegras.GerarSlug(req.Slug);
        if (!PlataformaRegras.SlugValido(slug))
            return Resultado.Invalido("O link deve ter ao menos 3 caracteres válidos.");

        if (await repositorio.SlugEmUsoAsync(slug, loja.Id))
            return Resultado.Conflito("Este link já está em uso. Escolha outro.");

        loja.Slug = slug;
        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new SlugDto(loja.Slug));
    }
}
