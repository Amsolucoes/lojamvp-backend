using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Envia o orçamento por e-mail para o cliente.
public class EnviarOrcamentoPorEmailCommandHandler(IOrcamentoNotificacao notificacao) : ICommandHandler<EnviarOrcamentoPorEmailCommand>
{
    public async Task<Resultado<object>> HandleAsync(EnviarOrcamentoPorEmailCommand command)
    {
        var (lojaId, id) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var resultado = await notificacao.EnviarPorEmailAsync(id, lojaId.Value);
        if (!resultado.Enviado)
            return Resultado.Invalido(resultado.Erro!);

        return Resultado.Ok(new { mensagem = "Orçamento enviado por e-mail." });
    }
}
