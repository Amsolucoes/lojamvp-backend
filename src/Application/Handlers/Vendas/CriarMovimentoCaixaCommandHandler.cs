using LojaApi.Domain.Vendas;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Lança um reforço ou sangria no caixa; se escolher uma conta bancária, espelha como ajuste de saldo.
public class CriarMovimentoCaixaCommandHandler(IVendasRepository repositorio) : ICommandHandler<CriarMovimentoCaixaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarMovimentoCaixaCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (!VendasRegras.TipoDeMovimentoValido(req.Tipo)) return Resultado.Invalido("Tipo inválido.");
        if (req.Valor <= 0) return Resultado.Invalido("Informe um valor maior que zero.");
        if (req.Data.Date > DateTime.UtcNow.Date) return Resultado.Invalido("A data não pode ser no futuro.");

        string? origemNome = null;
        if (req.OrigemVendaId.HasValue)
        {
            var origem = await repositorio.ObterOrigemAsync(req.OrigemVendaId.Value, lojaId);
            origemNome = origem?.Nome;
        }

        var dataUtc = VendasRegras.DataDoMovimentoUtc(req.Data);

        var movimento = new MovimentoCaixa
        {
            LojaId = lojaId.Value,
            Tipo = req.Tipo,
            Valor = req.Valor,
            Data = dataUtc,
            OrigemVendaId = req.OrigemVendaId,
            OrigemNome = origemNome,
            Observacao = req.Observacao,
            ContaBancariaId = req.ContaBancariaId,
        };

        // Se a loja tem Financeiro ativo e uma conta foi escolhida, espelha como ajuste de saldo
        if (req.ContaBancariaId.HasValue)
        {
            var conta = await repositorio.ObterContaBancariaAsync(req.ContaBancariaId.Value, lojaId);
            if (conta != null)
            {
                var ajuste = new AjusteContaBancaria
                {
                    LojaId = lojaId.Value,
                    ContaBancariaId = conta.Id,
                    Tipo = req.Tipo, // "entrada" ou "saida", mesmo vocabulário
                    Valor = req.Valor,
                    Observacao = VendasRegras.DescricaoDoAjuste(req.Tipo, origemNome, req.Observacao),
                    CriadoEm = dataUtc,
                };
                repositorio.Adicionar(ajuste);
                await repositorio.SalvarAsync();
                movimento.AjusteContaBancariaId = ajuste.Id;
            }
        }

        repositorio.Adicionar(movimento);
        await repositorio.SalvarAsync();

        return Resultado.Ok(movimento);
    }
}
