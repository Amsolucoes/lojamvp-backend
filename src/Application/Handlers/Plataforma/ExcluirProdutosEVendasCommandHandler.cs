using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Apaga produtos, vendas, trocas e ajustes de caixa da loja (mantém clientes, usuários e a loja).
public class ExcluirProdutosEVendasCommandHandler(IAdminRepository repositorio) : ICommandHandler<ExcluirProdutosEVendasCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirProdutosEVendasCommand command)
    {
        if (!await repositorio.LojaExisteAsync(command.Id)) return Resultado.NaoEncontrado();

        await repositorio.ExcluirProdutosEVendasAsync(command.Id);
        return Resultado.Ok(new MensagemDto("Produtos, vendas, trocas e ajustes de caixa foram removidos. Clientes e usuários foram mantidos."));
    }
}
