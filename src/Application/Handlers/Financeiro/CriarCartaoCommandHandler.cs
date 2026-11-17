using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cadastra um cartão de crédito da loja (a bandeira só vale se for uma das conhecidas).
public class CriarCartaoCommandHandler(ICartoesRepository repositorio) : ICommandHandler<CriarCartaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarCartaoCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var cartao = new CartaoCredito
        {
            LojaId = lojaId,
            Nome = req.Nome.Trim(),
            Limite = req.Limite,
            DiaFechamento = req.DiaFechamento,
            DiaVencimento = req.DiaVencimento,
            ContaBancariaId = req.ContaBancariaId,
            TaxaJurosMensal = req.TaxaJurosMensal,
            Bandeira = CartaoRegras.NormalizarBandeira(req.Bandeira),
        };
        repositorio.Adicionar(cartao);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { cartao.Id });
    }
}
