using LojaApi.Application.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;
using LojaApi.Tests.Suporte.Falsos;

namespace LojaApi.Tests.Application.Services.EquipeTeste;

// Service das comissões, testado com repositório em memória (sem banco, sem HTTP).
public class ComissoesServiceTests
{
    private static readonly Guid Loja = Guid.NewGuid();
    private static readonly Guid OutraLoja = Guid.NewGuid();
    private static readonly DateTime Hoje = DateTime.UtcNow.Date;

    private readonly RepositorioEquipeFalso _repo = new();
    private ComissoesService Servico() => new(_repo);

    private Profissional Profissional(string nome = "Joana", decimal? diaria = null)
    {
        var p = new Profissional { LojaId = Loja, Nome = nome, ValorDiaria = diaria };
        _repo.Profissionais.Add(p);
        return p;
    }

    private ComissaoFuncionario Comissao(Guid profissionalId, decimal valorComissao, string status = "pendente", DateTime? criadoEm = null)
    {
        var c = new ComissaoFuncionario { LojaId = Loja, ProfissionalId = profissionalId, ValorServico = valorComissao * 10, ComissaoPercentual = 10, ValorComissao = valorComissao, Status = status, CriadoEm = criadoEm ?? DateTime.UtcNow };
        _repo.Comissoes.Add(c);
        return c;
    }

    private Guid ContaDaLoja() { var id = Guid.NewGuid(); _repo.Contas.Add((id, Loja)); return id; }

    private static FecharComissaoRequest Fechar(Guid profissional, int diarias = 0, Guid? conta = null, DateTime? vencimento = null) =>
        new(profissional, Hoje.AddDays(-1), Hoje.AddDays(1), conta, vencimento, diarias);

    // ── Resumo ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resumo_soma_as_pendentes_por_profissional_do_maior_para_o_menor()
    {
        var ana = Profissional("Ana");
        var bia = Profissional("Bia", 120);
        Comissao(ana.Id, 10); Comissao(ana.Id, 10); Comissao(bia.Id, 30); Comissao(bia.Id, 99, "pago");

        var r = await Servico().ResumoAsync(Loja, null, null);

        var lista = Assert.IsType<List<ResumoComissaoProfissionalDto>>(r.Valor);
        Assert.Equal([("Bia", 30m, 1), ("Ana", 20m, 2)], lista.Select(x => (x.ProfissionalNome, x.ValorTotal, x.QtdAtendimentos)).ToList());
        Assert.Equal(120m, lista[0].ValorDiaria);
        Assert.Empty((IEnumerable<object>)(await Servico().ResumoAsync(null, null, null)).Valor!);   // sem loja: vazio
    }

