using LojaApi.Application.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;
using LojaApi.Tests.Suporte.Falsos;

namespace LojaApi.Tests.Application.Handlers.EquipeTeste;

// Handlers dos funcionários, testados com repositório e Financeiro de mentira (sem banco, sem HTTP).
public class FuncionariosHandlersTests
{
    private static readonly Guid Loja = Guid.NewGuid();
    private static readonly Guid OutraLoja = Guid.NewGuid();

    private readonly RepositorioEquipeFalso _repo = new();
    private readonly LancamentoFixoFinanceiroFalso _financeiro = new();

    private Task<Resultado<object>> CriarFuncionario(Guid? loja, SalvarFuncionarioRequest req) => new CriarFuncionarioCommandHandler(_repo, _financeiro).HandleAsync(new(loja, req));
    private Task<Resultado<object>> AtualizarFuncionario(Guid? loja, Guid id, SalvarFuncionarioRequest req) => new AtualizarFuncionarioCommandHandler(_repo, _financeiro).HandleAsync(new(loja, id, req));
    private Task<Resultado<object>> AlternarAtivoFuncionario(Guid? loja, Guid id) => new AlternarAtivoFuncionarioCommandHandler(_repo).HandleAsync(new(loja, id));
    private Task<Resultado<object>> ExcluirFuncionario(Guid? loja, Guid id) => new ExcluirFuncionarioCommandHandler(_repo).HandleAsync(new(loja, id));
    private Task<Resultado<object>> DefinirComissaoServico(Guid? loja, Guid id, SalvarComissaoServicoRequest req) => new DefinirComissaoServicoCommandHandler(_repo).HandleAsync(new(loja, id, req));
    private Task<Resultado<object>> RemoverComissaoServico(Guid? loja, Guid comissaoId) => new RemoverComissaoServicoCommandHandler(_repo).HandleAsync(new(loja, comissaoId));

    private Guid ContaDaLoja() { var id = Guid.NewGuid(); _repo.Contas.Add((id, Loja)); return id; }

    private static SalvarFuncionarioRequest Pedido(string nome = "Joana", string tipo = "comissao", decimal? salario = null, Guid? conta = null, int? dia = null, decimal? diaria = null) =>
        new(nome, 10m, true, dia, tipo, salario, conta, ValorDiaria: diaria);

    private Profissional Existente(string tipo = "comissao", Guid? fixoId = null, bool ativo = true)
    {
        var p = new Profissional { LojaId = Loja, Nome = "Joana", TipoRemuneracao = tipo, LancamentoFixoId = fixoId, Ativo = ativo };
        _repo.Profissionais.Add(p);
        return p;
    }

