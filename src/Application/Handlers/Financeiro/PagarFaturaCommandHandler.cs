using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Paga (ou desfaz o pagamento de) a fatura de um cartão num mês. Modos:
// "total": paga o restante (compras do ciclo - adiantamentos) mais as parcelas de financiamento antigo que vencem no mês;
// "parcial": paga parte e lança o saldo rotativo com juros na próxima fatura;
// "parcelado": financia a fatura (com entrada opcional) em parcelas a pagar, na conta do cartão;
// "desfazer": volta a fatura para pendente e reabre/apaga o que o pagamento gerou.
public class PagarFaturaCommandHandler(IFaturasRepository faturas, ICartoesRepository cartoes, ILancamentosRepository lancamentos, IFinanceiroRepository financeiro) : ICommandHandler<PagarFaturaCommand>
{
    public async Task<Resultado<object>> HandleAsync(PagarFaturaCommand command)
    {
        var (lojaIdOuNulo, cartaoId, ano, mes, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var cartao = await cartoes.ObterCartaoAsync(cartaoId, lojaId);
        if (cartao is null) return Resultado.NaoEncontrado();

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
        }
        fatura.Total = total;

        var totalAntecipado = await faturas.SomarAntecipadosAsync(fatura.Id);
        var totalDevido = total - totalAntecipado;

        // Data real do pagamento informada pelo usuário — se não vier, usa o momento atual.
        var dataPagamento = req.DataPagamento.HasValue ? LancamentosRegras.AoMeioDia(req.DataPagamento.Value) : DateTime.UtcNow;

        if (req.Modo != "desfazer" && req.ContaBancariaId.HasValue)
        {
            var conta = await financeiro.ObterContaAsync(req.ContaBancariaId.Value, lojaId);
            if (conta is null) return Resultado.Invalido("Conta bancária inválida.");
            fatura.ContaBancariaId = req.ContaBancariaId.Value;
        }

        if (req.Modo != "desfazer")
            fatura.NaoAfetaSaldo = req.NaoAfetaSaldo;

        switch (req.Modo)
        {
            case "desfazer":
                await DesfazerAsync(fatura, cartaoId, mesReferencia);
                break;

            case "total":
                await PagarTotalAsync(fatura, cartaoId, mesReferencia, totalDevido, dataPagamento, req);
                break;

            case "parcial":
            {
                var erro = await PagarParcialAsync(fatura, cartao, lojaId, mesReferencia, totalDevido, dataPagamento, req);
                if (erro is not null) return Resultado.Invalido(erro);
                break;
            }

            case "parcelado":
            {
                var erro = await FinanciarAsync(fatura, cartao, lojaId, mesReferencia, totalDevido, dataPagamento, req);
                if (erro is not null) return Resultado.Invalido(erro);
                break;
            }

            default:
                return Resultado.Invalido("Modo inválido.");
        }

        await faturas.SalvarAsync();
        return Resultado.Ok(new { fatura.Id, fatura.Status, fatura.Total, fatura.ValorPago });
    }

    private async Task DesfazerAsync(FaturaCartao fatura, Guid cartaoId, DateTime mesReferencia)
    {
        fatura.ContaBancariaId = null;
        fatura.NaoAfetaSaldo = false;

        // Se era um financiamento, remove todas as parcelas geradas (só as ainda não pagas, por segurança)
        if (fatura.Status == "financiada")
            lancamentos.Remover(await faturas.ListarParcelasPendentesDaFaturaAsync(fatura.Id));

        // Se estava paga junto com parcela(s) de refinanciamento deste ciclo, desfaz também
        foreach (var p in await faturas.ListarParcelasPagasDoMesAsync(cartaoId, mesReferencia.Year, mesReferencia.Month))
        {
            p.Status = "pendente";
            p.PagoEm = null;
            // Se tinha sido absorvida por um pagamento parcial, volta a descontar da conta normalmente
            if (FaturaRegras.FoiAbsorvidaNoPagamentoParcial(p.Descricao))
            {
                p.NaoAfetaSaldo = false;
                p.Descricao = FaturaRegras.RemoverSufixoDeAbsorcao(p.Descricao);
            }
        }

        fatura.Status = "pendente";
        fatura.ValorPago = 0;
        fatura.PagoEm = null;
    }

    // As parcelas de financiamento antigo que vencem neste mês NÃO estão em "total" (só soma compras do cartão): somam aqui
    private async Task PagarTotalAsync(FaturaCartao fatura, Guid cartaoId, DateTime mesReferencia, decimal totalDevido, DateTime dataPagamento, PagarFaturaRequest req)
    {
        var parcelasDoMes = await faturas.ListarParcelasPendentesDoMesAsync(cartaoId, mesReferencia.Year, mesReferencia.Month);

        fatura.Status = "pago";
        fatura.ValorPago = totalDevido + parcelasDoMes.Sum(p => p.Valor);
        fatura.PagoEm = dataPagamento;

        foreach (var p in parcelasDoMes)
        {
            p.Status = "pago";
            p.PagoEm = dataPagamento;
            if (req.NaoAfetaSaldo) p.NaoAfetaSaldo = true;
        }
    }