    // ── Fechar ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Fechar_valida_loja_profissional_diarias_e_se_ha_o_que_pagar()
    {
        var p = Profissional();

        Assert.Equal((TipoResultado.Invalido, "Loja não encontrada."), ((await Servico().FecharAsync(null, Fechar(p.Id))) is var a ? (a.Tipo, a.Mensagem) : default));
        var inexistente = await Servico().FecharAsync(Loja, Fechar(Guid.NewGuid()));
        Assert.Equal((TipoResultado.NaoEncontrado, "Profissional não encontrado."), (inexistente.Tipo, inexistente.Mensagem));
        Assert.Equal("Quantidade de diárias inválida.", (await Servico().FecharAsync(Loja, Fechar(p.Id, diarias: -1))).Mensagem);
        Assert.Equal("Este profissional não tem valor de diária cadastrado.", (await Servico().FecharAsync(Loja, Fechar(p.Id, diarias: 2))).Mensagem);
        Assert.Equal("Nenhuma comissão pendente encontrada nesse período, e nenhuma diária informada.", (await Servico().FecharAsync(Loja, Fechar(p.Id))).Mensagem);
        Assert.Empty(_repo.Fechamentos);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Fechar_com_conta_de_outra_loja_e_recusado_sem_marcar_nada_como_pago()
    {
        var p = Profissional();
        var comissao = Comissao(p.Id, 10);
        var contaAlheia = Guid.NewGuid();
        _repo.Contas.Add((contaAlheia, OutraLoja));

        var r = await Servico().FecharAsync(Loja, Fechar(p.Id, conta: contaAlheia));

        Assert.Equal((TipoResultado.Invalido, "Conta bancária não encontrada."), (r.Tipo, r.Mensagem));
        Assert.Equal("pendente", comissao.Status);
        Assert.Empty(_repo.Fechamentos);
        Assert.Empty(_repo.Lancamentos);
    }

    [Fact]
    public async Task Fechar_soma_comissoes_e_diarias_e_marca_as_comissoes_do_periodo_como_pagas()
    {
        var p = Profissional(diaria: 100);
        var a = Comissao(p.Id, 20);
        var b = Comissao(p.Id, 30);
        var foraDoPeriodo = Comissao(p.Id, 500, criadoEm: DateTime.UtcNow.AddDays(-30));
        var jaPaga = Comissao(p.Id, 700, "pago");

        var r = await Servico().FecharAsync(Loja, Fechar(p.Id, diarias: 2));

        Assert.True(r.Sucesso);
        var fechamento = Assert.Single(_repo.Fechamentos);
        Assert.Equal((250m, 2, 200m, 2), (fechamento.ValorTotal, fechamento.QtdDiarias, fechamento.ValorDiarias, fechamento.QtdAtendimentos));   // 20 + 30 + 2 x 100
        Assert.All(new[] { a, b }, c => Assert.Equal(("pago", fechamento.Id), (c.Status, c.FechamentoId!.Value)));
        Assert.Equal("pendente", foraDoPeriodo.Status);
        Assert.Null(jaPaga.FechamentoId);
        Assert.Null(fechamento.LancamentoFinanceiroId);   // sem conta, não vai para o Financeiro
        Assert.Empty(_repo.Lancamentos);
    }

    [Fact]
    public async Task Fechar_com_conta_cria_a_conta_a_pagar_e_reaproveita_a_categoria()
    {
        var p = Profissional("Joana");
        var conta = ContaDaLoja();
        Comissao(p.Id, 40);

        await Servico().FecharAsync(Loja, Fechar(p.Id, conta: conta));
        Comissao(p.Id, 10);
        await Servico().FecharAsync(Loja, Fechar(p.Id, conta: conta, vencimento: Hoje.AddDays(20)));

        Assert.Equal(2, _repo.Lancamentos.Count);
        var primeiro = _repo.Lancamentos[0];
        Assert.Equal(("pagar", "avulsa", "pendente", 40m, conta), (primeiro.Tipo, primeiro.Modo, primeiro.Status, primeiro.Valor, primeiro.ContaBancariaId));
        Assert.Equal(Hoje.AddDays(1 + 5).AddHours(12), primeiro.Vencimento);                    // fim do período + 5 dias, ao meio-dia
        Assert.Equal(Hoje.AddDays(20).AddHours(12), _repo.Lancamentos[1].Vencimento);           // o informado
        Assert.StartsWith("Comissão — Joana (", primeiro.Descricao);
        Assert.Equal(_repo.Fechamentos[0].LancamentoFinanceiroId, primeiro.Id);
        var categoria = Assert.Single(_repo.Categorias);
        Assert.Equal(("Comissões de Funcionários", categoria.Id), (categoria.Nome, primeiro.CategoriaId!.Value));
    }

    // ── Desfazer ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Desfazer_volta_as_comissoes_para_pendente_e_apaga_fechamento_e_lancamento()
    {
        var p = Profissional();
        var conta = ContaDaLoja();
        var comissao = Comissao(p.Id, 40);
        await Servico().FecharAsync(Loja, Fechar(p.Id, conta: conta));
        var fechamentoId = _repo.Fechamentos.Single().Id;

        var r = await Servico().DesfazerFechamentoAsync(Loja, fechamentoId);

        Assert.True(r.Sucesso);
        Assert.Equal(("pendente", null, null), (comissao.Status, comissao.PagoEm?.ToString(), comissao.FechamentoId?.ToString()));
        Assert.Empty(_repo.Fechamentos);
        Assert.Empty(_repo.Lancamentos);
        Assert.Equal(TipoResultado.NaoEncontrado, (await Servico().DesfazerFechamentoAsync(Loja, fechamentoId)).Tipo);
    }

    [Fact]
    public async Task Desfazer_e_recusado_se_o_lancamento_ja_foi_pago_no_financeiro()
    {
        var p = Profissional();
        var comissao = Comissao(p.Id, 40);
        await Servico().FecharAsync(Loja, Fechar(p.Id, conta: ContaDaLoja()));
        var fechamentoId = _repo.Fechamentos.Single().Id;
        _repo.Lancamentos.Single().Status = "pago";

        var r = await Servico().DesfazerFechamentoAsync(Loja, fechamentoId);

        Assert.Equal((TipoResultado.Invalido, "Essa comissão já foi paga no Financeiro. Estorne o lançamento lá antes de desfazer o fechamento."), (r.Tipo, r.Mensagem));
        Assert.Single(_repo.Fechamentos);
        Assert.Equal("pago", comissao.Status);
    }

    [Fact]
    public async Task Desfazer_nao_mexe_em_fechamento_de_outra_loja()
    {
        var p = Profissional();
        Comissao(p.Id, 40);
        await Servico().FecharAsync(Loja, Fechar(p.Id));
        var fechamentoId = _repo.Fechamentos.Single().Id;

        Assert.Equal(TipoResultado.NaoEncontrado, (await Servico().DesfazerFechamentoAsync(OutraLoja, fechamentoId)).Tipo);
        Assert.Single(_repo.Fechamentos);
    }
}