    // ── Criar ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Criar_sem_loja_ou_sem_nome_nao_grava_nada()
    {
        var semLoja = await CriarFuncionario(null, Pedido());
        Assert.Equal((TipoResultado.Invalido, "Loja não encontrada."), (semLoja.Tipo, semLoja.Mensagem));

        var semNome = await CriarFuncionario(Loja, Pedido("  "));
        Assert.Equal((TipoResultado.Invalido, "Nome é obrigatório."), (semNome.Tipo, semNome.Mensagem));
        Assert.Empty(_repo.Profissionais);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Criar_valida_os_valores_da_remuneracao()
    {
        Assert.Equal("Informe o valor do salário fixo.", (await CriarFuncionario(Loja, Pedido(tipo: "salario_fixo"))).Mensagem);
        Assert.Equal("Informe o valor da diária.", (await CriarFuncionario(Loja, Pedido(tipo: "diaria"))).Mensagem);
        Assert.Empty(_repo.Profissionais);
    }

    [Fact]
    public async Task Salario_fixo_com_conta_da_loja_cria_o_lancamento_fixo_e_pede_o_lote_ao_financeiro()
    {
        var conta = ContaDaLoja();

        var r = await CriarFuncionario(Loja, Pedido("Anderson", "salario_fixo", 3000, conta));

        Assert.True(r.Sucesso);
        var profissional = Assert.Single(_repo.Profissionais);
        var fixo = Assert.Single(_repo.Fixos);
        Assert.Equal(("Salário — Anderson", 3000m, 5, conta, Loja), (fixo.Descricao, fixo.Valor, fixo.DiaVencimento, fixo.ContaBancariaId, fixo.LojaId));   // sem dia: vence no 5
        Assert.Equal(fixo.Id, profissional.LancamentoFixoId);
        Assert.Equal([$"gerar:{fixo.Id}"], _financeiro.Chamadas);
        var categoria = Assert.Single(_repo.Categorias);
        Assert.Equal(("Salários de Funcionários", fixo.CategoriaId), (categoria.Nome, categoria.Id));
    }

    [Fact]
    public async Task Salario_fixo_com_conta_de_outra_loja_e_recusado_sem_gravar_nada()
    {
        var contaAlheia = Guid.NewGuid();
        _repo.Contas.Add((contaAlheia, OutraLoja));

        var r = await CriarFuncionario(Loja, Pedido("Fixo", "salario_fixo", 3000, contaAlheia));

        Assert.Equal((TipoResultado.Invalido, "Conta bancária não encontrada."), (r.Tipo, r.Mensagem));
        Assert.Empty(_repo.Profissionais);
        Assert.Empty(_repo.Fixos);
        Assert.Empty(_financeiro.Chamadas);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Fora_do_salario_fixo_a_conta_nao_e_usada_nem_validada()
    {
        var r = await CriarFuncionario(Loja, Pedido("Comissionada", "comissao", conta: Guid.NewGuid()));

        Assert.True(r.Sucesso);
        Assert.Empty(_repo.Fixos);
        Assert.Empty(_financeiro.Chamadas);
    }

    [Fact]
    public async Task Segundo_salario_fixo_reaproveita_a_categoria()
    {
        var conta = ContaDaLoja();
        await CriarFuncionario(Loja, Pedido("Um", "salario_fixo", 1000, conta));
        await CriarFuncionario(Loja, Pedido("Dois", "salario_fixo", 2000, conta, dia: 15));

        Assert.Single(_repo.Categorias);
        Assert.Equal([5, 15], _repo.Fixos.Select(f => f.DiaVencimento).ToList());
    }

    // ── Atualizar ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Atualizar_da_404_para_inexistente_ou_de_outra_loja()
    {
        var existente = Existente();

        Assert.Equal(TipoResultado.NaoEncontrado, (await AtualizarFuncionario(Loja, Guid.NewGuid(), Pedido())).Tipo);
        Assert.Equal(TipoResultado.NaoEncontrado, (await AtualizarFuncionario(OutraLoja, existente.Id, Pedido("Invasor"))).Tipo);
        Assert.Equal("Joana", existente.Nome);
    }

    [Fact]
    public async Task Sair_do_salario_fixo_desativa_o_lancamento_fixo_e_solta_o_vinculo()
    {
        var fixo = new LancamentoFixo { LojaId = Loja, Ativa = true };
        _repo.Fixos.Add(fixo);
        var profissional = Existente("salario_fixo", fixo.Id);

        var r = await AtualizarFuncionario(Loja, profissional.Id, Pedido("Joana", "comissao"));

        Assert.True(r.Sucesso);
        Assert.False(fixo.Ativa);
        Assert.Null(profissional.LancamentoFixoId);
        Assert.Empty(_financeiro.Chamadas);
    }

    [Fact]
    public async Task Editar_o_salario_fixo_atualiza_o_fixo_e_manda_limpar_os_futuros_antes_de_gerar()
    {
        var conta = ContaDaLoja();
        var fixo = new LancamentoFixo { LojaId = Loja, Valor = 3000, DiaVencimento = 10, Ativa = false };
        _repo.Fixos.Add(fixo);
        var profissional = Existente("salario_fixo", fixo.Id);

        await AtualizarFuncionario(Loja, profissional.Id, Pedido("Joana", "salario_fixo", 3500, conta, dia: 12));

        Assert.Equal((3500m, 12, conta, true), (fixo.Valor, fixo.DiaVencimento, fixo.ContaBancariaId, fixo.Ativa));
        Assert.Equal([$"limpar:{fixo.Id}", $"gerar:{fixo.Id}"], _financeiro.Chamadas);   // nessa ordem
    }

    [Fact]
    public async Task Entrar_em_salario_fixo_pela_edicao_cria_o_lancamento_fixo()
    {
        var conta = ContaDaLoja();
        var profissional = Existente("comissao");

        await AtualizarFuncionario(Loja, profissional.Id, Pedido("Joana", "salario_fixo", 2500, conta, dia: 6));

        var fixo = Assert.Single(_repo.Fixos);
        Assert.Equal((2500m, 6), (fixo.Valor, fixo.DiaVencimento));
        Assert.Equal(fixo.Id, profissional.LancamentoFixoId);
        Assert.Equal([$"gerar:{fixo.Id}"], _financeiro.Chamadas);
    }

    [Fact]
    public async Task Atualizar_com_conta_de_outra_loja_nao_altera_nada()
    {
        var contaAlheia = Guid.NewGuid();
        _repo.Contas.Add((contaAlheia, OutraLoja));
        var profissional = Existente("comissao");

        var r = await AtualizarFuncionario(Loja, profissional.Id, Pedido("Novo nome", "salario_fixo", 2000, contaAlheia));

        Assert.Equal((TipoResultado.Invalido, "Conta bancária não encontrada."), (r.Tipo, r.Mensagem));
        Assert.Equal(("Joana", "comissao"), (profissional.Nome, profissional.TipoRemuneracao));
        Assert.Empty(_repo.Fixos);
    }

    // ── Ativar/desativar e excluir ───────────────────────────────────────────

    [Fact]
    public async Task Alternar_ativo_acompanha_o_lancamento_fixo()
    {
        var fixo = new LancamentoFixo { LojaId = Loja, Ativa = true };
        _repo.Fixos.Add(fixo);
        var profissional = Existente("salario_fixo", fixo.Id);

        await AlternarAtivoFuncionario(Loja, profissional.Id);
        Assert.False(profissional.Ativo);
        Assert.False(fixo.Ativa);
        await AlternarAtivoFuncionario(Loja, profissional.Id);
        Assert.True(fixo.Ativa);
    }

    [Fact]
    public async Task Excluir_so_desativa_quem_tem_historico_e_remove_quem_nao_tem()
    {
        var usado = Existente();
        var livre = Existente();
        _repo.ProfissionaisEmUso.Add(usado.Id);

        Assert.True((await ExcluirFuncionario(Loja, usado.Id)).Sucesso);
        Assert.False(usado.Ativo);
        Assert.Contains(usado, _repo.Profissionais);

        Assert.True((await ExcluirFuncionario(Loja, livre.Id)).Sucesso);
        Assert.DoesNotContain(livre, _repo.Profissionais);
        Assert.Equal(TipoResultado.NaoEncontrado, (await ExcluirFuncionario(Loja, livre.Id)).Tipo);
    }

    // ── Comissão por serviço ─────────────────────────────────────────────────

    [Fact]
    public async Task Comissao_por_servico_cria_atualiza_e_so_aceita_servico_da_loja()
    {
        var profissional = Existente();
        var servico = Guid.NewGuid();
        var servicoAlheio = Guid.NewGuid();
        _repo.Servicos.Add((servico, Loja));
        _repo.Servicos.Add((servicoAlheio, OutraLoja));

        var alheio = await DefinirComissaoServico(Loja, profissional.Id, new(servicoAlheio, 5));
        Assert.Equal((TipoResultado.Invalido, "Serviço não encontrado."), (alheio.Tipo, alheio.Mensagem));

        await DefinirComissaoServico(Loja, profissional.Id, new(servico, 25));
        await DefinirComissaoServico(Loja, profissional.Id, new(servico, 30));
        var linha = Assert.Single(_repo.ComissoesServico);
        Assert.Equal((30m, Loja), (linha.ComissaoPercentual, linha.LojaId));

        Assert.True((await RemoverComissaoServico(Loja, linha.Id)).Sucesso);
        Assert.Empty(_repo.ComissoesServico);
        Assert.Equal(TipoResultado.NaoEncontrado, (await RemoverComissaoServico(Loja, linha.Id)).Tipo);
    }
}
