using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using LojaApi.src.Models.OrdemServico;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.EquipeTeste;

// Comissões dos funcionários: resumo, detalhe por origem (agendamento, venda, ordem de serviço), fechar/pagar, histórico e desfazer.
[Collection(ColecaoApi.Nome)]
public class ComissoesApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static string Hoje(int dias = 0) => DateTime.UtcNow.Date.AddDays(dias).ToString("yyyy-MM-dd");

    private static object Fechar(Guid profissional, int qtdDiarias = 0, Guid? conta = null, string? vencimento = null, int inicioEmDias = -1, int fimEmDias = 1) =>
        new { profissionalId = profissional, periodoInicio = Hoje(inicioEmDias), periodoFim = Hoje(fimEmDias), contaBancariaId = conta, vencimento, qtdDiarias };

    private Task<FechamentoComissao> FechamentoNoBanco(Guid id) => api.NoBancoAsync(db => db.FechamentosComissao.AsNoTracking().FirstAsync(f => f.Id == id));

    // loja com 1 serviço de R$ 100 e um profissional com 10% de comissão
    private async Task<(LojaTeste Loja, Guid Servico, Guid Profissional)> CenarioAsync(string nome = "Joana", decimal comissao = 10m, string tipo = "comissao", decimal? diaria = null, int? dia = null)
    {
        var loja = await NovaAsync();
        return (loja, await loja.ServicoIdAsync("Banho", 100, 60), await loja.FuncionarioIdAsync(nome, comissao, tipo, diaria: diaria, dia: dia));
    }

    // ── Permissões ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_fecha_ou_desfaz_o_fechamento()
    {
        var (loja, _, profissional) = await CenarioAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/comissoes/resumo")).Codigo);

        var operador = await api.OperadorAsync(loja);
        Assert.Equal(200, (await operador.Get("/api/comissoes/resumo")).Codigo);
        Assert.Equal(200, (await operador.Get($"/api/comissoes/profissional/{profissional}")).Codigo);
        Assert.Equal(200, (await operador.Get("/api/comissoes/fechamentos")).Codigo);
        Assert.Equal(403, (await operador.Post("/api/comissoes/fechar", Fechar(profissional))).Codigo);
        Assert.Equal(403, (await operador.Post($"/api/comissoes/fechamentos/{Guid.NewGuid()}/desfazer")).Codigo);
    }

    // ── Resumo ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Resumo_soma_so_as_pendentes_por_profissional_do_maior_para_o_menor()
    {
        var loja = await NovaAsync();
        var servico = await loja.ServicoIdAsync("Banho", 100, 60);
        var ana = await loja.FuncionarioIdAsync("Ana", 10m);
        var bia = await loja.FuncionarioIdAsync("Bia", 10m, "diaria", diaria: 120, dia: 7);
        await loja.ComissaoDeAgendamentoAsync(servico, ana, 100, hora: 9);    // 10
        await loja.ComissaoDeAgendamentoAsync(servico, ana, 100, hora: 11);   // 10
        await loja.ComissaoDeAgendamentoAsync(servico, bia, 300, hora: 13);   // 30
        var jaPaga = await loja.ComissaoDeAgendamentoAsync(servico, bia, 1000, hora: 15);
        await api.NoBancoAsync(db => db.ComissoesFuncionario.Where(c => c.OrigemId == jaPaga).ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, "pago")));

        var resumo = (await loja.Painel.Get("/api/comissoes/resumo")).Json.EnumerateArray().ToList();

        Assert.Equal(["Bia", "Ana"], resumo.Select(r => r.GetProperty("profissionalNome").GetString()).ToList());   // 30 antes de 20
        Assert.Equal((bia, 1, 30m, "diaria", 120m, 7), (resumo[0].GetProperty("profissionalId").GetGuid(), resumo[0].GetProperty("qtdAtendimentos").GetInt32(),
            resumo[0].GetProperty("valorTotal").GetDecimal(), resumo[0].GetProperty("tipoRemuneracao").GetString(), resumo[0].GetProperty("valorDiaria").GetDecimal(),
            resumo[0].GetProperty("diaPagamentoPadrao").GetInt32()));
        Assert.Equal((2, 20m, "comissao"), (resumo[1].GetProperty("qtdAtendimentos").GetInt32(), resumo[1].GetProperty("valorTotal").GetDecimal(), resumo[1].GetProperty("tipoRemuneracao").GetString()));
    }

    [FatoIntegracao]
    public async Task Resumo_filtra_pelo_periodo_de_criacao_da_comissao()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        await loja.ComissaoDeAgendamentoAsync(servico, profissional);

        async Task<int> Qtd(string query) => (await loja.Painel.Get("/api/comissoes/resumo" + query)).Json.GetArrayLength();
        // Datas completas em UTC, com folga de dias para o teste não depender do fuso horário da máquina
        static string Utc(int dias) => Uri.EscapeDataString($"{Hoje(dias)}T00:00:00Z");

        Assert.Equal(1, await Qtd(""));
        Assert.Equal(1, await Qtd($"?de={Utc(-3)}&ate={Utc(3)}"));
        Assert.Equal(0, await Qtd($"?de={Utc(3)}"));      // começa só daqui a 3 dias
        Assert.Equal(0, await Qtd($"?ate={Utc(-3)}"));    // terminou há 3 dias
    }

    // Limites do resumo: o `de` vale desde 00:00 do dia e o `ate` cobre o dia inteiro (até 00:00 do dia seguinte, inclusive)
    [FatoIntegracao]
    public async Task Resumo_com_de_e_ate_no_mesmo_dia_inclui_as_comissoes_daquele_dia_inteiro()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        var dia = DateTime.UtcNow.Date.AddDays(-20);
        var momentos = new Dictionary<decimal, DateTime>
        {
            [101] = dia.AddMinutes(-1),      // dia anterior: fora
            [102] = dia,                     // 00:00 exato: dentro (de é inclusivo)
            [103] = dia.AddHours(23).AddMinutes(59),   // fim do dia: dentro
            [104] = dia.AddDays(1),          // 00:00 do dia seguinte: dentro (ate é inclusivo, como era)
            [105] = dia.AddDays(1).AddMinutes(1),      // depois disso: fora
        };
        foreach (var (valor, quando) in momentos)
        {
            await loja.ComissaoDeAgendamentoAsync(servico, profissional, valor, hora: 9 + (int)(valor - 101));
            await api.NoBancoAsync(db => db.ComissoesFuncionario.Where(c => c.ValorServico == valor && c.LojaId == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.CriadoEm, quando)));
        }
        // hora do meio do dia nos dois filtros: o de e o ate são truncados para o dia
        var meioDia = Uri.EscapeDataString($"{dia:yyyy-MM-dd}T12:00:00Z");

        var r = (await loja.Painel.Get($"/api/comissoes/resumo?de={meioDia}&ate={meioDia}")).Json.EnumerateArray().Single();

        Assert.Equal(3, r.GetProperty("qtdAtendimentos").GetInt32());   // 102, 103 e 104
        Assert.Equal(Math.Round((102m + 103m + 104m) * 0.10m, 2), r.GetProperty("valorTotal").GetDecimal());
    }

    // ── Detalhe por origem ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Detalhe_de_comissao_de_agendamento_traz_servico_cliente_e_item()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        var agendamento = await loja.ComissaoDeAgendamentoAsync(servico, profissional, 200, "Dona Maria");

        var lista = (await loja.Painel.Get($"/api/comissoes/profissional/{profissional}")).Json;

        var c = Assert.Single(lista.EnumerateArray());
        Assert.Equal((200m, 10m, 20m, "pendente", "Banho", "Dona Maria", "agendamento"), (c.GetProperty("valorServico").GetDecimal(), c.GetProperty("comissaoPercentual").GetDecimal(),
            c.GetProperty("valorComissao").GetDecimal(), c.GetProperty("status").GetString(), c.GetProperty("nomeServico").GetString(), c.GetProperty("nomeCliente").GetString(), c.GetProperty("tipo").GetString()));
        var item = Assert.Single(c.GetProperty("itens").EnumerateArray());
        Assert.Equal(("Banho", 1m, 200m, "servico"), (item.GetProperty("descricao").GetString(), item.GetProperty("quantidade").GetDecimal(), item.GetProperty("valor").GetDecimal(), item.GetProperty("tipo").GetString()));
        Assert.Equal(DateTime.Parse($"{Datas.Texto(Datas.Segunda())}T09:00:00"), c.GetProperty("dataAtendimento").GetDateTime());
        Assert.NotEqual(Guid.Empty, agendamento);
    }

    [FatoIntegracao]
    public async Task Detalhe_filtra_por_status_e_vem_da_mais_nova_para_a_mais_antiga()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 100, hora: 9);
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 300, hora: 11);
        await api.NoBancoAsync(db => db.ComissoesFuncionario.Where(c => c.ValorServico == 100).ExecuteUpdateAsync(s => s.SetProperty(c => c.CriadoEm, DateTime.UtcNow.AddHours(-5))));
        await api.NoBancoAsync(db => db.ComissoesFuncionario.Where(c => c.ValorServico == 300).ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, "pago")));

        var pendentes = (await loja.Painel.Get($"/api/comissoes/profissional/{profissional}")).Json;                    // padrão: pendente
        var pagas = (await loja.Painel.Get($"/api/comissoes/profissional/{profissional}?status=pago")).Json;

        Assert.Equal([100m], pendentes.EnumerateArray().Select(c => c.GetProperty("valorServico").GetDecimal()).ToList());
        Assert.Equal([300m], pagas.EnumerateArray().Select(c => c.GetProperty("valorServico").GetDecimal()).ToList());
    }

    [FatoIntegracao]
    public async Task Detalhe_de_comissao_de_venda_e_de_ordem_de_servico_resolve_cliente_e_itens()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        var cliente = await loja.ClienteIdAsync("Seu João", "67955550000");
        var venda = new Venda { LojaId = loja.Id, ClienteId = cliente, Total = 150, TotalFinal = 150, FormaPagamento = "pix" };
        var orcamento = new OrcamentoServico { LojaId = loja.Id, ClienteId = cliente, VeiculoDescricao = "Gol 2015", Placa = "ABC1D23", ValorTotal = 500, ConcluidoEm = DateTime.UtcNow.AddHours(-2) };
        await api.NoBancoAsync(async db =>
        {
            db.Vendas.Add(venda);
            db.OrcamentosServico.Add(orcamento);
            await db.SaveChangesAsync();
            db.ItensVenda.Add(new ItemVenda { VendaId = venda.Id, ServicoId = servico, NomeProduto = "Banho", Quantidade = 1, PrecoUnitario = 100, Subtotal = 100 });
            db.ItensVenda.Add(new ItemVenda { VendaId = venda.Id, NomeProduto = "Shampoo", Quantidade = 2, PrecoUnitario = 25, Subtotal = 50 });
            db.ItensOrcamentoServico.Add(new ItemOrcamentoServico { LojaId = loja.Id, OrcamentoId = orcamento.Id, Tipo = "peca", Descricao = "Filtro", Quantidade = 2, ValorUnitario = 100, ValorTotal = 200 });
            db.ItensOrcamentoServico.Add(new ItemOrcamentoServico { LojaId = loja.Id, OrcamentoId = orcamento.Id, Tipo = "servico", Descricao = "Mão de obra", Quantidade = 1, ValorUnitario = 300, ValorTotal = 300 });
            db.ComissoesFuncionario.Add(new ComissaoFuncionario { LojaId = loja.Id, ProfissionalId = profissional, OrigemTipo = "venda", OrigemId = venda.Id, ValorServico = 150, ComissaoPercentual = 10, ValorComissao = 15, CriadoEm = DateTime.UtcNow.AddHours(-1) });
            db.ComissoesFuncionario.Add(new ComissaoFuncionario { LojaId = loja.Id, ProfissionalId = profissional, OrigemTipo = "ordem_servico", OrigemId = orcamento.Id, ValorServico = 500, ComissaoPercentual = 10, ValorComissao = 50, CriadoEm = DateTime.UtcNow.AddHours(-3) });
            await db.SaveChangesAsync();
        });

        var lista = (await loja.Painel.Get($"/api/comissoes/profissional/{profissional}")).Json.EnumerateArray().ToList();

        var daVenda = lista.Single(c => c.GetProperty("tipo").GetString() == "venda");
        Assert.Equal(("Venda", "Seu João"), (daVenda.GetProperty("nomeServico").GetString(), daVenda.GetProperty("nomeCliente").GetString()));
        Assert.Equal([("Banho", 1m, 100m, "servico"), ("Shampoo", 2m, 50m, "produto")],
            daVenda.GetProperty("itens").EnumerateArray().Select(i => (i.GetProperty("descricao").GetString()!, i.GetProperty("quantidade").GetDecimal(), i.GetProperty("valor").GetDecimal(), i.GetProperty("tipo").GetString()!)).OrderBy(i => i.Item1).ToList());

        var daOs = lista.Single(c => c.GetProperty("tipo").GetString() == "ordem_servico");
        Assert.Equal(("Ordem de Serviço — Gol 2015 · ABC1D23", "Seu João"), (daOs.GetProperty("nomeServico").GetString(), daOs.GetProperty("nomeCliente").GetString()));
        Assert.Equal([("Filtro", 2m, 200m, "peca"), ("Mão de obra", 1m, 300m, "servico")],
            daOs.GetProperty("itens").EnumerateArray().Select(i => (i.GetProperty("descricao").GetString()!, i.GetProperty("quantidade").GetDecimal(), i.GetProperty("valor").GetDecimal(), i.GetProperty("tipo").GetString()!)).OrderBy(i => i.Item1).ToList());
        Assert.Equal(["venda", "ordem_servico"], lista.Select(c => c.GetProperty("tipo").GetString()).ToList());   // a mais nova primeiro
    }

    // ── Fechar ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Fechar_valida_profissional_diarias_e_se_ha_o_que_pagar()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        var semDiaria = profissional;

        var inexistente = await loja.Painel.Post("/api/comissoes/fechar", Fechar(Guid.NewGuid()));
        Assert.Equal((404, "Profissional não encontrado."), (inexistente.Codigo, inexistente.Erro));
        Assert.Equal("Quantidade de diárias inválida.", (await loja.Painel.Post("/api/comissoes/fechar", Fechar(semDiaria, qtdDiarias: -1))).Erro);
        Assert.Equal("Este profissional não tem valor de diária cadastrado.", (await loja.Painel.Post("/api/comissoes/fechar", Fechar(semDiaria, qtdDiarias: 2))).Erro);

        var vazio = await loja.Painel.Post("/api/comissoes/fechar", Fechar(semDiaria));
        Assert.Equal((400, "Nenhuma comissão pendente encontrada nesse período, e nenhuma diária informada."), (vazio.Codigo, vazio.Erro));
        Assert.Equal(0, await api.NoBancoAsync(db => db.FechamentosComissao.CountAsync(f => f.LojaId == loja.Id)));
        Assert.NotEqual(Guid.Empty, servico);
    }

    [FatoIntegracao]
    public async Task Fechar_sem_conta_marca_as_comissoes_como_pagas_e_nao_cria_lancamento()
    {
        var (loja, servico, profissional) = await CenarioAsync("Joana");
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 100, hora: 9);
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 200, hora: 11);

        var r = await loja.Painel.Post("/api/comissoes/fechar", Fechar(profissional));

        Assert.Equal(200, r.Codigo);
        Assert.Equal((30m, 2, 0, 0m, "Joana"), (r.Json.GetProperty("valorTotal").GetDecimal(), r.Json.GetProperty("qtdAtendimentos").GetInt32(),
            r.Json.GetProperty("qtdDiarias").GetInt32(), r.Json.GetProperty("valorDiarias").GetDecimal(), r.Json.GetProperty("profissionalNome").GetString()));
        var fechamentoId = r.Json.GetProperty("id").GetGuid();
        var comissoes = await api.ComissoesDoProfissionalAsync(profissional);
        Assert.All(comissoes, c => Assert.Equal(("pago", fechamentoId), (c.Status, c.FechamentoId!.Value)));
        Assert.All(comissoes, c => Assert.NotNull(c.PagoEm));
        Assert.Null((await FechamentoNoBanco(fechamentoId)).LancamentoFinanceiroId);
        Assert.Equal(0, await api.NoBancoAsync(db => db.LancamentosFinanceiros.CountAsync(l => l.LojaId == loja.Id)));
        Assert.Equal(0, (await loja.Painel.Get("/api/comissoes/resumo")).Json.GetArrayLength());   // nada mais pendente
    }

    [FatoIntegracao]
    public async Task Fechar_com_conta_e_diarias_cria_a_conta_a_pagar_com_o_total()
    {
        var (loja, servico, profissional) = await CenarioAsync("Diarista", 10m, "diaria", diaria: 100);
        var conta = await loja.ContaBancariaIdAsync();
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 200, hora: 9);   // 20 de comissão

        var r = await loja.Painel.Post("/api/comissoes/fechar", Fechar(profissional, qtdDiarias: 2, conta: conta));

        Assert.Equal((220m, 1, 2, 200m), (r.Json.GetProperty("valorTotal").GetDecimal(), r.Json.GetProperty("qtdAtendimentos").GetInt32(), r.Json.GetProperty("qtdDiarias").GetInt32(), r.Json.GetProperty("valorDiarias").GetDecimal()));
        var fechamento = await FechamentoNoBanco(r.Json.GetProperty("id").GetGuid());
        var lancamento = await api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().SingleAsync(l => l.LojaId == loja.Id));
        Assert.Equal(lancamento.Id, fechamento.LancamentoFinanceiroId);
        Assert.Equal(("pagar", "avulsa", "pendente", 220m, conta), (lancamento.Tipo, lancamento.Modo, lancamento.Status, lancamento.Valor, lancamento.ContaBancariaId));
        Assert.Equal($"Comissão + 2 diária(s) — Diarista ({DateTime.UtcNow.Date.AddDays(-1):dd/MM} a {DateTime.UtcNow.Date.AddDays(1):dd/MM})", lancamento.Descricao);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(1 + 5).AddHours(12), lancamento.Vencimento);   // fim do período + 5 dias, ao meio-dia
        var categoria = await api.NoBancoAsync(db => db.CategoriasFinanceiras.AsNoTracking().SingleAsync(c => c.LojaId == loja.Id && c.Nome == "Comissões de Funcionários"));
        Assert.Equal(categoria.Id, lancamento.CategoriaId);
    }

    [FatoIntegracao]
    public async Task Fechar_so_com_diarias_e_vencimento_informado()
    {
        var (loja, _, profissional) = await CenarioAsync("Diarista", 10m, "diaria", diaria: 80);
        var conta = await loja.ContaBancariaIdAsync();

        var r = await loja.Painel.Post("/api/comissoes/fechar", Fechar(profissional, qtdDiarias: 3, conta: conta, vencimento: Hoje(10)));

        Assert.Equal((240m, 0, 3), (r.Json.GetProperty("valorTotal").GetDecimal(), r.Json.GetProperty("qtdAtendimentos").GetInt32(), r.Json.GetProperty("qtdDiarias").GetInt32()));
        var lancamento = await api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().SingleAsync(l => l.LojaId == loja.Id));
        Assert.Equal(DateTime.UtcNow.Date.AddDays(10).AddHours(12), lancamento.Vencimento);
        Assert.StartsWith("Comissão + 3 diária(s) — Diarista", lancamento.Descricao);
    }

    [FatoIntegracao]
    public async Task Fechar_leva_so_as_comissoes_pendentes_do_periodo_e_reaproveita_a_categoria()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        var conta = await loja.ContaBancariaIdAsync();
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 100, hora: 9);
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 500, hora: 11);
        await api.NoBancoAsync(db => db.ComissoesFuncionario.Where(c => c.ValorServico == 500).ExecuteUpdateAsync(s => s.SetProperty(c => c.CriadoEm, DateTime.UtcNow.AddDays(-30))));   // fora do período

        var primeiro = await loja.Painel.Post("/api/comissoes/fechar", Fechar(profissional, conta: conta));
        Assert.Equal((10m, 1), (primeiro.Json.GetProperty("valorTotal").GetDecimal(), primeiro.Json.GetProperty("qtdAtendimentos").GetInt32()));
        Assert.Equal("pendente", (await api.ComissoesDoProfissionalAsync(profissional)).Single(c => c.ValorServico == 500).Status);

        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 100, hora: 13);
        (await loja.Painel.Post("/api/comissoes/fechar", Fechar(profissional, conta: conta))).DeveSerSucesso();
        Assert.Equal(1, await api.NoBancoAsync(db => db.CategoriasFinanceiras.CountAsync(c => c.LojaId == loja.Id && c.Nome == "Comissões de Funcionários")));
    }

    [FatoIntegracao]
    public async Task Fechar_inclui_o_primeiro_instante_do_periodo_e_exclui_o_instante_depois_do_fim()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        var inicio = DateTime.UtcNow.Date.AddDays(-1);        // Fechar(): de ontem...
        var fimExclusivo = DateTime.UtcNow.Date.AddDays(2);   // ...até hoje+1, valendo o dia inteiro
        var momentos = new Dictionary<decimal, DateTime>
        {
            [101] = inicio.AddMinutes(-1),           // antes do período: fica pendente
            [102] = inicio,                          // 00:00 do primeiro dia: entra
            [103] = fimExclusivo.AddMinutes(-1),     // 23:59 do último dia: entra
            [104] = fimExclusivo,                    // 00:00 do dia seguinte: fica pendente
        };
        foreach (var (valor, quando) in momentos)
        {
            await loja.ComissaoDeAgendamentoAsync(servico, profissional, valor, hora: 9 + (int)(valor - 101));
            await api.NoBancoAsync(db => db.ComissoesFuncionario.Where(c => c.ValorServico == valor && c.LojaId == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(c => c.CriadoEm, quando)));
        }

        var r = await loja.Painel.Post("/api/comissoes/fechar", Fechar(profissional));

        Assert.Equal(2, r.Json.GetProperty("qtdAtendimentos").GetInt32());
        var porValor = (await api.ComissoesDoProfissionalAsync(profissional)).ToDictionary(c => c.ValorServico, c => c.Status);
        Assert.Equal(("pendente", "pago", "pago", "pendente"), (porValor[101], porValor[102], porValor[103], porValor[104]));
    }

    [FatoIntegracao]
    public async Task Fechar_nao_leva_comissao_de_outra_loja_mesmo_com_o_mesmo_profissional_no_registro()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        var outra = await NovaAsync();
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 100);
        await api.NoBancoAsync(async db =>
        {
            db.ComissoesFuncionario.Add(new ComissaoFuncionario { LojaId = outra.Id, ProfissionalId = profissional, OrigemId = Guid.NewGuid(), ValorServico = 900, ComissaoPercentual = 10, ValorComissao = 90 });
            await db.SaveChangesAsync();
        });

        var r = await loja.Painel.Post("/api/comissoes/fechar", Fechar(profissional));

        Assert.Equal((10m, 1), (r.Json.GetProperty("valorTotal").GetDecimal(), r.Json.GetProperty("qtdAtendimentos").GetInt32()));
        var daOutra = await api.NoBancoAsync(db => db.ComissoesFuncionario.AsNoTracking().SingleAsync(c => c.LojaId == outra.Id));
        Assert.Equal(("pendente", null), (daOutra.Status, daOutra.FechamentoId?.ToString()));
    }

    // Correção desta migração: antes, a conta bancária de OUTRA loja era aceita e a conta a pagar ia para ela.
    [FatoIntegracao]
    public async Task Fechar_nao_aceita_conta_bancaria_de_outra_loja()
    {
        var (a, _, _) = await CenarioAsync();
        var (b, servico, profissionalB) = await CenarioAsync();
        var contaDeA = await a.ContaBancariaIdAsync();
        await b.ComissaoDeAgendamentoAsync(servico, profissionalB, 100);

        var r = await b.Painel.Post("/api/comissoes/fechar", Fechar(profissionalB, conta: contaDeA));

        Assert.Equal((400, "Conta bancária não encontrada."), (r.Codigo, r.Erro));
        Assert.Equal(0, await api.NoBancoAsync(db => db.FechamentosComissao.CountAsync(f => f.LojaId == b.Id)));
        Assert.Equal("pendente", (await api.ComissoesDoProfissionalAsync(profissionalB)).Single().Status);
        Assert.Equal(0, await api.NoBancoAsync(db => db.LancamentosFinanceiros.CountAsync(l => l.ContaBancariaId == contaDeA)));
    }

    // ── Histórico e desfazer ─────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Fechamentos_vem_do_mais_recente_e_filtra_por_profissional()
    {
        var loja = await NovaAsync();
        var servico = await loja.ServicoIdAsync("Banho", 100, 60);
        var ana = await loja.FuncionarioIdAsync("Ana", 10m);
        var bia = await loja.FuncionarioIdAsync("Bia", 10m);
        await loja.ComissaoDeAgendamentoAsync(servico, ana, 100, hora: 9);
        await loja.ComissaoDeAgendamentoAsync(servico, bia, 200, hora: 11);
        var daAna = (await loja.Painel.Post("/api/comissoes/fechar", Fechar(ana))).Json.GetProperty("id").GetGuid();
        var daBia = (await loja.Painel.Post("/api/comissoes/fechar", Fechar(bia))).Json.GetProperty("id").GetGuid();
        await api.NoBancoAsync(db => db.FechamentosComissao.Where(f => f.Id == daAna).ExecuteUpdateAsync(s => s.SetProperty(f => f.PagoEm, DateTime.UtcNow.AddHours(-3))));

        var todos = (await loja.Painel.Get("/api/comissoes/fechamentos")).Json.EnumerateArray().ToList();
        var soAna = (await loja.Painel.Get($"/api/comissoes/fechamentos?profissionalId={ana}")).Json.EnumerateArray().ToList();

        Assert.Equal([daBia, daAna], todos.Select(f => f.GetProperty("id").GetGuid()).ToList());
        Assert.Equal(("Bia", 20m, 1), (todos[0].GetProperty("profissionalNome").GetString(), todos[0].GetProperty("valorTotal").GetDecimal(), todos[0].GetProperty("qtdAtendimentos").GetInt32()));
        Assert.Equal(["Ana"], soAna.Select(f => f.GetProperty("profissionalNome").GetString()).ToList());
    }

    [FatoIntegracao]
    public async Task Desfazer_volta_as_comissoes_para_pendente_e_apaga_o_lancamento()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        var conta = await loja.ContaBancariaIdAsync();
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 100);
        var fechamento = (await loja.Painel.Post("/api/comissoes/fechar", Fechar(profissional, conta: conta))).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Post($"/api/comissoes/fechamentos/{fechamento}/desfazer");

        Assert.Equal("Fechamento desfeito. As comissões voltaram para pendente.", r.Json.GetProperty("mensagem").GetString());
        var c = Assert.Single(await api.ComissoesDoProfissionalAsync(profissional));
        Assert.Equal(("pendente", null, null), (c.Status, c.PagoEm?.ToString(), c.FechamentoId?.ToString()));
        Assert.Equal(0, await api.NoBancoAsync(db => db.FechamentosComissao.CountAsync(f => f.Id == fechamento)));
        Assert.Equal(0, await api.NoBancoAsync(db => db.LancamentosFinanceiros.CountAsync(l => l.LojaId == loja.Id)));
        Assert.Equal(404, (await loja.Painel.Post($"/api/comissoes/fechamentos/{fechamento}/desfazer")).Codigo);
    }

    [FatoIntegracao]
    public async Task Desfazer_e_recusado_se_o_lancamento_ja_foi_pago_no_financeiro()
    {
        var (loja, servico, profissional) = await CenarioAsync();
        var conta = await loja.ContaBancariaIdAsync();
        await loja.ComissaoDeAgendamentoAsync(servico, profissional, 100);
        var fechamento = (await loja.Painel.Post("/api/comissoes/fechar", Fechar(profissional, conta: conta))).Json.GetProperty("id").GetGuid();
        await api.NoBancoAsync(db => db.LancamentosFinanceiros.Where(l => l.LojaId == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, "pago")));

        var r = await loja.Painel.Post($"/api/comissoes/fechamentos/{fechamento}/desfazer");

        Assert.Equal((400, "Essa comissão já foi paga no Financeiro. Estorne o lançamento lá antes de desfazer o fechamento."), (r.Codigo, r.Erro));
        Assert.Equal(1, await api.NoBancoAsync(db => db.FechamentosComissao.CountAsync(f => f.Id == fechamento)));
        Assert.Equal("pago", (await api.ComissoesDoProfissionalAsync(profissional)).Single().Status);
    }

    // ── Isolamento entre lojas ───────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Uma_loja_nao_ve_nem_fecha_comissoes_de_outra()
    {
        var (a, servico, profissionalA) = await CenarioAsync();
        var b = await NovaAsync();
        await a.ComissaoDeAgendamentoAsync(servico, profissionalA, 100);
        var fechamento = (await a.Painel.Post("/api/comissoes/fechar", Fechar(profissionalA))).Json.GetProperty("id").GetGuid();
        await a.ComissaoDeAgendamentoAsync(servico, profissionalA, 100, hora: 11);

        Assert.Equal(0, (await b.Painel.Get("/api/comissoes/resumo")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get($"/api/comissoes/profissional/{profissionalA}")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get("/api/comissoes/fechamentos")).Json.GetArrayLength());
        Assert.Equal(404, (await b.Painel.Post("/api/comissoes/fechar", Fechar(profissionalA))).Codigo);
        Assert.Equal(404, (await b.Painel.Post($"/api/comissoes/fechamentos/{fechamento}/desfazer")).Codigo);
        Assert.Equal("pendente", (await api.ComissoesDoProfissionalAsync(profissionalA)).Last().Status);
    }
}
