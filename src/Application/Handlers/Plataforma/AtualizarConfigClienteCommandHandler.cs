using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Atualiza nome, cor, logo, endereço e telefone da loja: só muda o que veio (nulo = não mexe).
public class AtualizarConfigClienteCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AtualizarConfigClienteCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarConfigClienteCommand command)
    {
        var (usuarioId, req) = command;
        var loja = await repositorio.ObterMinhaLojaComPagamentosAsync(usuarioId);
        if (loja is null) return Resultado.NaoEncontrado();

        loja.Nome = req.Nome ?? loja.Nome;
        loja.CorPrimaria = req.CorPrimaria ?? loja.CorPrimaria;
        loja.LogoUrl = req.LogoUrl ?? loja.LogoUrl;
        if (req.Endereco is not null) loja.Endereco = req.Endereco;
        if (req.Telefone is not null) loja.Telefone = req.Telefone;
        loja.AtualizadoEm = DateTime.UtcNow;

        await repositorio.SalvarAsync();
        return Resultado.Ok(new MensagemDto("Configurações atualizadas."));
    }
}
