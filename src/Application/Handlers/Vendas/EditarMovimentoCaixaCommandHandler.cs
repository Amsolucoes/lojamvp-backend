using LojaApi.Domain.Vendas;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Edita um movimento do caixa e refaz o espelho na conta bancária.
public class EditarMovimentoCaixaCommandHandler(IVendasRepository repositorio) : ICommandHandler<EditarMovimentoCaixaCommand>
{
    public async Task<Resultado<object>> HandleAsync(EditarMovimentoCaixaCommand command)
    {
        var (lojaId, id, req) = command;
        var movimento = lojaId is null ? null : await repositorio.ObterMovimentoCaixaAsync(id, lojaId);
        if (movimento is null) return Resultado.NaoEncontrado();

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

        movimento.Tipo = req.Tipo;
        movimento.Valor = req.Valor;
        movimento.Data = dataUtc;
        movimento.OrigemVendaId = req.OrigemVendaId;
        movimento.OrigemNome = origemNome;
        movimento.Observacao = req.Observacao;

        // Remove o espelho antigo (se existir) e recria do zero — mais simples e seguro
        // do que tentar ajustar o ajuste existente pra cada combinação de mudança possível
        if (movimento.AjusteContaBancariaId.HasValue)
        {
            var ajusteAntigo = await repositorio.ObterAjusteDaContaAsync(movimento.AjusteContaBancariaId.Value);
            if (ajusteAntigo != null) repositorio.Remover(ajusteAntigo);
            movimento.AjusteContaBancariaId = null;
        }

        if (req.ContaBancariaId.HasValue)
        {
            var conta = await repositorio.ObterContaBancariaAsync(req.ContaBancariaId.Value, lojaId);
            if (conta != null)
            {
                var ajuste = new AjusteContaBancaria
                {
                    LojaId = lojaId!.Value,
                    ContaBancariaId = conta.Id,
                    Tipo = req.Tipo,
                    Valor = req.Valor,
                    Observacao = VendasRegras.DescricaoDoAjuste(req.Tipo, origemNome, req.Observacao),
                    CriadoEm = dataUtc,
                };
                repositorio.Adicionar(ajuste);
                await repositorio.SalvarAsync();
                movimento.ContaBancariaId = conta.Id;
                movimento.AjusteContaBancariaId = ajuste.Id;
            }
        }
        else
        {
            movimento.ContaBancariaId = null;
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(movimento);
    }
}
