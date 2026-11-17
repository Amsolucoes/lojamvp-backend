using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Registra um pagamento antecipado (adiantamento) de uma fatura; cria a fatura do mês se ainda não existir.
public class AdicionarAntecipadoFaturaCommandHandler(IFaturasRepository faturas, ICartoesRepository cartoes, IFinanceiroRepository financeiro) : ICommandHandler<AdicionarAntecipadoFaturaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AdicionarAntecipadoFaturaCommand command)
    {
        var (lojaIdOuNulo, cartaoId, ano, mes, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var cartao = await cartoes.ObterCartaoAsync(cartaoId, lojaId);
        if (cartao is null) return Resultado.NaoEncontrado();

        if (req.Valor <= 0) return Resultado.Invalido("Valor deve ser maior que zero.");
        if (req.Data.Date > DateTime.UtcNow.Date) return Resultado.Invalido("A data não pode ser no futuro.");

        var conta = await financeiro.ObterContaAsync(req.ContaBancariaId, lojaId);
        if (conta is null) return Resultado.Invalido("Conta bancária inválida.");

        var vencimento = CartaoRegras.VencimentoDaFatura(cartao.DiaVencimento, ano, mes);
        var mesReferencia = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var (inicio, fim) = CartaoRegras.CicloDaFatura(cartao.DiaFechamento, vencimento);

        var total = await faturas.SomarComprasDoCicloAsync(cartaoId, inicio, fim);

        var fatura = await faturas.ObterFaturaPorReferenciaAsync(cartaoId, mesReferencia);
        if (fatura is null)
        {
            fatura = new FaturaCartao
            {
                LojaId = lojaId,
                CartaoCreditoId = cartaoId,
                MesReferencia = mesReferencia,
                Vencimento = vencimento,
                Total = total,
            };
            faturas.Adicionar(fatura);
            await faturas.SalvarAsync();
        }

        var jaAntecipado = await faturas.SomarAntecipadosAsync(fatura.Id);
        if (jaAntecipado + req.Valor > total)
            return Resultado.Invalido("O valor adiantado não pode ultrapassar o total da fatura.");

        faturas.Adicionar(new PagamentoAntecipadoFatura
        {
            LojaId = lojaId,
            FaturaCartaoId = fatura.Id,
            ContaBancariaId = req.ContaBancariaId,
            Valor = req.Valor,
            Data = LancamentosRegras.AoMeioDia(req.Data),
            Observacao = req.Observacao,
        });
        await faturas.SalvarAsync();

        return Resultado.Ok(new { mensagem = "Pagamento antecipado registrado." });
    }
}
