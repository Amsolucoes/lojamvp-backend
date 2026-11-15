using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Superadmin altera o valor e a disponibilidade ("em breve") de um módulo.
public class AtualizarModuloPrecoCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AtualizarModuloPrecoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarModuloPrecoCommand command)
    {
        var (id, req) = command;
        var modulo = await repositorio.ObterModuloPrecoAsync(id);
        if (modulo is null) return Resultado.NaoEncontrado();

        modulo.Valor = req.Valor;
        modulo.DisponivelParaAtivar = req.DisponivelParaAtivar;
        modulo.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();
        return Resultado.Ok(modulo);
    }
}
