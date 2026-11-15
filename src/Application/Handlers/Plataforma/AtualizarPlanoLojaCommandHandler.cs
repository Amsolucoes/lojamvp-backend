using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Define o tipo de plano e os módulos ativos da loja (módulos nulos viram vazio).
public class AtualizarPlanoLojaCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AtualizarPlanoLojaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarPlanoLojaCommand command)
    {
        var (id, req) = command;
        var loja = await repositorio.ObterLojaAsync(id);
        if (loja is null) return Resultado.NaoEncontrado();

        loja.TipoPlano = req.TipoPlano;
        loja.ModulosAtivos = req.ModulosAtivos ?? "";
        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new LojaPlanoDto(loja.Id, loja.TipoPlano, loja.ModulosAtivos));
    }
}
