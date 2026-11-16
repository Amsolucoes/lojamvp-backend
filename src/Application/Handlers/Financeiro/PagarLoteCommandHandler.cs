using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Paga várias contas a pagar de uma vez.
// Forma "conta": paga no débito/dinheiro, saindo da conta bancária escolhida.
// Forma "cartao": vira compra no cartão de crédito (à vista ou parcelada), mantendo descrição, categoria e observação;
// a conta sai de Contas a Pagar e passa a compor a fatura do cartão.
public class PagarLoteCommandHandler(ILancamentosRepository repositorio, IFinanceiroRepository financeiro) : ICommandHandler<PagarLoteCommand>
{
    public async Task<Resultado<object>> HandleAsync(PagarLoteCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var ids = (req.Ids ?? []).Distinct().ToList();
        if (ids.Count == 0) return Resultado.Invalido("Selecione ao menos uma conta.");

        var lancs = await repositorio.ListarPendentesParaPagarEmLoteAsync(ids, lojaId);
        if (lancs.Count != ids.Count)
            return Resultado.Invalido("Alguma conta selecionada não está mais pendente. Atualize a lista e tente de novo.");

        var agora = DateTime.UtcNow;
        var dataBase = LancamentosRegras.AoMeioDia(req.Data ?? agora);
        var total = lancs.Sum(l => l.Valor);

        if (req.Forma == "conta")
        {
            var conta = req.ContaBancariaId is { } contaId ? await financeiro.ObterContaAsync(contaId, lojaId) : null;
            if (conta is null) return Resultado.Invalido("Escolha a conta de onde sai o pagamento.");

            foreach (var l in lancs)
            {
                l.ContaBancariaId = conta.Id;
                l.Status = "pago";
                l.PagoEm = req.Data.HasValue ? dataBase : agora;
            }
        }
        else if (req.Forma == "cartao")
        {
            var cartao = req.CartaoId is { } cartaoId ? await repositorio.ObterCartaoAsync(cartaoId, lojaId) : null;
            if (cartao is null) return Resultado.Invalido("Escolha o cartão de crédito.");

            var parcelas = req.Parcelas;
            if (parcelas < 1 || parcelas > LancamentosRegras.MaxParcelasNoCartao) return Resultado.Invalido("Escolha entre 1 e 24 parcelas.");

            foreach (var l in lancs)
            {
                if (parcelas == 1)
                {
                    repositorio.Adicionar(new LancamentoCartao
                    {
                        LojaId = lojaId,
                        CartaoCreditoId = cartao.Id,
                        Descricao = l.Descricao,
                        Valor = l.Valor,
                        DataCompra = dataBase,
                        Modo = "avulsa",
                        CategoriaId = l.CategoriaId,
                        Observacao = l.Observacao,
                    });
                }
                else
                {
                    var grupoId = Guid.NewGuid();
                    var valores = LancamentosRegras.ValoresDasParcelasDoCartao(l.Valor, parcelas);
                    for (int i = 0; i < parcelas; i++)
                    {
                        repositorio.Adicionar(new LancamentoCartao
                        {
                            LojaId = lojaId,
                            CartaoCreditoId = cartao.Id,
                            Descricao = $"{l.Descricao} ({i + 1}/{parcelas})",
                            Valor = valores[i],
                            DataCompra = dataBase.AddMonths(i),
                            Modo = "parcelada",
                            GrupoParcelamentoId = grupoId,
                            NumeroParcela = i + 1,
                            TotalParcelas = parcelas,
                            CategoriaId = l.CategoriaId,
                            Observacao = l.Observacao,
                        });
                    }
                }
                repositorio.Remover(l);
            }
        }
        else
        {
            return Resultado.Invalido("Forma de pagamento inválida.");
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { pagos = lancs.Count, total, forma = req.Forma });
    }
}
