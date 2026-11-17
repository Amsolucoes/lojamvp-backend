using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Paga várias contas a pagar de uma vez.
// Forma "conta": paga no débito/dinheiro, saindo da conta bancária escolhida.
// Forma "cartao": vira compra no cartão de crédito (à vista ou parcelada), mantendo descrição, categoria e observação;
// a conta sai de Contas a Pagar e passa a compor a fatura do cartão.
// Divisoes (só forma "conta", uma conta avulsa): o pagamento sai de mais de uma conta bancária; a soma tem de fechar com o valor da conta.
// Acrescimo (juros/multa/taxa pagos a mais): vira um lançamento à parte na categoria "Juros" (conta) ou uma compra à vista no mesmo cartão.
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

        if (req.Acrescimo < 0) return Resultado.Invalido("O juros/multa não pode ser negativo.");
        var acrescimo = Math.Round(req.Acrescimo, 2);

        if (!LancamentosRegras.TentarNormalizarFormaPagamento(req.FormaPagamento, out var formaPagamento))
            return Resultado.Invalido("Forma de pagamento inválida. Use pix, boleto, débito, dinheiro, transferência ou outro.");
        if (req.Forma != "conta") formaPagamento = null; // no cartão a forma já é o crédito

        var agora = DateTime.UtcNow;
        var dataBase = LancamentosRegras.AoMeioDia(req.Data ?? agora);
        var total = lancs.Sum(l => l.Valor);

        var divisoes = req.Divisoes ?? [];
        if (divisoes.Count > 0 && req.Forma != "conta") return Resultado.Invalido("A divisão do pagamento só vale para pagar pela conta.");

        if (req.Forma == "conta" && divisoes.Count > 0)
        {
            if (divisoes.Count < 2) return Resultado.Invalido("Para dividir, informe ao menos duas contas.");
            if (lancs.Count != 1 || lancs[0].Modo != "avulsa") return Resultado.Invalido("Só é possível dividir o pagamento de uma conta avulsa por vez.");
            var original = lancs[0];

            var partes = new List<(Guid ContaId, decimal Valor)>();
            foreach (var d in divisoes)
            {
                var c = d.ContaBancariaId is { } id ? await financeiro.ObterContaAsync(id, lojaId) : null;
                if (c is null) return Resultado.Invalido("Escolha a conta de cada parte do pagamento.");
                var valor = Math.Round(d.Valor, 2);
                if (valor <= 0) return Resultado.Invalido("O valor de cada parte deve ser maior que zero.");
                partes.Add((c.Id, valor));
            }
            if (partes.Sum(x => x.Valor) != original.Valor)
                return Resultado.Invalido($"A soma das partes deve ser igual ao valor da conta ({original.Valor:0.00}).");

            var pagoEm = req.Data.HasValue ? dataBase : agora;
            var n = partes.Count;
            var descricaoBase = original.Descricao;
            var descricaoJuros = DescricaoDoAcrescimo(lancs);
            string Parte(int i)
            {
                var texto = $"{descricaoBase} (parte {i}/{n})";
                return texto.Length <= 150 ? texto : texto[..150];
            }

            original.Descricao = Parte(1);
            original.Valor = partes[0].Valor;
            original.ContaBancariaId = partes[0].ContaId;
            original.Status = "pago";
            original.PagoEm = pagoEm;
            original.FormaPagamento = formaPagamento;

            for (int i = 1; i < n; i++)
            {
                repositorio.Adicionar(new LancamentoFinanceiro
                {
                    LojaId = lojaId,
                    ContaBancariaId = partes[i].ContaId,
                    Tipo = original.Tipo,
                    Modo = "avulsa",
                    Descricao = Parte(i + 1),
                    CategoriaId = original.CategoriaId,
                    Valor = partes[i].Valor,
                    Vencimento = original.Vencimento,
                    Status = "pago",
                    PagoEm = pagoEm,
                    FormaPagamento = formaPagamento,
                    Observacao = original.Observacao,
                });
            }

            if (acrescimo > 0)
            {
                repositorio.Adicionar(new LancamentoFinanceiro
                {
                    LojaId = lojaId,
                    ContaBancariaId = partes[0].ContaId,
                    Tipo = "pagar",
                    Modo = "avulsa",
                    Descricao = descricaoJuros,
                    CategoriaId = (await ObterOuCriarCategoriaJurosAsync(lojaId)).Id,
                    Valor = acrescimo,
                    Vencimento = dataBase,
                    Status = "pago",
                    PagoEm = pagoEm,
                    FormaPagamento = formaPagamento,
                });
            }
        }
        else if (req.Forma == "conta")
        {
            var conta = req.ContaBancariaId is { } contaId ? await financeiro.ObterContaAsync(contaId, lojaId) : null;
            if (conta is null) return Resultado.Invalido("Escolha a conta de onde sai o pagamento.");

            foreach (var l in lancs)
            {
                l.ContaBancariaId = conta.Id;
                l.Status = "pago";
                l.PagoEm = req.Data.HasValue ? dataBase : agora;
                l.FormaPagamento = formaPagamento;
            }

            if (acrescimo > 0)
            {
                repositorio.Adicionar(new LancamentoFinanceiro
                {
                    LojaId = lojaId,
                    ContaBancariaId = conta.Id,
                    Tipo = "pagar",
                    Modo = "avulsa",
                    Descricao = DescricaoDoAcrescimo(lancs),
                    CategoriaId = (await ObterOuCriarCategoriaJurosAsync(lojaId)).Id,
                    Valor = acrescimo,
                    Vencimento = dataBase,
                    Status = "pago",
                    PagoEm = req.Data.HasValue ? dataBase : agora,
                    FormaPagamento = formaPagamento,
                });
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

            if (acrescimo > 0)
            {
                repositorio.Adicionar(new LancamentoCartao
                {
                    LojaId = lojaId,
                    CartaoCreditoId = cartao.Id,
                    Descricao = DescricaoDoAcrescimo(lancs),
                    Valor = acrescimo,
                    DataCompra = dataBase,
                    Modo = "avulsa",
                    CategoriaId = (await ObterOuCriarCategoriaJurosAsync(lojaId)).Id,
                });
            }
        }
        else
        {
            return Resultado.Invalido("Forma de pagamento inválida.");
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { pagos = lancs.Count, total, forma = req.Forma });
    }

    private static string DescricaoDoAcrescimo(List<LancamentoFinanceiro> lancs)
    {
        var texto = lancs.Count == 1 ? $"Juros/multa - {lancs[0].Descricao}" : $"Juros/multa - {lancs.Count} contas";
        return texto.Length <= 150 ? texto : texto[..150];
    }

    // Usa a categoria "Juros" que a loja já tenha (de despesa, ou "ambos"); se não houver, cria
    private async Task<CategoriaFinanceira> ObterOuCriarCategoriaJurosAsync(Guid lojaId)
    {
        var nome = FinanceiroRegras.NormalizarNomeCategoria(NomeCategoriaJuros);
        var existente = (await financeiro.ListarCategoriasDaLojaAsync(lojaId))
            .FirstOrDefault(c => FinanceiroRegras.CategoriaEquivalente(c.Nome, c.Tipo, nome, "pagar"));
        if (existente is not null)
        {
            if (existente.Ativa) return existente;
            var reativada = await financeiro.ObterCategoriaAsync(existente.Id, lojaId);
            if (reativada is not null)
            {
                reativada.Ativa = true;
                return reativada;
            }
        }

        var nova = new CategoriaFinanceira { LojaId = lojaId, Nome = NomeCategoriaJuros, Tipo = "pagar", Icone = "💸" };
        financeiro.Adicionar(nova);
        return nova;
    }

    private const string NomeCategoriaJuros = "Juros";
}
