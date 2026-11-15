using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// O superadmin edita os dados da loja. Tipo de plano, módulos e "loja de teste" só mudam quando vêm preenchidos.
public class AtualizarLojaAdminCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AtualizarLojaAdminCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarLojaAdminCommand command)
    {
        var (id, req) = command;
        var loja = await repositorio.ObterLojaAsync(id);
        if (loja is null) return Resultado.NaoEncontrado();

        loja.Nome = req.Nome;
        loja.Email = req.Email;
        loja.Cnpj = req.Cnpj;
        loja.Cpf = req.Cpf;
        loja.Telefone = req.Telefone;
        loja.Endereco = req.Endereco;
        loja.CorPrimaria = req.CorPrimaria;
        loja.LogoUrl = req.LogoUrl;
        loja.MensalidadeDia = req.MensalidadeDia;
        loja.MensalidadeValor = req.MensalidadeValor;
        if (req.TipoPlano is not null) loja.TipoPlano = req.TipoPlano;
        if (req.ModulosAtivos is not null) loja.ModulosAtivos = req.ModulosAtivos;
        if (req.EhTeste is not null) loja.EhTeste = req.EhTeste.Value;
        loja.AtualizadoEm = DateTime.UtcNow;

        await repositorio.SalvarAsync();
        return Resultado.Ok(PlataformaApoio.ParaLojaDto(loja, DateTime.UtcNow));
    }
}
