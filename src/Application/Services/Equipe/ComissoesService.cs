using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Comissões dos funcionários: o que cada um tem a receber, de onde veio, fechar/pagar o período e desfazer.
public class ComissoesService(IEquipeRepository repositorio)
{
    private const string LojaNaoEncontrada = "Loja não encontrada.";

    // ── Resumo por profissional: quanto cada um tem pendente ─────────────────

    public async Task<Resultado<object>> ResumoAsync(Guid? lojaId, DateTime? de, DateTime? ate)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var comissoes = await repositorio.ListarComissoesPendentesAsync(lojaId.Value,
            de?.Date,
            ate?.Date.AddDays(1));
        var profissionalIds = comissoes.Select(c => c.ProfissionalId).Distinct().ToList();
        var profissionais = await repositorio.ListarProfissionaisPorIdsAsync(profissionalIds);

        var resumo = profissionalIds.Select(pid =>
        {
            var doProfissional = comissoes.Where(c => c.ProfissionalId == pid).ToList();
            var prof = profissionais.FirstOrDefault(p => p.Id == pid);
            return new ResumoComissaoProfissionalDto(
                pid,
                prof?.Nome ?? "—",
                prof?.DiaPagamentoPadrao,
                prof?.TipoRemuneracao ?? TipoRemuneracao.Comissao,
                prof?.ValorDiaria,
                doProfissional.Count,
                doProfissional.Sum(c => c.ValorComissao));
        }).OrderByDescending(r => r.ValorTotal).ToList();

