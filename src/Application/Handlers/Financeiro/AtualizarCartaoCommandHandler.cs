using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Edita os dados de um cartão de crédito da loja.
public class AtualizarCartaoCommandHandler(ICartoesRepository repositorio) : ICommandHandler<AtualizarCartaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarCartaoCommand command)
    {
        var (lojaIdOuNulo, id, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var cartao = await repositorio.ObterCartaoAsync(id, lojaId);
        if (cartao is null) return Resultado.NaoEncontrado();

        cartao.Nome = req.Nome.Trim();
        cartao.Limite = req.Limite;
        cartao.DiaFechamento = req.DiaFechamento;
        cartao.DiaVencimento = req.DiaVencimento;
        cartao.ContaBancariaId = req.ContaBancariaId;
        cartao.TaxaJurosMensal = req.TaxaJurosMensal;
        cartao.Bandeira = CartaoRegras.NormalizarBandeira(req.Bandeira);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { cartao.Id });
    }
}
