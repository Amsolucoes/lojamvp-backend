using LojaApi.Domain.Vendas;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Cria as origens padrão (Loja física, Site, WhatsApp) para uma loja que ainda não tem nenhuma.
public class CriarOrigensPadraoCommandHandler(IVendasRepository repositorio) : ICommandHandler<CriarOrigensPadraoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarOrigensPadraoCommand command)
    {
        var lojaId = command.LojaId;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (await repositorio.LojaTemOrigensAsync(lojaId))
            return Resultado.Invalido("Você já tem origens cadastradas.");

        for (int i = 0; i < VendasRegras.OrigensPadrao.Length; i++)
        {
            repositorio.Adicionar(new OrigemVenda
            {
                LojaId = lojaId.Value,
                Nome = VendasRegras.OrigensPadrao[i],
                Ordem = i,
            });
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Origens padrão criadas." });
    }
}
