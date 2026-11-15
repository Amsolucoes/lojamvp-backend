using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Roda na hora a verificação de bloqueio de todas as lojas não canceladas (a mesma que o serviço automático faz).
public class VerificarBloqueiosCommandHandler(IAdminRepository admin, IPlataformaRepository plataforma) : ICommandHandler<VerificarBloqueiosCommand>
{
    public async Task<Resultado<object>> HandleAsync(VerificarBloqueiosCommand command)
    {
        foreach (var loja in await admin.ListarLojasNaoCanceladasAsync())
            PlataformaApoio.AtualizarStatusDaLoja(loja, DateTime.UtcNow);

        await plataforma.SalvarAsync();
        return Resultado.Ok(new MensagemDto("Verificação de bloqueios executada."));
    }
}