        return Resultado.Ok(resumo);
    }

    // ── Detalhe das comissões de um profissional ─────────────────────────────

    public async Task<Resultado<object>> DetalheProfissionalAsync(Guid? lojaId, Guid profissionalId, string status)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var comissoes = await repositorio.ListarComissoesDoProfissionalAsync(lojaId.Value, profissionalId, status);

        // Comissões de Ordem de Serviço e de Venda (Caixa) não têm Agendamento: nome, cliente e itens vêm da origem, buscados em lote
        var orcamentoIds = comissoes.Where(c => c.OrigemTipo == OrigemComissao.OrdemServico).Select(c => c.OrigemId).Distinct().ToList();
        var vendaIds = comissoes.Where(c => c.OrigemTipo == OrigemComissao.Venda).Select(c => c.OrigemId).Distinct().ToList();
        var origens = await repositorio.ObterOrigensDasComissoesAsync(orcamentoIds, vendaIds);

        var lista = comissoes.Select(c => c.OrigemTipo switch
        {
            OrigemComissao.OrdemServico => DeOrdemDeServico(c, origens),
            OrigemComissao.Venda => DeVenda(c, origens),
            _ => DeAgendamento(c),
        }).ToList();

        return Resultado.Ok(lista);
    }

    private static ComissaoDetalheDto DeOrdemDeServico(ComissaoFuncionario c, OrigensDasComissoesDto origens)
    {
        var orcamento = origens.Orcamentos.FirstOrDefault(o => o.Id == c.OrigemId);
        var cliente = orcamento != null ? origens.Clientes.FirstOrDefault(cl => cl.Id == orcamento.ClienteId) : null;

        return new ComissaoDetalheDto(
            c.Id, c.ValorServico, c.ComissaoPercentual, c.ValorComissao, c.Status, c.PagoEm, c.CriadoEm,
            orcamento != null ? EquipeRegras.NomeDaOrdemDeServico(orcamento.VeiculoDescricao, orcamento.Placa) : "Ordem de Serviço",
            cliente?.Nome, orcamento?.ConcluidoEm,
            OrigemComissao.OrdemServico,
            origens.ItensOrdemServico.Where(i => i.OrcamentoId == c.OrigemId)
                .Select(i => new ComissaoItemDto(i.Descricao, i.Quantidade, i.ValorTotal, i.Tipo)).ToList());
    }

    private static ComissaoDetalheDto DeVenda(ComissaoFuncionario c, OrigensDasComissoesDto origens)
    {
        var venda = origens.Vendas.FirstOrDefault(v => v.Id == c.OrigemId);
        var cliente = venda?.ClienteId.HasValue == true ? origens.Clientes.FirstOrDefault(cl => cl.Id == venda.ClienteId) : null;

        return new ComissaoDetalheDto(
            c.Id, c.ValorServico, c.ComissaoPercentual, c.ValorComissao, c.Status, c.PagoEm, c.CriadoEm,
            "Venda", cliente?.Nome, venda?.CriadaEm,
            OrigemComissao.Venda,
            origens.ItensVenda.Where(i => i.VendaId == c.OrigemId)
                .Select(i => new ComissaoItemDto(i.NomeProduto, i.Quantidade, i.Subtotal, i.ServicoId.HasValue ? "servico" : "produto")).ToList());
    }

    private static ComissaoDetalheDto DeAgendamento(ComissaoFuncionario c) => new(
        c.Id, c.ValorServico, c.ComissaoPercentual, c.ValorComissao, c.Status, c.PagoEm, c.CriadoEm,
        c.Agendamento?.NomeServico,
        c.Agendamento?.NomeCliente,
        c.Agendamento?.DataHora,
        OrigemComissao.Agendamento,
        c.Agendamento != null
            ? new List<ComissaoItemDto> { new(c.Agendamento.NomeServico, 1, c.Agendamento.Preco, "servico") }
            : null);

    // ── Fechar e pagar as comissões (+ diárias) de um profissional num período ─

    public async Task<Resultado<object>> FecharAsync(Guid? lojaId, FecharComissaoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var profissional = await repositorio.ObterProfissionalAsync(req.ProfissionalId, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado("Profissional não encontrado.");

        var erro = EquipeRegras.ValidarFechamento(req.QtdDiarias, profissional.ValorDiaria);
        if (erro is not null) return Resultado.Invalido(erro);

        // A conta bancária onde cai o pagamento tem que ser desta mesma loja
        if (req.ContaBancariaId.HasValue && !await repositorio.ContaBancariaDaLojaExisteAsync(req.ContaBancariaId.Value, lojaId.Value))
            return Resultado.Invalido("Conta bancária não encontrada.");

        var inicio = EquipeRegras.InicioDoPeriodo(req.PeriodoInicio);
        var fim = EquipeRegras.FimDoPeriodoExclusivo(req.PeriodoFim);

        var comissoesPendentes = await repositorio.ListarComissoesPendentesDoPeriodoAsync(lojaId.Value, req.ProfissionalId, inicio, fim);

        if (comissoesPendentes.Count == 0 && req.QtdDiarias == 0)
            return Resultado.Invalido("Nenhuma comissão pendente encontrada nesse período, e nenhuma diária informada.");

        var valorComissoes = comissoesPendentes.Sum(c => c.ValorComissao);
        var valorDiarias = EquipeRegras.ValorDasDiarias(req.QtdDiarias, profissional.ValorDiaria);
        var valorTotal = valorComissoes + valorDiarias;

        var fechamento = new FechamentoComissao
        {
            LojaId = lojaId.Value,
            ProfissionalId = profissional.Id,
            PeriodoInicio = inicio,
            PeriodoFim = EquipeRegras.UltimoDiaDoPeriodo(req.PeriodoFim),
            ValorTotal = valorTotal,
            QtdAtendimentos = comissoesPendentes.Count,
            QtdDiarias = req.QtdDiarias,
            ValorDiarias = valorDiarias,
        };
        repositorio.Adicionar(fechamento);
        await repositorio.SalvarAsync();

        foreach (var c in comissoesPendentes)
        {
            c.Status = StatusComissao.Pago;
            c.PagoEm = DateTime.UtcNow;
            c.FechamentoId = fechamento.Id;
        }

        // Integração opcional com o Financeiro: só cria a conta a pagar se veio conta bancária escolhida
        if (req.ContaBancariaId.HasValue)
        {
            var categoriaId = await ObterOuCriarCategoriaDeComissaoAsync(lojaId.Value);
            var lancamento = new LancamentoFinanceiro
            {
                LojaId = lojaId.Value,
                ContaBancariaId = req.ContaBancariaId.Value,
                Tipo = "pagar",
                Modo = "avulsa",
                Descricao = EquipeRegras.DescricaoDoFechamento(profissional.Nome, req.QtdDiarias, req.PeriodoInicio, req.PeriodoFim),
                CategoriaId = categoriaId,
                Valor = valorTotal,
                Vencimento = EquipeRegras.VencimentoDoFechamento(req.Vencimento, req.PeriodoFim),
                Status = "pendente",
            };
            repositorio.Adicionar(lancamento);
            await repositorio.SalvarAsync();

            fechamento.LancamentoFinanceiroId = lancamento.Id;
        }

        await repositorio.SalvarAsync();

        return Resultado.Ok(new
        {
            fechamento.Id,
            fechamento.ValorTotal,
            fechamento.QtdAtendimentos,
            fechamento.QtdDiarias,
            fechamento.ValorDiarias,
            profissionalNome = profissional.Nome,
        });
    }

    // ── Histórico de fechamentos já pagos ────────────────────────────────────

    public async Task<Resultado<object>> ListarFechamentosAsync(Guid? lojaId, Guid? profissionalId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarFechamentosAsync(lojaId.Value, profissionalId));
    }

    // ── Desfazer um fechamento (volta as comissões para pendente) ────────────

    public async Task<Resultado<object>> DesfazerFechamentoAsync(Guid? lojaId, Guid id)
    {
        var fechamento = await repositorio.ObterFechamentoAsync(id, lojaId);
        if (fechamento is null) return Resultado.NaoEncontrado();

        if (fechamento.LancamentoFinanceiroId.HasValue)
        {
            var lancamento = await repositorio.ObterLancamentoFinanceiroAsync(fechamento.LancamentoFinanceiroId.Value);
            if (lancamento != null)
            {
                if (lancamento.Status == "pago")
                    return Resultado.Invalido("Essa comissão já foi paga no Financeiro. Estorne o lançamento lá antes de desfazer o fechamento.");

                repositorio.Remover(lancamento);
            }
        }

        foreach (var c in await repositorio.ListarComissoesDoFechamentoAsync(id))
        {
            c.Status = StatusComissao.Pendente;
            c.PagoEm = null;
            c.FechamentoId = null;
        }

        repositorio.Remover(fechamento);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { mensagem = "Fechamento desfeito. As comissões voltaram para pendente." });
    }

    // Categoria do Financeiro onde caem as comissões pagas (criada na primeira vez)
    private async Task<Guid> ObterOuCriarCategoriaDeComissaoAsync(Guid lojaId)
    {
        var categoria = await repositorio.ObterCategoriaFinanceiraAsync(lojaId, EquipeRegras.CategoriaComissoes);
        if (categoria != null) return categoria.Id;

        categoria = new CategoriaFinanceira { LojaId = lojaId, Nome = EquipeRegras.CategoriaComissoes, Tipo = "pagar", Icone = EquipeRegras.IconeComissoes };
        repositorio.Adicionar(categoria);
        await repositorio.SalvarAsync();
        return categoria.Id;
    }
}
