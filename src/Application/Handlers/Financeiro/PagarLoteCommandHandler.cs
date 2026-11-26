using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Paga várias contas a pagar de uma vez. As regras (validações, descrições, parcelas) estão em PagamentoEmLoteRegras (Domain); aqui só se busca e grava.
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

        var erroAcrescimo = PagamentoEmLoteRegras.ValidarAcrescimo(req.Acrescimo);
        if (erroAcrescimo is not null) return Resultado.Invalido(erroAcrescimo);
        var acrescimo = PagamentoEmLoteRegras.ArredondarValor(req.Acrescimo);

        if (!LancamentosRegras.TentarNormalizarFormaPagamento(req.FormaPagamento, out var formaPagamento))
            return Resultado.Invalido("Forma de pagamento inválida. Use pix, boleto, débito, dinheiro, transferência ou outro.");
        formaPagamento = PagamentoEmLoteRegras.FormaDePagamentoGravada(req.Forma, formaPagamento);

        var agora = DateTime.UtcNow;
        var dataBase = PagamentoEmLoteRegras.DataBase(req.Data, agora);
        var pagoEm = PagamentoEmLoteRegras.PagoEm(req.Data, agora);
        var total = lancs.Sum(l => l.Valor);
        var descricoes = lancs.Select(l => l.Descricao).ToList();

        var divisoes = req.Divisoes ?? [];
        var erroDivisao = PagamentoEmLoteRegras.ValidarDivisao(req.Forma, divisoes.Count, lancs.Count, lancs.FirstOrDefault()?.Modo);
        if (erroDivisao is not null) return Resultado.Invalido(erroDivisao);

        if (req.Forma == PagamentoEmLoteRegras.FormaConta && divisoes.Count > 0)
        {
            var original = lancs[0];

            var partes = new List<(Guid ContaId, decimal Valor)>();
            foreach (var d in divisoes)
            {
                var c = d.ContaBancariaId is { } id ? await financeiro.ObterContaAsync(id, lojaId) : null;
                if (c is null) return Resultado.Invalido("Escolha a conta de cada parte do pagamento.");
                var valor = PagamentoEmLoteRegras.ArredondarValor(d.Valor);
                var erroParte = PagamentoEmLoteRegras.ValidarValorDaParte(valor);
                if (erroParte is not null) return Resultado.Invalido(erroParte);
                partes.Add((c.Id, valor));
            }
            var erroSoma = PagamentoEmLoteRegras.ValidarSomaDasPartes(partes.Select(x => x.Valor), original.Valor);
            if (erroSoma is not null) return Resultado.Invalido(erroSoma);

            var n = partes.Count;
            var descricaoBase = original.Descricao;
            var descricaoJuros = PagamentoEmLoteRegras.DescricaoDoAcrescimo(descricoes);

            original.Descricao = PagamentoEmLoteRegras.DescricaoDaParte(descricaoBase, 1, n);
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
                    Modo = PagamentoEmLoteRegras.ModoAvulsa,
                    Descricao = PagamentoEmLoteRegras.DescricaoDaParte(descricaoBase, i + 1, n),
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
                repositorio.Adicionar(await AcrescimoPagoNaContaAsync(lojaId, partes[0].ContaId, descricaoJuros, acrescimo, dataBase, pagoEm, formaPagamento));
        }
        else if (req.Forma == PagamentoEmLoteRegras.FormaConta)
        {
            var conta = req.ContaBancariaId is { } contaId ? await financeiro.ObterContaAsync(contaId, lojaId) : null;
            if (conta is null) return Resultado.Invalido("Escolha a conta de onde sai o pagamento.");

            foreach (var l in lancs)
            {
                l.ContaBancariaId = conta.Id;
                l.Status = "pago";
                l.PagoEm = pagoEm;
                l.FormaPagamento = formaPagamento;
            }

            if (acrescimo > 0)
                repositorio.Adicionar(await AcrescimoPagoNaContaAsync(lojaId, conta.Id, PagamentoEmLoteRegras.DescricaoDoAcrescimo(descricoes), acrescimo, dataBase, pagoEm, formaPagamento));
        }
        else if (req.Forma == PagamentoEmLoteRegras.FormaCartao)
        {
            var cartao = req.CartaoId is { } cartaoId ? await repositorio.ObterCartaoAsync(cartaoId, lojaId) : null;
            if (cartao is null) return Resultado.Invalido("Escolha o cartão de crédito.");

            var parcelas = req.Parcelas;
            var erroParcelas = PagamentoEmLoteRegras.ValidarParcelasNoCartao(parcelas);
            if (erroParcelas is not null) return Resultado.Invalido(erroParcelas);

            foreach (var l in lancs)
            {
                var grupoId = parcelas > 1 ? Guid.NewGuid() : (Guid?)null;
                foreach (var p in PagamentoEmLoteRegras.ParcelasDoCartao(l.Descricao, l.Valor, parcelas, dataBase))
                {
                    repositorio.Adicionar(new LancamentoCartao
                    {
                        LojaId = lojaId,
                        CartaoCreditoId = cartao.Id,
                        Descricao = p.Descricao,
                        Valor = p.Valor,
                        DataCompra = p.DataCompra,
                        Modo = p.Parcelada ? "parcelada" : PagamentoEmLoteRegras.ModoAvulsa,
                        GrupoParcelamentoId = grupoId,
                        NumeroParcela = p.NumeroParcela,
                        TotalParcelas = p.TotalParcelas,
                        CategoriaId = l.CategoriaId,
                        Observacao = l.Observacao,
                    });
                }
                repositorio.Remover(l);
            }

            if (acrescimo > 0)
            {
                repositorio.Adicionar(new LancamentoCartao
                {
                    LojaId = lojaId,
                    CartaoCreditoId = cartao.Id,
                    Descricao = PagamentoEmLoteRegras.DescricaoDoAcrescimo(descricoes),
                    Valor = acrescimo,
                    DataCompra = dataBase,
                    Modo = PagamentoEmLoteRegras.ModoAvulsa,
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

    // Juros/multa pagos pela conta: um lançamento à parte, já pago, na categoria "Juros"
    private async Task<LancamentoFinanceiro> AcrescimoPagoNaContaAsync(Guid lojaId, Guid contaId, string descricao, decimal valor, DateTime dataBase, DateTime pagoEm, string? formaPagamento) => new()
    {
        LojaId = lojaId,
        ContaBancariaId = contaId,
        Tipo = "pagar",
        Modo = PagamentoEmLoteRegras.ModoAvulsa,
        Descricao = descricao,
        CategoriaId = (await ObterOuCriarCategoriaJurosAsync(lojaId)).Id,
        Valor = valor,
        Vencimento = dataBase,
        Status = "pago",
        PagoEm = pagoEm,
        FormaPagamento = formaPagamento,
    };

    // Usa a categoria "Juros" que a loja já tenha (de despesa, ou "ambos"); se não houver, cria
    private async Task<CategoriaFinanceira> ObterOuCriarCategoriaJurosAsync(Guid lojaId)
    {
        var nome = FinanceiroRegras.NormalizarNomeCategoria(PagamentoEmLoteRegras.NomeCategoriaJuros);
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

        var nova = new CategoriaFinanceira { LojaId = lojaId, Nome = PagamentoEmLoteRegras.NomeCategoriaJuros, Tipo = "pagar", Icone = "💸" };
        financeiro.Adicionar(nova);
        return nova;
    }
}
