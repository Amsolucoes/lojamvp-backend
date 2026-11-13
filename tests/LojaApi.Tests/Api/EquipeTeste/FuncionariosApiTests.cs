using System.Text.Json;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.EquipeTeste;

// Cadastro de funcionários/profissionais: remuneração (comissão, salário fixo, diária), endereço e comissão por serviço.
[Collection(ColecaoApi.Nome)]
public class FuncionariosApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private Task<Profissional> NoBanco(Guid id) => api.NoBancoAsync(db => db.Profissionais.AsNoTracking().FirstAsync(p => p.Id == id));
    private Task<List<LancamentoFixo>> FixosDa(LojaTeste loja) => api.NoBancoAsync(db => db.LancamentosFixos.AsNoTracking().Where(f => f.LojaId == loja.Id).ToListAsync());

    // ── Permissões ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_altera_o_cadastro()
    {
        var loja = await NovaAsync();
        var id = await loja.FuncionarioIdAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/funcionarios")).Codigo);

        var operador = await api.OperadorAsync(loja);
        Assert.Equal(200, (await operador.Get("/api/funcionarios")).Codigo);
        Assert.Equal(200, (await operador.Get("/api/funcionarios/ativos")).Codigo);
        Assert.Equal(403, (await operador.Post("/api/funcionarios", CenariosEquipe.Corpo())).Codigo);
        Assert.Equal(403, (await operador.Put($"/api/funcionarios/{id}", CenariosEquipe.Corpo())).Codigo);
        Assert.Equal(403, (await operador.Patch($"/api/funcionarios/{id}/ativo")).Codigo);
        Assert.Equal(403, (await operador.Delete($"/api/funcionarios/{id}")).Codigo);
        Assert.Equal(403, (await operador.Post($"/api/funcionarios/{id}/comissoes-servico", new { servicoId = Guid.NewGuid(), comissaoPercentual = 5 })).Codigo);
        Assert.Equal(403, (await operador.Delete($"/api/funcionarios/comissoes-servico/{Guid.NewGuid()}")).Codigo);
    }

    // ── Criar ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_valida_nome_e_os_valores_de_cada_tipo_de_remuneracao()
    {
        var loja = await NovaAsync();

        Assert.Equal((400, "Nome é obrigatório."), ((await loja.NovoFuncionarioAsync(nome: "  ")) is var a ? (a.Codigo, a.Erro) : default));
        Assert.Equal("Informe o valor do salário fixo.", (await loja.NovoFuncionarioAsync(tipo: "salario_fixo", salario: null)).Erro);
        Assert.Equal("Informe o valor do salário fixo.", (await loja.NovoFuncionarioAsync(tipo: "salario_fixo", salario: 0)).Erro);
        Assert.Equal("Informe o valor da diária.", (await loja.NovoFuncionarioAsync(tipo: "diaria", diaria: null)).Erro);
        Assert.Equal("Informe o percentual de comissão em vendas.", (await loja.NovoFuncionarioAsync(comissaoVendas: true, percentualVendas: null)).Erro);
        Assert.Equal("Informe o percentual de comissão em vendas.", (await loja.NovoFuncionarioAsync(comissaoVendas: true, percentualVendas: 0)).Erro);
        Assert.Equal(0, (await loja.Painel.Get("/api/funcionarios")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Criar_so_guarda_o_que_vale_para_o_tipo_de_remuneracao()
    {
        var loja = await NovaAsync();

        var comissao = (await loja.NovoFuncionarioAsync("Comissionada", 12m, "comissao", salario: 3000, diaria: 100, dia: 31, baseCalculo: "qualquer", comissaoVendas: false, percentualVendas: 7)).Json;
        Assert.Equal((12m, "comissao", "total", false), (comissao.GetProperty("comissaoPadraoPercentual").GetDecimal(), comissao.GetProperty("tipoRemuneracao").GetString(),
            comissao.GetProperty("comissaoBaseCalculo").GetString(), comissao.GetProperty("comissaoVendasAtiva").GetBoolean()));
        Assert.Equal(JsonValueKind.Null, comissao.GetProperty("salarioFixo").ValueKind);                 // só vale no tipo salario_fixo
        Assert.Equal(JsonValueKind.Null, comissao.GetProperty("valorDiaria").ValueKind);                 // só vale no tipo diaria
        Assert.Equal(JsonValueKind.Null, comissao.GetProperty("diaPagamentoPadrao").ValueKind);          // 31 está fora de 1-28
        Assert.Equal(JsonValueKind.Null, comissao.GetProperty("comissaoVendasPercentual").ValueKind);    // vendas desligada

        var diaria = (await loja.NovoFuncionarioAsync("Diarista", null, "diaria", salario: 999, diaria: 150, dia: 15, baseCalculo: "servico", comissaoVendas: true, percentualVendas: 5)).Json;
        Assert.Equal((150m, 15, "servico", true, 5m), (diaria.GetProperty("valorDiaria").GetDecimal(), diaria.GetProperty("diaPagamentoPadrao").GetInt32(),
            diaria.GetProperty("comissaoBaseCalculo").GetString(), diaria.GetProperty("comissaoVendasAtiva").GetBoolean(), diaria.GetProperty("comissaoVendasPercentual").GetDecimal()));
        Assert.Equal(JsonValueKind.Null, diaria.GetProperty("salarioFixo").ValueKind);
        Assert.Equal(JsonValueKind.Null, diaria.GetProperty("comissaoPadraoPercentual").ValueKind);
    }

    [FatoIntegracao]
    public async Task Criar_limpa_o_endereco_e_corta_no_tamanho_maximo()
    {
        var loja = await NovaAsync();

        var r = (await loja.NovoFuncionarioAsync("  Maria  ", telefone: "67999990000", cep: " 79000-0000123 ", endereco: "   ", numero: " 10A ", bairro: new string('b', 150), cidade: " Campo Grande ", uf: " ms ")).Json;

        Assert.Equal("Maria", r.GetProperty("nome").GetString());
        Assert.Equal("79000-000", r.GetProperty("cep").GetString());                       // 9 caracteres
        Assert.Equal(JsonValueKind.Null, r.GetProperty("endereco").ValueKind);              // só espaços = nulo
        Assert.Equal(("10A", 100, "Campo Grande", "MS"), (r.GetProperty("numero").GetString(), r.GetProperty("bairro").GetString()!.Length,
            r.GetProperty("cidade").GetString(), r.GetProperty("uf").GetString()));
        Assert.Equal("67999990000", r.GetProperty("telefone").GetString());
        Assert.Equal("MA", (await loja.NovoFuncionarioAsync("Outro", uf: "mato grosso")).Json.GetProperty("uf").GetString());
    }

    // ── Salário fixo vira lançamento fixo no Financeiro ──────────────────────

    [FatoIntegracao]
    public async Task Salario_fixo_com_conta_cria_o_lancamento_fixo_e_gera_os_60_meses()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaBancariaIdAsync();

        var id = await loja.FuncionarioIdAsync("Anderson", null, "salario_fixo", salario: 3000, conta: conta, dia: 10);

        var fixo = Assert.Single(await FixosDa(loja));
        Assert.Equal(("Salário — Anderson", "pagar", 3000m, 10, true, conta), (fixo.Descricao, fixo.Tipo, fixo.Valor, fixo.DiaVencimento, fixo.Ativa, fixo.ContaBancariaId));
        Assert.Equal(fixo.Id, (await NoBanco(id)).LancamentoFixoId);
        var lancamentos = await api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().Where(l => l.LancamentoFixoId == fixo.Id).ToListAsync());
        Assert.Equal(60, lancamentos.Count);
        Assert.All(lancamentos, l => Assert.Equal(("pendente", "pagar", 3000m), (l.Status, l.Tipo, l.Valor)));
        var categoria = await api.NoBancoAsync(db => db.CategoriasFinanceiras.AsNoTracking().SingleAsync(c => c.LojaId == loja.Id && c.Nome == "Salários de Funcionários"));
        Assert.Equal(categoria.Id, fixo.CategoriaId);
    }

    [FatoIntegracao]
    public async Task Salario_fixo_usa_dia_5_sem_dia_informado_e_reaproveita_a_categoria()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaBancariaIdAsync();

        await loja.FuncionarioIdAsync("Um", null, "salario_fixo", salario: 1000, conta: conta);
        await loja.FuncionarioIdAsync("Dois", null, "salario_fixo", salario: 2000, conta: conta, dia: 29);   // 29 é inválido: também cai no 5

        var fixos = await FixosDa(loja);
        Assert.Equal([5, 5], fixos.Select(f => f.DiaVencimento).ToList());
        Assert.Equal(1, await api.NoBancoAsync(db => db.CategoriasFinanceiras.CountAsync(c => c.LojaId == loja.Id && c.Nome == "Salários de Funcionários")));
    }

    [FatoIntegracao]
    public async Task Salario_fixo_sem_conta_nao_cria_lancamento()
    {
        var loja = await NovaAsync();

        var id = await loja.FuncionarioIdAsync("Sem conta", null, "salario_fixo", salario: 1500);

        Assert.Empty(await FixosDa(loja));
        Assert.Null((await NoBanco(id)).LancamentoFixoId);
        Assert.Equal(1500m, (await NoBanco(id)).SalarioFixo);
    }

    // ── Atualizar ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Atualizar_troca_os_dados_valida_e_da_404_para_inexistente_ou_de_outra_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var id = await a.FuncionarioIdAsync("Antigo", 10m);

        var r = await a.Painel.Put($"/api/funcionarios/{id}", CenariosEquipe.Corpo("  Novo  ", 15m, ativo: false, telefone: "67911112222", uf: "sp", dia: 20));
        Assert.Equal(200, r.Codigo);
        var p = await NoBanco(id);
        Assert.Equal(("Novo", 15m, false, "67911112222", "SP", 20), (p.Nome, p.ComissaoPadraoPercentual, p.Ativo, p.Telefone, p.Uf, p.DiaPagamentoPadrao));

        Assert.Equal("Informe o valor do salário fixo.", (await a.Painel.Put($"/api/funcionarios/{id}", CenariosEquipe.Corpo(tipo: "salario_fixo"))).Erro);
        Assert.Equal("Informe o valor da diária.", (await a.Painel.Put($"/api/funcionarios/{id}", CenariosEquipe.Corpo(tipo: "diaria"))).Erro);
        Assert.Equal("Informe o percentual de comissão em vendas.", (await a.Painel.Put($"/api/funcionarios/{id}", CenariosEquipe.Corpo(comissaoVendas: true))).Erro);

        Assert.Equal(404, (await a.Painel.Put($"/api/funcionarios/{Guid.NewGuid()}", CenariosEquipe.Corpo())).Codigo);
        Assert.Equal(404, (await b.Painel.Put($"/api/funcionarios/{id}", CenariosEquipe.Corpo("Invasor"))).Codigo);
        Assert.Equal("Novo", (await NoBanco(id)).Nome);
    }

    [FatoIntegracao]
    public async Task Sair_do_salario_fixo_desativa_o_lancamento_fixo()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var id = await loja.FuncionarioIdAsync("Ex-fixo", null, "salario_fixo", salario: 3000, conta: conta, dia: 8);

        (await loja.Painel.Put($"/api/funcionarios/{id}", CenariosEquipe.Corpo("Ex-fixo", 10m, "comissao"))).DeveSerSucesso();

        var fixo = Assert.Single(await FixosDa(loja));
        Assert.False(fixo.Ativa);
        var p = await NoBanco(id);
        Assert.Null(p.LancamentoFixoId);
        Assert.Null(p.SalarioFixo);
    }

    [FatoIntegracao]
    public async Task Entrar_em_salario_fixo_pela_edicao_cria_o_lancamento_fixo()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var id = await loja.FuncionarioIdAsync("Comissionado", 10m);
        Assert.Empty(await FixosDa(loja));

        (await loja.Painel.Put($"/api/funcionarios/{id}", CenariosEquipe.Corpo("Comissionado", 10m, "salario_fixo", salario: 2500, conta: conta, dia: 6))).DeveSerSucesso();

        var fixo = Assert.Single(await FixosDa(loja));
        Assert.Equal(("Salário — Comissionado", 2500m, 6), (fixo.Descricao, fixo.Valor, fixo.DiaVencimento));
        Assert.Equal(fixo.Id, (await NoBanco(id)).LancamentoFixoId);
    }

    [FatoIntegracao]
    public async Task Editar_o_salario_fixo_atualiza_o_valor_o_dia_e_a_conta_do_lancamento_fixo()
    {
        var loja = await NovaAsync();
        var conta1 = await loja.ContaBancariaIdAsync("Conta 1");
        var conta2 = await loja.ContaBancariaIdAsync("Conta 2");
        var id = await loja.FuncionarioIdAsync("Fixo", null, "salario_fixo", salario: 3000, conta: conta1, dia: 10);

        (await loja.Painel.Put($"/api/funcionarios/{id}", CenariosEquipe.Corpo("Fixo", null, "salario_fixo", salario: 3500, conta: conta2, dia: 12))).DeveSerSucesso();

        var fixo = Assert.Single(await FixosDa(loja));
        Assert.Equal((3500m, 12, conta2, true), (fixo.Valor, fixo.DiaVencimento, fixo.ContaBancariaId, fixo.Ativa));
        Assert.Equal(fixo.Id, (await NoBanco(id)).LancamentoFixoId);   // continua o mesmo lançamento fixo
    }

    // ── Ativar/desativar e excluir ───────────────────────────────────────────

    [FatoIntegracao]
    public async Task Alternar_ativo_tambem_liga_e_desliga_o_lancamento_fixo_do_salario()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var id = await loja.FuncionarioIdAsync("Fixo", null, "salario_fixo", salario: 3000, conta: conta);

        Assert.False((await loja.Painel.Patch($"/api/funcionarios/{id}/ativo")).Json.GetProperty("ativo").GetBoolean());
        Assert.False(Assert.Single(await FixosDa(loja)).Ativa);
        Assert.True((await loja.Painel.Patch($"/api/funcionarios/{id}/ativo")).Json.GetProperty("ativo").GetBoolean());
        Assert.True(Assert.Single(await FixosDa(loja)).Ativa);
        Assert.Equal(404, (await loja.Painel.Patch($"/api/funcionarios/{Guid.NewGuid()}/ativo")).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_remove_quem_nunca_foi_usado_e_so_desativa_quem_tem_historico()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var livre = await loja.FuncionarioIdAsync("Livre");
        var comAgendamento = await loja.FuncionarioIdAsync("Com agenda");
        var comComissao = await loja.FuncionarioIdAsync("Com comissao");
        var servico = await loja.ServicoIdAsync();
        (await loja.AgendarAsync(servico, $"{Datas.Texto(Datas.Segunda())}T09:00:00", nomeCliente: "X", profissionalId: comAgendamento)).DeveSerSucesso();
        await api.NoBancoAsync(async db =>
        {
            db.ComissoesFuncionario.Add(new ComissaoFuncionario { LojaId = loja.Id, ProfissionalId = comComissao, OrigemId = Guid.NewGuid(), ValorServico = 10, ComissaoPercentual = 10, ValorComissao = 1 });
            await db.SaveChangesAsync();
        });

        Assert.Equal("Profissional excluído.", (await loja.Painel.Delete($"/api/funcionarios/{livre}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(0, await api.NoBancoAsync(db => db.Profissionais.CountAsync(p => p.Id == livre)));

        foreach (var id in new[] { comAgendamento, comComissao })
        {
            Assert.Equal("Profissional em uso — foi desativado em vez de excluído.", (await loja.Painel.Delete($"/api/funcionarios/{id}")).Json.GetProperty("mensagem").GetString());
            Assert.False((await NoBanco(id)).Ativo);
        }

        Assert.Equal(404, (await loja.Painel.Delete($"/api/funcionarios/{livre}")).Codigo);
        Assert.Equal(404, (await outra.Painel.Delete($"/api/funcionarios/{comAgendamento}")).Codigo);
    }

    // ── Comissão por serviço ─────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Comissao_por_servico_define_atualiza_e_remove()
    {
        var loja = await NovaAsync();
        var id = await loja.FuncionarioIdAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80, 60);

        var criada = await loja.Painel.Post($"/api/funcionarios/{id}/comissoes-servico", new { servicoId = servico, comissaoPercentual = 25 });
        Assert.Equal("Comissão do serviço definida.", criada.Json.GetProperty("mensagem").GetString());
        await loja.Painel.Post($"/api/funcionarios/{id}/comissoes-servico", new { servicoId = servico, comissaoPercentual = 30 });   // atualiza a mesma

        var linhas = await api.NoBancoAsync(db => db.ComissoesServicoProfissional.AsNoTracking().Where(c => c.ProfissionalId == id).ToListAsync());
        var linha = Assert.Single(linhas);
        Assert.Equal((30m, servico, loja.Id), (linha.ComissaoPercentual, linha.ServicoId, linha.LojaId));

        var excecoes = (await loja.Painel.Get("/api/funcionarios")).Json[0].GetProperty("comissoesPorServico");
        Assert.Equal((linha.Id, servico, 30m), (excecoes[0].GetProperty("id").GetGuid(), excecoes[0].GetProperty("servicoId").GetGuid(), excecoes[0].GetProperty("comissaoPercentual").GetDecimal()));

        var removida = await loja.Painel.Delete($"/api/funcionarios/comissoes-servico/{linha.Id}");
        Assert.Equal("Exceção removida — volta a usar a comissão padrão do profissional.", removida.Json.GetProperty("mensagem").GetString());
        Assert.Equal(0, (await loja.Painel.Get("/api/funcionarios")).Json[0].GetProperty("comissoesPorServico").GetArrayLength());
        Assert.Equal(404, (await loja.Painel.Delete($"/api/funcionarios/comissoes-servico/{linha.Id}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Comissao_por_servico_so_aceita_profissional_e_servico_da_propria_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var profissionalA = await a.FuncionarioIdAsync();
        var servicoA = await a.ServicoIdAsync();
        var profissionalB = await b.FuncionarioIdAsync();

        Assert.Equal(404, (await b.Painel.Post($"/api/funcionarios/{profissionalA}/comissoes-servico", new { servicoId = servicoA, comissaoPercentual = 5 })).Codigo);
        var servicoAlheio = await b.Painel.Post($"/api/funcionarios/{profissionalB}/comissoes-servico", new { servicoId = servicoA, comissaoPercentual = 5 });
        Assert.Equal((400, "Serviço não encontrado."), (servicoAlheio.Codigo, servicoAlheio.Erro));

        await a.Painel.Post($"/api/funcionarios/{profissionalA}/comissoes-servico", new { servicoId = servicoA, comissaoPercentual = 5 });
        var linha = await api.NoBancoAsync(db => db.ComissoesServicoProfissional.AsNoTracking().SingleAsync(c => c.ProfissionalId == profissionalA));
        Assert.Equal(404, (await b.Painel.Delete($"/api/funcionarios/comissoes-servico/{linha.Id}")).Codigo);   // outra loja não remove a exceção
        Assert.Equal(1, await api.NoBancoAsync(db => db.ComissoesServicoProfissional.CountAsync(c => c.ProfissionalId == profissionalA)));
    }

    // ── Listas ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_vem_em_ordem_de_nome_so_da_loja_e_ativos_esconde_os_desativados()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        await a.FuncionarioIdAsync("Zeca");
        var ana = await a.FuncionarioIdAsync("Ana");
        var bia = await a.FuncionarioIdAsync("Bia");
        await b.FuncionarioIdAsync("Da B");
        await a.Painel.Patch($"/api/funcionarios/{bia}/ativo");

        var todos = (await a.Painel.Get("/api/funcionarios")).Json.EnumerateArray().ToList();
        Assert.Equal(["Ana", "Bia", "Zeca"], todos.Select(p => p.GetProperty("nome").GetString()).ToList());
        Assert.Equal(ana, todos[0].GetProperty("id").GetGuid());
        Assert.False(todos[1].GetProperty("ativo").GetBoolean());

        var ativos = (await a.Painel.Get("/api/funcionarios/ativos")).Json.EnumerateArray().ToList();
        Assert.Equal(["Ana", "Zeca"], ativos.Select(p => p.GetProperty("nome").GetString()).ToList());
        Assert.Equal(2, ativos[0].EnumerateObject().Count());   // só id e nome
    }
}