    private async Task<string?> PagarParcialAsync(FaturaCartao fatura, CartaoCredito cartao, Guid lojaId, DateTime mesReferencia, decimal totalDevido, DateTime dataPagamento, PagarFaturaRequest req)
    {
        // Parcelas de financiamento antigo que vencem neste mês fazem parte do total da fatura (a tela mostra o total já com elas):
        // entram no valor devido e saem de Contas a Pagar.
        var parcelasAntigas = await faturas.ListarParcelasPendentesDoMesAsync(cartao.Id, mesReferencia.Year, mesReferencia.Month);
        var totalDevidoComParcelas = totalDevido + parcelasAntigas.Sum(p => p.Valor);

        var pago = req.ValorPago ?? 0;
        if (!FaturaRegras.PagamentoParcialValido(pago, totalDevidoComParcelas))
            return "Valor parcial deve ser maior que zero e menor que o total (já descontado o que foi antecipado).";

        foreach (var antiga in parcelasAntigas)
            FaturasApoio.AbsorverParcela(antiga, dataPagamento, "pagamento parcial");

        var comJuros = FaturaRegras.SaldoRotativoComJuros(totalDevidoComParcelas - pago, cartao.TaxaJurosMensal);

        // Lança o saldo devedor + juros como um item na PRÓXIMA fatura
        var proximo = mesReferencia.AddMonths(1);
        var proximoVencimento = CartaoRegras.VencimentoDaFatura(cartao.DiaVencimento, proximo.Year, proximo.Month);
        var (proxInicio, _) = CartaoRegras.CicloDaFatura(cartao.DiaFechamento, proximoVencimento);

        cartoes.Adicionar(new LancamentoCartao
        {
            LojaId = lojaId,
            CartaoCreditoId = cartao.Id,
            Descricao = $"Saldo rotativo (fatura {FaturaRegras.AbreviacaoDoMes(mesReferencia.Month)}) + juros",
            Valor = comJuros,
            DataCompra = proxInicio,
            EhJurosRotativo = true,
        });

        fatura.Status = "parcial";
        fatura.ValorPago = pago;
        fatura.PagoEm = dataPagamento;
        return null;
    }

    private async Task<string?> FinanciarAsync(FaturaCartao fatura, CartaoCredito cartao, Guid lojaId, DateTime mesReferencia, decimal totalDevido, DateTime dataPagamento, PagarFaturaRequest req)
    {
        var parcelas = req.TotalParcelas ?? 0;
        if (!FaturaRegras.TotalDeParcelasDoFinanciamentoValido(parcelas))
            return "Escolha entre 2 e 24 parcelas.";

        // Parcelas de financiamento antigo que vencem neste mesmo mês entram no valor financiado agora
        // ("totalDevido" só considera as compras do cartão)
        var parcelasAntigas = await faturas.ListarParcelasPendentesDoMesAsync(cartao.Id, mesReferencia.Year, mesReferencia.Month);
        var totalParaFinanciar = totalDevido + parcelasAntigas.Sum(p => p.Valor);

        var entrada = req.ValorEntrada ?? 0;
        if (!FaturaRegras.EntradaValida(entrada, totalParaFinanciar))
            return "O valor de entrada deve ser maior ou igual a zero e menor que o total (já descontado o que foi antecipado).";

        var valorParcela = FaturaRegras.ValorDaParcelaFinanciada(totalParaFinanciar - entrada, cartao.TaxaJurosMensal, parcelas);
        var grupoId = Guid.NewGuid();

        // A 1ª parcela sempre cai na data de vencimento REAL do cartão (assim some na fatura certa em vez de virar linha separada).
        // Se o usuário escolheu uma data, usa só o mês/ano dela; o dia vem do vencimento do cartão. Sem data, avança até um ciclo que ainda não fechou.
        var mesInicioParcelas = req.PrimeiraParcela.HasValue
            ? new DateTime(req.PrimeiraParcela.Value.Year, req.PrimeiraParcela.Value.Month, 1)
            : mesReferencia.AddMonths(1);
        if (!req.PrimeiraParcela.HasValue)
            mesInicioParcelas = FaturaRegras.PrimeiroMesDasParcelas(cartao.DiaFechamento, cartao.DiaVencimento, mesInicioParcelas, DateTime.UtcNow.Date);

        // Encerra a(s) parcela(s) antiga(s) já somada(s) no valor financiado, senão continuam contando dobrado em "A Pagar"
        foreach (var antiga in parcelasAntigas)
        {
            antiga.Status = "pago";
            antiga.PagoEm = dataPagamento;
            antiga.Descricao = FaturaRegras.DescricaoDaParcelaRefinanciada(antiga.Descricao, dataPagamento);
        }

        for (int i = 0; i < parcelas; i++)
        {
            var mesParcela = mesInicioParcelas.AddMonths(i);
            lancamentos.Adicionar(new LancamentoFinanceiro
            {
                LojaId = lojaId,
                ContaBancariaId = cartao.ContaBancariaId,
                Tipo = "pagar",
                Modo = "parcelada",
                Descricao = $"Financiamento fatura {cartao.Nome} ({FaturaRegras.AbreviacaoDoMes(mesReferencia.Month)})",
                Valor = valorParcela,
                Vencimento = CartaoRegras.VencimentoDaFatura(cartao.DiaVencimento, mesParcela.Year, mesParcela.Month),
                GrupoParcelamentoId = grupoId,
                NumeroParcela = i + 1,
                TotalParcelas = parcelas,
                FaturaCartaoId = fatura.Id,
                CartaoOrigemId = cartao.Id,
            });
        }

        fatura.Status = "financiada";
        fatura.ValorPago = entrada;
        fatura.PagoEm = dataPagamento;
        return null;
    }
}
