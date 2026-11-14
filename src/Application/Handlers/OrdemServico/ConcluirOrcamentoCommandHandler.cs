using LojaApi.Domain.OrdemServico;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.OrdemServico;

// Conclui a ordem: baixa o estoque das peças, gera o lançamento a receber no Financeiro e a comissão de cada mecânico.
// Tudo é gravado junto no fim (ou tudo, ou nada).
public class ConcluirOrcamentoCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<ConcluirOrcamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ConcluirOrcamentoCommand command)
    {
        var (lojaId, id, req) = command;
        var orcamento = await repositorio.ObterOrcamentoParaConclusaoAsync(id, lojaId);
        if (orcamento is null) return Resultado.NaoEncontrado();

        if (!OrdemServicoRegras.PodeConcluir(orcamento.Status))
            return Resultado.Invalido("Só é possível concluir uma ordem em andamento (aprovada).");

        var conta = await repositorio.ObterContaBancariaAsync(req.ContaBancariaId, lojaId);
        if (conta is null) return Resultado.Invalido("Conta bancária não encontrada.");

        // ── Valida o estoque das peças que vieram do estoque ──
        var pecasDoEstoque = orcamento.Itens.Where(i => i.Tipo == OrdemServicoRegras.TipoPeca && i.ProdutoId.HasValue).ToList();
        var produtos = new Dictionary<Guid, Produto>();
        foreach (var item in pecasDoEstoque)
        {
            var produto = await repositorio.ObterProdutoAsync(item.ProdutoId!.Value, lojaId);
            if (produto is null) return Resultado.Invalido($"Produto do item '{item.Descricao}' não encontrado.");
            if (produto.Estoque < item.Quantidade)
                return Resultado.Invalido($"Estoque insuficiente para '{produto.Nome}' — disponível: {produto.Estoque:0.###}.");
            produtos[item.ProdutoId!.Value] = produto;
        }

        // Antes de mexer em qualquer coisa: se já pagou, precisa dizer como
        if (req.JaPago && string.IsNullOrWhiteSpace(req.FormaPagamento))
            return Resultado.Invalido("Informe a forma de pagamento.");

        // ── Baixa o estoque ──
        foreach (var item in pecasDoEstoque)
        {
            var produto = produtos[item.ProdutoId!.Value];
            produto.Estoque -= item.Quantidade;
            produto.AtualizadoEm = DateTime.UtcNow;

            repositorio.Adicionar(new MovimentoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = "saida",
                Quantidade = item.Quantidade,
                Observacao = $"Ordem de Serviço #{orcamento.Id.ToString()[..8]} - {item.Descricao}",
                LojaId = lojaId,
            });
        }

        // ── Gera o lançamento a receber no Financeiro ──
        var categoria = await repositorio.ObterCategoriaFinanceiraAsync(lojaId!.Value, "Ordem de Serviço");
        if (categoria is null)
        {
            categoria = new CategoriaFinanceira
            {
                LojaId = lojaId.Value,
                Nome = "Ordem de Serviço",
                Tipo = "receber",
                Icone = "🔧",
            };
            repositorio.Adicionar(categoria);
        }

        // Guarda a forma de pagamento (e parcelas, se for cartão) como observação —
        // o LancamentoFinanceiro não tem um campo próprio pra isso hoje, diferente
        // da Venda do Caixa.
        var lancamento = new LancamentoFinanceiro
        {
            LojaId = lojaId.Value,
            ContaBancariaId = conta.Id,
            Tipo = "receber",
            Modo = "avulsa",
            Descricao = OrdemServicoRegras.DescricaoDoLancamento(orcamento.VeiculoDescricao, orcamento.Id),
            CategoriaId = categoria.Id,
            Valor = orcamento.ValorTotal,
            Vencimento = OrdemServicoRegras.VencimentoDoLancamento(req.Vencimento, DateTime.UtcNow),
            Status = req.JaPago ? "pago" : "pendente",
            PagoEm = req.JaPago ? DateTime.UtcNow : null,
            Observacao = OrdemServicoRegras.ObservacaoDoPagamento(req.JaPago, req.FormaPagamento, req.Parcelas),
        };
        repositorio.Adicionar(lancamento);

        orcamento.LancamentoFinanceiroId = lancamento.Id;

        // ── Gera a comissão de cada mecânico vinculado ──
        var itensComissionaveis = orcamento.Itens.Select(i => new ItemComissionavel(i.Tipo, i.ServicoId, i.ValorTotal)).ToList();
        var idsServicoUsados = orcamento.Itens
            .Where(i => i.Tipo == OrdemServicoRegras.TipoServico && i.ServicoId.HasValue)
            .Select(i => i.ServicoId!.Value)
            .Distinct()
            .ToList();
        var idsMecanicos = orcamento.Mecanicos.Select(m => m.ProfissionalId).ToList();

        var excecoes = idsServicoUsados.Count == 0
            ? new List<ComissaoServicoProfissional>()
            : await repositorio.ListarExcecoesDeComissaoAsync(idsServicoUsados, idsMecanicos);

        foreach (var mecanico in orcamento.Mecanicos)
        {
            var comissao = OrdemServicoRegras.CalcularComissao(
                itensComissionaveis,
                mecanico.Profissional?.ComissaoBaseCalculo,
                mecanico.ComissaoPercentual,
                servicoId => excecoes.FirstOrDefault(e => e.ProfissionalId == mecanico.ProfissionalId && e.ServicoId == servicoId)?.ComissaoPercentual);
            if (comissao is null) continue;

            repositorio.Adicionar(new ComissaoFuncionario
            {
                LojaId = lojaId.Value,
                ProfissionalId = mecanico.ProfissionalId,
                OrigemTipo = "ordem_servico",
                OrigemId = orcamento.Id,
                ValorServico = comissao.ValorBase,
                ComissaoPercentual = comissao.PercentualEfetivo,
                ValorComissao = comissao.Valor,
            });
        }

        orcamento.Status = OrdemServicoRegras.Concluido;
        orcamento.ConcluidoEm = DateTime.UtcNow;

        // Uma única gravação no final — ou tudo (baixa de estoque + financeiro + comissão
        // + status) é salvo junto, ou nada é, evitando ficar com efeito parcial se algo falhar no meio.
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { orcamento.Id, orcamento.Status, orcamento.LancamentoFinanceiroId });
    }
}
