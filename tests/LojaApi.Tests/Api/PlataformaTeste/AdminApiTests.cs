using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using static LojaApi.Tests.Suporte.CenariosPlataforma;

namespace LojaApi.Tests.Api.PlataformaTeste;

// Painel do superadmin: dashboard, lojas (criar, listar, editar, status, plano, e-mail, valor, trial), pagamentos manuais,
// bloqueios, comunicados, acesso como suporte, backup e exclusão.
[Collection(ColecaoApi.Nome)]
public class AdminApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    private static object NovaLoja(string nome, string email, string? cnpj = null, string adminEmail = "", string? perfilId = null,
        string? modulos = null, bool ehTeste = false, decimal valor = 99.90m) =>
        new
        {
            nome, email, cnpj, cpf = (string?)null, telefone = "67999990000", endereco = "Rua X, 1", corPrimaria = "#112233",
            mensalidadeDia = 10, mensalidadeValor = valor, adminNome = "Admin Novo", adminEmail = adminEmail == "" ? "adm-" + email : adminEmail,
            adminSenha = "senha123", perfilId, tipoPlano = (string?)null, modulosAtivos = modulos, ehTeste,
        };

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..10];

    // Cria uma loja pelo superadmin e devolve o id e o e-mail do admin dela
    private async Task<(Guid Id, string AdminEmail, string Nome)> CriarLojaAsync(ClienteApi superadmin, string? perfilId = null, string? modulos = null, bool ehTeste = false, string? cnpj = null)
    {
        var s = Sufixo();
        var nome = $"Loja Admin {s}";
        var r = await superadmin.Post("/api/admin/lojas", NovaLoja(nome, $"loja-{s}@teste.com", cnpj: cnpj, perfilId: perfilId, modulos: modulos, ehTeste: ehTeste));
        r.DeveSerSucesso();
        return (r.Json.GetProperty("id").GetGuid(), $"adm-loja-{s}@teste.com", nome);
    }

    private Task InserirProdutoAsync(Guid lojaId, string nome) => api.NoBancoAsync(async db =>
    {
        db.Produtos.Add(new LojaApi.Models.Produto { LojaId = lojaId, Nome = nome, Categoria = "Teste", PrecoCusto = 1m, PrecoVenda = 2m, Estoque = 3m });
        await db.SaveChangesAsync();
    });

    private async Task<ClienteApi> SuperAsync()
    {
        var loja = await NovaAsync();
        return await api.SuperadminAsync(loja);
    }

    // ── Papéis ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task So_o_superadmin_acessa_o_painel()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var id = loja.Id;

        foreach (var url in new[] { "dashboard", "lojas", $"lojas/{id}", $"lojas/{id}/pagamentos", "comunicados", $"lojas/{id}/backup" })
        {
            Assert.Equal(401, (await loja.Site.Get($"/api/admin/{url}")).Codigo);
            Assert.Equal(403, (await loja.Painel.Get($"/api/admin/{url}")).Codigo);                                      // admin da loja
            Assert.Equal(403, (await operador.Get($"/api/admin/{url}")).Codigo);
        }
        Assert.Equal(403, (await loja.Painel.Post("/api/admin/lojas", new { })).Codigo);
        Assert.Equal(403, (await loja.Painel.Delete($"/api/admin/lojas/{id}")).Codigo);
        Assert.Equal(403, (await loja.Painel.Post($"/api/admin/lojas/{id}/acessar")).Codigo);
        Assert.Equal(403, (await loja.Painel.Post("/api/admin/verificar-bloqueios")).Codigo);
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Dashboard_conta_as_lojas_reais_e_ignora_as_de_teste()
    {
        var super = await SuperAsync();
        var antes = (await super.Get("/api/admin/dashboard")).Json;
        Assert.Equal(["totalLojas", "lojasAtivas", "lojasTrial", "lojasBloqueadas", "lojasEmAtraso", "receitaMensal", "receitaTotal", "lojasAtrasadas", "ultimosPagamentos", "projecaoMensal"],
            antes.EnumerateObject().Select(x => x.Name));

        await CriarLojaAsync(super);
        await CriarLojaAsync(super, ehTeste: true);                                                                     // loja de teste não entra na conta

        var depois = (await super.Get("/api/admin/dashboard")).Json;
        Assert.Equal(antes.GetProperty("totalLojas").GetInt32() + 1, depois.GetProperty("totalLojas").GetInt32());
        Assert.Equal(antes.GetProperty("lojasTrial").GetInt32() + 1, depois.GetProperty("lojasTrial").GetInt32());
        Assert.True(depois.GetProperty("ultimosPagamentos").GetArrayLength() <= 10);
    }

    [FatoIntegracao]
    public async Task Dashboard_lista_loja_ativa_vencida_como_atrasada_e_soma_a_receita_mensal_das_ativas()
    {
        var super = await SuperAsync();
        var (id, _, nome) = await CriarLojaAsync(super);
        var antes = (await super.Get("/api/admin/dashboard")).Json;
        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(l => l.Status, LojaApi.Models.StatusLoja.Ativo).SetProperty(l => l.MensalidadeValor, 50m).SetProperty(l => l.ProximoVencimento, DateTime.UtcNow.AddDays(-3.5))));

        var j = (await super.Get("/api/admin/dashboard")).Json;

        Assert.Equal(antes.GetProperty("lojasAtivas").GetInt32() + 1, j.GetProperty("lojasAtivas").GetInt32());
        Assert.Equal(antes.GetProperty("lojasTrial").GetInt32() - 1, j.GetProperty("lojasTrial").GetInt32());
        Assert.Equal(antes.GetProperty("receitaMensal").GetDecimal() + 50m, j.GetProperty("receitaMensal").GetDecimal());
        var atrasada = j.GetProperty("lojasAtrasadas").EnumerateArray().Single(l => l.GetProperty("id").GetGuid() == id);
        Assert.Equal(("Ativo", true, 3), (atrasada.GetProperty("status").GetString()!, atrasada.GetProperty("emAtraso").GetBoolean(), atrasada.GetProperty("diasAtraso").GetInt32()));
        Assert.Equal(["id", "nome", "email", "status", "proximoVencimento", "mensalidadeValor", "emAtraso", "diasAtraso"], atrasada.EnumerateObject().Select(x => x.Name));
        var projecao = j.GetProperty("projecaoMensal").EnumerateArray().Select(p => p.GetProperty("status").GetString()).ToList();
        Assert.Equal(projecao.OrderByDescending(s => s == "Ativo").ToList(), projecao);                                  // ativas primeiro
        Assert.Contains(j.GetProperty("projecaoMensal").EnumerateArray(), p => p.GetProperty("nome").GetString() == nome);
    }

    // ── Lojas ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_filtra_por_status_e_busca_e_ordena_da_mais_nova_para_a_mais_antiga()
    {
        var super = await SuperAsync();
        var (a, _, nomeA) = await CriarLojaAsync(super, cnpj: "11" + Sufixo());
        var (b, _, nomeB) = await CriarLojaAsync(super);
        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == a).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LojaApi.Models.StatusLoja.Bloqueado)));

        var bloqueadas = (await super.Get("/api/admin/lojas?status=BLOQUEADO")).Json.EnumerateArray().Select(l => l.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(a, bloqueadas);
        Assert.DoesNotContain(b, bloqueadas);

        var todas = (await super.Get("/api/admin/lojas?status=inexistente")).Json.EnumerateArray().Select(l => l.GetProperty("id").GetGuid()).ToList();      // status inválido não filtra
        Assert.Contains(a, todas);
        Assert.Contains(b, todas);
        Assert.True(todas.IndexOf(b) < todas.IndexOf(a));                                                               // b é mais nova

        var porNome = (await super.Get($"/api/admin/lojas?busca={Uri.EscapeDataString(nomeA.ToUpper())}")).Json.EnumerateArray().ToList();
        Assert.Equal([a], porNome.Select(l => l.GetProperty("id").GetGuid()));
        Assert.Equal(["id", "nome", "email", "cnpj", "cpf", "telefone", "endereco", "corPrimaria", "logoUrl", "status", "trialAte", "mensalidadeDia", "mensalidadeValor", "proximoVencimento",
            "ultimaCobranca", "schemaNome", "criadoEm", "totalUsuarios", "totalPago", "emAtraso", "diasAtraso", "promocional", "fase", "diasRestantes", "tipoPlano", "modulosAtivos",
            "ehTeste", "assinaturaStatus", "assinaturaCartaoFinal", "ultimoLoginEm"], porNome[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal(nomeB, (await super.Get($"/api/admin/lojas/{b}")).Json.GetProperty("nome").GetString());
        Assert.Equal(404, (await super.Get($"/api/admin/lojas/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_loja_cria_admin_vinculo_e_primeira_fatura_e_recusa_email_ou_cnpj_repetido()
    {
        var super = await SuperAsync();
        var s = Sufixo();
        var cnpj = "22" + s;
        var r = await super.Post("/api/admin/lojas", NovaLoja($"Loja Criada {s}", $"criada-{s}@teste.com", cnpj: cnpj, valor: 120m));

        Assert.Equal(201, r.Codigo);
        Assert.EndsWith($"/api/admin/lojas/{r.Json.GetProperty("id").GetGuid()}", r.Local);
        var id = r.Json.GetProperty("id").GetGuid();
        var l = await api.LojaNoBancoAsync(id);
        Assert.Equal((LojaApi.Models.StatusLoja.Trial, 120m, "loja", "#112233", false), (l.Status, l.MensalidadeValor, l.TipoPlano, l.CorPrimaria, l.EhTeste));
        Assert.InRange((l.TrialAte - DateTime.UtcNow).TotalDays, 6.9, 7.01);
        Assert.StartsWith("loja_loja_criada_", l.SchemaNome);

        var admin = await api.NoBancoAsync(db => db.UsuariosLoja.AsNoTracking().Include(ul => ul.Usuario).SingleAsync(ul => ul.LojaId == id));
        Assert.Equal(("admin", "admin", $"adm-criada-{s}@teste.com"), (admin.Role, admin.Usuario!.Role, admin.Usuario.Email));
        Assert.True(BCrypt.Net.BCrypt.Verify("senha123", admin.Usuario.SenhaHash));
        var fatura = Assert.Single(await api.PagamentosDaLojaNoBancoAsync(id));
        Assert.Equal(("pendente", 120m, l.TrialAte), (fatura.Status, fatura.Valor, fatura.Vencimento));
        Assert.Equal(200, (await api.Fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = $"adm-criada-{s}@teste.com", senha = "senha123" })).StatusCode.GetHashCode());

        Assert.Equal((409, "E-mail já cadastrado."), await Erro(super.Post("/api/admin/lojas", NovaLoja("Outra", $"CRIADA-{s}@teste.com"))));
        Assert.Equal((409, "CNPJ já cadastrado."), await Erro(super.Post("/api/admin/lojas", NovaLoja("Outra", $"outra-{s}@teste.com", cnpj: cnpj))));
    }

    [FatoIntegracao]
    public async Task Criar_loja_com_perfil_copia_categorias_e_servicos_e_so_define_plano_se_nao_veio_modulos()
    {
        var super = await SuperAsync();
        var perfis = (await api.Fabrica.CreateClient().GetFromJsonAsync<JsonElement>("/api/perfis")).EnumerateArray().ToList();
        var comCategorias = perfis.First(p => p.GetProperty("categorias").GetArrayLength() > 0);
        var perfilId = comCategorias.GetProperty("id").GetString()!;
        var perfil = await api.NoBancoAsync(db => db.PerfisLoja.AsNoTracking().Include(p => p.Categorias).Include(p => p.Servicos).FirstAsync(p => p.Id == Guid.Parse(perfilId)));

        var (semModulos, _, _) = await CriarLojaAsync(super, perfilId: perfilId);
        var l1 = await api.LojaNoBancoAsync(semModulos);
        Assert.Equal(perfil.TipoPlanoAplica, l1.TipoPlano);
        Assert.Equal(perfil.Categorias.Select(c => c.Nome).Order(), await api.NoBancoAsync(db => db.CategoriasLoja.Where(c => c.LojaId == semModulos).Select(c => c.Nome).ToListAsync()) is var cs ? cs.Order() : null);
        Assert.Equal(perfil.Servicos.Select(s => s.Nome).Order(), (await api.NoBancoAsync(db => db.Servicos.Where(s => s.LojaId == semModulos).Select(s => s.Nome).ToListAsync())).Order());
        Assert.Equal(perfil.TipoPlanoAplica is "servicos" or "loja_modulos" ? "servicos" : "", l1.ModulosAtivos);

        var (comModulos, _, _) = await CriarLojaAsync(super, perfilId: perfilId, modulos: "financeiro");
        var l2 = await api.LojaNoBancoAsync(comModulos);
        Assert.Equal(("loja", "financeiro"), (l2.TipoPlano, l2.ModulosAtivos));                                         // o admin escolheu os módulos: o perfil não mexe no plano
    }

    [FatoIntegracao]
    public async Task Criar_loja_com_perfil_de_servicos_so_define_o_plano_se_nao_vieram_modulos()
    {
        var super = await SuperAsync();
        var perfilId = await api.NoBancoAsync(async db =>
        {
            var p = new LojaApi.Models.PerfilLoja { Nome = $"Perfil Serviços {Guid.NewGuid():N}"[..28], TipoPlanoAplica = "servicos", Ativo = true };
            db.PerfisLoja.Add(p);
            await db.SaveChangesAsync();
            return p.Id.ToString();
        });

        var (semModulos, _, _) = await CriarLojaAsync(super, perfilId: perfilId);
        var a = await api.LojaNoBancoAsync(semModulos);
        Assert.Equal(("servicos", "servicos"), (a.TipoPlano, a.ModulosAtivos));

        var (comModulos, _, _) = await CriarLojaAsync(super, perfilId: perfilId, modulos: "financeiro");
        var b = await api.LojaNoBancoAsync(comModulos);
        Assert.Equal(("loja", "financeiro"), (b.TipoPlano, b.ModulosAtivos));                                           // o admin escolheu os módulos: o perfil não mexe
    }

    [FatoIntegracao]
    public async Task Dashboard_nao_lista_loja_cancelada_entre_as_atrasadas()
    {
        var super = await SuperAsync();
        var (id, _, _) = await CriarLojaAsync(super);
        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(l => l.Status, LojaApi.Models.StatusLoja.Cancelado).SetProperty(l => l.ProximoVencimento, DateTime.UtcNow.AddDays(-10))));

        var j = (await super.Get("/api/admin/dashboard")).Json;

        Assert.DoesNotContain(j.GetProperty("lojasAtrasadas").EnumerateArray(), l => l.GetProperty("id").GetGuid() == id);
    }

    [FatoIntegracao]
    public async Task Atualizar_loja_troca_os_campos_e_so_mexe_em_plano_modulos_e_teste_se_vierem()
    {
        var super = await SuperAsync();
        var (id, _, _) = await CriarLojaAsync(super, modulos: "nf");
        object Corpo(string? plano = null, string? modulos = null, bool? teste = null) => new
        {
            nome = "Nome Editado", email = $"editado-{Sufixo()}@teste.com", cnpj = "33" + Sufixo(), cpf = "12345678901", telefone = "6733330000", endereco = "Av. Y, 2",
            corPrimaria = "#445566", logoUrl = "https://x/l.png", mensalidadeDia = 20, mensalidadeValor = 77m, tipoPlano = plano, modulosAtivos = modulos, ehTeste = teste,
        };

        var r = await super.Put($"/api/admin/lojas/{id}", Corpo());
        Assert.Equal(200, r.Codigo);
        var l = await api.LojaNoBancoAsync(id);
        Assert.Equal(("Nome Editado", "#445566", "https://x/l.png", 20, 77m, "loja", "nf", false), (l.Nome, l.CorPrimaria, l.LogoUrl, l.MensalidadeDia, l.MensalidadeValor, l.TipoPlano, l.ModulosAtivos, l.EhTeste));

        (await super.Put($"/api/admin/lojas/{id}", Corpo("servicos", "", true))).DeveSerSucesso();
        var depois = await api.LojaNoBancoAsync(id);
        Assert.Equal(("servicos", "", true), (depois.TipoPlano, depois.ModulosAtivos, depois.EhTeste));
        Assert.Equal(404, (await super.Put($"/api/admin/lojas/{Guid.NewGuid()}", Corpo())).Codigo);
    }

    [FatoIntegracao]
    public async Task Alterar_status_valida_o_nome_e_aceita_qualquer_caixa()
    {
        var super = await SuperAsync();
        var (id, _, _) = await CriarLojaAsync(super);

        Assert.Equal((400, "Status inválido."), await Erro(super.Patch($"/api/admin/lojas/{id}/status", new { status = "xyz" })));
        var r = await super.Patch($"/api/admin/lojas/{id}/status", new { status = "bloqueado" });
        Assert.Equal("Status alterado para Bloqueado.", r.Json.GetProperty("mensagem").GetString());
        Assert.Equal(LojaApi.Models.StatusLoja.Bloqueado, (await api.LojaNoBancoAsync(id)).Status);
        Assert.Equal(404, (await super.Patch($"/api/admin/lojas/{Guid.NewGuid()}/status", new { status = "ativo" })).Codigo);
    }

    [FatoIntegracao]
    public async Task Atualizar_plano_define_tipo_e_modulos_e_modulos_nulo_vira_vazio()
    {
        var super = await SuperAsync();
        var (id, _, _) = await CriarLojaAsync(super, modulos: "nf");

        var r = await super.Patch($"/api/admin/lojas/{id}/plano", new { tipoPlano = "servicos", modulosAtivos = (string?)null });

        Assert.Equal(["id", "tipoPlano", "modulosAtivos"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("servicos", ""), (r.Json.GetProperty("tipoPlano").GetString()!, r.Json.GetProperty("modulosAtivos").GetString()!));
        Assert.Equal(404, (await super.Patch($"/api/admin/lojas/{Guid.NewGuid()}/plano", new { tipoPlano = "loja" })).Codigo);
    }

    [FatoIntegracao]
    public async Task Trocar_email_valida_formato_escolha_admin_e_duplicidade_e_aplica_no_login_e_na_loja()
    {
        var super = await SuperAsync();
        var (id, adminEmail, _) = await CriarLojaAsync(super);
        var (_, outroAdminEmail, _) = await CriarLojaAsync(super);
        var emailDeOutraLoja = (await api.LojaNoBancoAsync((await CriarLojaAsync(super)).Id)).Email;
        var s = Sufixo();

        Assert.Equal((404, "Loja não encontrada."), await Erro(super.Patch($"/api/admin/lojas/{Guid.NewGuid()}/email", new { novoEmail = "a@b.com", trocarLogin = true, trocarLoja = true })));
        Assert.Equal((400, "E-mail inválido."), await Erro(super.Patch($"/api/admin/lojas/{id}/email", new { novoEmail = "semarroba", trocarLogin = true, trocarLoja = true })));
        Assert.Equal((400, "Escolha ao menos um e-mail para trocar."), await Erro(super.Patch($"/api/admin/lojas/{id}/email", new { novoEmail = $"x-{s}@teste.com", trocarLogin = false, trocarLoja = false })));
        Assert.Equal((409, "Este e-mail já está em uso por outro usuário."), await Erro(super.Patch($"/api/admin/lojas/{id}/email", new { novoEmail = outroAdminEmail.ToUpper(), trocarLogin = true, trocarLoja = false })));
        Assert.Equal((409, "Este e-mail já está em uso por outra loja."), await Erro(super.Patch($"/api/admin/lojas/{id}/email", new { novoEmail = emailDeOutraLoja, trocarLogin = false, trocarLoja = true })));

        var novo = $"  Novo-{s}@Teste.com ";
        var ok = await super.Patch($"/api/admin/lojas/{id}/email", new { novoEmail = novo, trocarLogin = true, trocarLoja = true });
        Assert.Equal("E-mail atualizado com sucesso.", ok.Json.GetProperty("mensagem").GetString());
        Assert.Equal($"novo-{s}@teste.com", (await api.LojaNoBancoAsync(id)).Email);
        Assert.Equal(200, (int)(await api.Fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = $"novo-{s}@teste.com", senha = "senha123" })).StatusCode);
        Assert.Equal(401, (int)(await api.Fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = adminEmail, senha = "senha123" })).StatusCode);
    }

    [FatoIntegracao]
    public async Task Trocar_email_so_do_login_nao_mexe_no_da_loja_e_exige_admin_para_o_login()
    {
        var super = await SuperAsync();
        var (id, _, _) = await CriarLojaAsync(super);
        var emailLoja = (await api.LojaNoBancoAsync(id)).Email;
        var s = Sufixo();

        (await super.Patch($"/api/admin/lojas/{id}/email", new { novoEmail = $"so-login-{s}@teste.com", trocarLogin = true, trocarLoja = false })).DeveSerSucesso();
        Assert.Equal(emailLoja, (await api.LojaNoBancoAsync(id)).Email);

        await api.NoBancoAsync(db => db.UsuariosLoja.Where(ul => ul.LojaId == id).ExecuteDeleteAsync());
        Assert.Equal((400, "Usuário admin da loja não encontrado."), await Erro(super.Patch($"/api/admin/lojas/{id}/email", new { novoEmail = $"y-{s}@teste.com", trocarLogin = true, trocarLoja = false })));
        (await super.Patch($"/api/admin/lojas/{id}/email", new { novoEmail = $"y-{s}@teste.com", trocarLogin = false, trocarLoja = true })).DeveSerSucesso();       // só a loja: não precisa de admin
    }

    [FatoIntegracao]
    public async Task Atualizar_valor_valida_e_sincroniza_com_o_mercado_pago_so_quando_ha_assinatura_ativa()
    {
        var super = await SuperAsync();
        var (id, _, _) = await CriarLojaAsync(super);

        Assert.Equal((404, "Loja não encontrada."), await Erro(super.Patch($"/api/admin/lojas/{Guid.NewGuid()}/valor", new { novoValor = 10m, sincronizarAssinatura = false })));
        Assert.Equal((400, "Valor inválido."), await Erro(super.Patch($"/api/admin/lojas/{id}/valor", new { novoValor = 0m, sincronizarAssinatura = false })));

        var simples = (await super.Patch($"/api/admin/lojas/{id}/valor", new { novoValor = 150m, sincronizarAssinatura = false })).Json;
        Assert.Equal(("Valor atualizado.", false, JsonValueKind.Null), (simples.GetProperty("mensagem").GetString()!, simples.GetProperty("sincronizou").GetBoolean(), simples.GetProperty("aviso").ValueKind));
        Assert.Equal(150m, (await api.LojaNoBancoAsync(id)).MensalidadeValor);

        var semAssinatura = (await super.Patch($"/api/admin/lojas/{id}/valor", new { novoValor = 160m, sincronizarAssinatura = true })).Json;
        Assert.Equal("A loja não tem assinatura ativa para sincronizar. Só o valor das próximas faturas foi alterado.", semAssinatura.GetProperty("aviso").GetString());

        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == id).ExecuteUpdateAsync(s => s.SetProperty(l => l.MpPreapprovalId, "pre-teste").SetProperty(l => l.AssinaturaStatus, "authorized")));
        var sincronizada = (await super.Patch($"/api/admin/lojas/{id}/valor", new { novoValor = 170m, sincronizarAssinatura = true })).Json;
        Assert.Equal(("Valor atualizado na loja e na assinatura recorrente.", true), (sincronizada.GetProperty("mensagem").GetString()!, sincronizada.GetProperty("sincronizou").GetBoolean()));
        Assert.Contains(api.MercadoPago.ValoresAtualizados, v => v.PreapprovalId == "pre-teste" && v.Valor == 170m);

        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == id).ExecuteUpdateAsync(s => s.SetProperty(l => l.AssinaturaStatus, "cancelled")));
        var antes = api.MercadoPago.ValoresAtualizados.Count;
        var cancelada = (await super.Patch($"/api/admin/lojas/{id}/valor", new { novoValor = 175m, sincronizarAssinatura = true })).Json;
        Assert.StartsWith("A loja não tem assinatura ativa para sincronizar.", cancelada.GetProperty("aviso").GetString());       // assinatura cancelada não é sincronizada
        Assert.Equal(antes, api.MercadoPago.ValoresAtualizados.Count);
        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == id).ExecuteUpdateAsync(s => s.SetProperty(l => l.AssinaturaStatus, "authorized")));

        api.MercadoPago.FalharAtualizacaoDeValor = true;
        try
        {
            var falhou = (await super.Patch($"/api/admin/lojas/{id}/valor", new { novoValor = 180m, sincronizarAssinatura = true })).Json;
            Assert.Equal(("Valor atualizado.", false), (falhou.GetProperty("mensagem").GetString()!, falhou.GetProperty("sincronizou").GetBoolean()));
            Assert.StartsWith("O valor da loja foi alterado, mas não foi possível atualizar a assinatura no Mercado Pago.", falhou.GetProperty("aviso").GetString());
            Assert.Equal(180m, (await api.LojaNoBancoAsync(id)).MensalidadeValor);                                      // o valor da loja muda mesmo assim
        }
        finally { api.MercadoPago.FalharAtualizacaoDeValor = false; }
    }

    [FatoIntegracao]
    public async Task Alterar_trial_vai_ate_o_fim_do_dia_e_reativa_loja_bloqueada_como_trial()
    {
        var super = await SuperAsync();
        var (id, _, _) = await CriarLojaAsync(super);
        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == id).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LojaApi.Models.StatusLoja.Bloqueado)));
        var nova = DateTime.UtcNow.AddDays(5);

        var r = await super.Patch($"/api/admin/lojas/{id}/trial", new { trialAte = nova.ToString("o") });

        Assert.Equal(["id", "trialAte", "status"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal((int)LojaApi.Models.StatusLoja.Trial, r.Json.GetProperty("status").GetInt32());                    // o status sai como número aqui (enum sem texto)
        var l = await api.LojaNoBancoAsync(id);
        Assert.Equal((nova.Date, 23, 59, LojaApi.Models.StatusLoja.Trial), (l.TrialAte.Date, l.TrialAte.Hour, l.TrialAte.Minute, l.Status));

        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, LojaApi.Models.StatusLoja.Ativo)));
        (await super.Patch($"/api/admin/lojas/{id}/trial", new { trialAte = nova.ToString("o") })).DeveSerSucesso();
        Assert.Equal(LojaApi.Models.StatusLoja.Ativo, (await api.LojaNoBancoAsync(id)).Status);                          // só reativa quem estava bloqueado
        Assert.Equal(404, (await super.Patch($"/api/admin/lojas/{Guid.NewGuid()}/trial", new { trialAte = nova.ToString("o") })).Codigo);
    }

    // ── Pagamentos ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Registrar_pagamento_manual_paga_a_fatura_pendente_reativa_a_loja_e_cria_a_proxima()
    {
        var super = await SuperAsync();
        var (id, _, _) = await CriarLojaAsync(super);
        var fatura = await api.FaturaPendenteAsync(id);
        var pagoEm = DateTime.UtcNow.AddHours(-1);

        var r = await super.Post("/api/admin/pagamentos", new { lojaId = id, valor = fatura.Valor, vencimento = fatura.Vencimento.ToString("o"), pagoEm = pagoEm.ToString("o"), formaPagamento = "pix", observacao = "Recebido por fora" });

        Assert.Equal("Pagamento registrado e loja reativada.", r.Json.GetProperty("mensagem").GetString());
        var faturas = await api.PagamentosDaLojaNoBancoAsync(id);
        Assert.Equal(["pago", "pendente"], faturas.Select(p => p.Status));
        Assert.Equal(("pix", "Recebido por fora"), (faturas[0].FormaPagamento, faturas[0].Observacao));
        Assert.Equal(LojaApi.Models.StatusLoja.Ativo, (await api.LojaNoBancoAsync(id)).Status);

        var lista = (await super.Get($"/api/admin/lojas/{id}/pagamentos")).Json.EnumerateArray().ToList();
        Assert.Equal(2, lista.Count);
        var vencimentos = lista.Select(p => p.GetProperty("vencimento").GetDateTime()).ToList();
        Assert.Equal(vencimentos.OrderByDescending(v => v).ToList(), vencimentos);                                       // mais nova primeiro
        Assert.Equal(404, (await super.Post("/api/admin/pagamentos", new { lojaId = Guid.NewGuid(), valor = 1m, vencimento = DateTime.UtcNow.ToString("o"), pagoEm = DateTime.UtcNow.ToString("o"), formaPagamento = "pix" })).Codigo);
    }

    [FatoIntegracao]
    public async Task Pagamento_manual_de_loja_promocional_so_encerra_a_promocao_um_pagamento_depois_do_previsto()
    {
        var super = await SuperAsync();
        var (id, _, _) = await CriarLojaAsync(super);
        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(l => l.Promocional, true).SetProperty(l => l.ValorPosPromocional, 200m).SetProperty(l => l.MesesPromocional, 1).SetProperty(l => l.MensalidadeValor, 90m)));
        var primeira = await api.FaturaPendenteAsync(id);

        (await super.Post("/api/admin/pagamentos", new { lojaId = id, valor = 90m, vencimento = primeira.Vencimento.ToString("o"), pagoEm = DateTime.UtcNow.ToString("o"), formaPagamento = "pix" })).DeveSerSucesso();

        // defeito antigo mantido: a contagem de faturas pagas roda antes de gravar a que está sendo paga agora,
        // então com 1 mês de promoção ela ainda vale depois do primeiro pagamento
        var depoisDoPrimeiro = await api.LojaNoBancoAsync(id);
        Assert.Equal((true, 90m), (depoisDoPrimeiro.Promocional, depoisDoPrimeiro.MensalidadeValor));

        var segunda = await api.FaturaPendenteAsync(id);
        (await super.Post("/api/admin/pagamentos", new { lojaId = id, valor = 90m, vencimento = segunda.Vencimento.ToString("o"), pagoEm = DateTime.UtcNow.ToString("o"), formaPagamento = "pix" })).DeveSerSucesso();

        var depoisDoSegundo = await api.LojaNoBancoAsync(id);
        Assert.Equal((false, 200m), (depoisDoSegundo.Promocional, depoisDoSegundo.MensalidadeValor));
        Assert.Equal(200m, (await api.FaturaPendenteAsync(id)).Valor);                                                   // a próxima fatura já vem com o valor cheio
    }

    // ── Bloqueios ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Verificar_bloqueios_bloqueia_trial_vencido_e_assinatura_atrasada()
    {
        var super = await SuperAsync();
        var (trial, _, _) = await CriarLojaAsync(super);
        var (ativa, _, _) = await CriarLojaAsync(super);
        var (emDia, _, _) = await CriarLojaAsync(super);
        await api.NoBancoAsync(async db =>
        {
            await db.Lojas.Where(l => l.Id == trial).ExecuteUpdateAsync(s => s.SetProperty(l => l.TrialAte, DateTime.UtcNow.AddDays(-3)));
            await db.Lojas.Where(l => l.Id == ativa).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LojaApi.Models.StatusLoja.Ativo).SetProperty(l => l.ProximoVencimento, DateTime.UtcNow.AddDays(-3)));
            await db.Lojas.Where(l => l.Id == emDia).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, LojaApi.Models.StatusLoja.Ativo).SetProperty(l => l.ProximoVencimento, DateTime.UtcNow.AddDays(10)));
        });

        var r = await super.Post("/api/admin/verificar-bloqueios");

        Assert.Equal("Verificação de bloqueios executada.", r.Json.GetProperty("mensagem").GetString());
        Assert.Equal(LojaApi.Models.StatusLoja.Bloqueado, (await api.LojaNoBancoAsync(trial)).Status);
        Assert.Equal(LojaApi.Models.StatusLoja.Bloqueado, (await api.LojaNoBancoAsync(ativa)).Status);
        Assert.Equal(LojaApi.Models.StatusLoja.Ativo, (await api.LojaNoBancoAsync(emDia)).Status);
    }

    // ── Comunicados ──────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Comunicado_valida_assunto_mensagem_e_destinatarios()
    {
        var super = await SuperAsync();

        Assert.Equal((400, "Preencha assunto e mensagem."), await Erro(super.Post("/api/admin/comunicados/enviar", new { todasLojas = true, assunto = " ", corpoHtml = "<p>oi</p>" })));
        Assert.Equal((400, "Preencha assunto e mensagem."), await Erro(super.Post("/api/admin/comunicados/enviar", new { todasLojas = true, assunto = "A", corpoHtml = "" })));
        Assert.Equal((400, "Selecione ao menos uma loja, informe um e-mail extra, ou marque \"Todas as lojas\"."),
            await Erro(super.Post("/api/admin/comunicados/enviar", new { todasLojas = false, lojaIds = Array.Empty<Guid>(), assunto = "A", corpoHtml = "<p>oi</p>" })));
    }

    [FatoIntegracao]
    public async Task Comunicado_envia_para_as_lojas_escolhidas_e_extras_e_aparece_na_lista()
    {
        var super = await SuperAsync();
        var (id, _, nome) = await CriarLojaAsync(super);
        var (teste, _, _) = await CriarLojaAsync(super, ehTeste: true);
        var emailLoja = (await api.LojaNoBancoAsync(id)).Email;
        var assunto = $"Novidade {Sufixo()}";

        var r = await super.Post("/api/admin/comunicados/enviar", new { todasLojas = false, lojaIds = new[] { id, teste }, assunto, corpoHtml = "<p>Olá!</p>", emailsExtras = new[] { "extra@teste.com", " " } });

        Assert.Equal(200, r.Codigo);
        Assert.Equal("2 e-mail(s) enviado(s).", r.Json.GetProperty("mensagem").GetString());                                // a loja de teste fica de fora; o extra em branco também
        Assert.Equal((2, 0), (r.Json.GetProperty("totalEnviados").GetInt32(), r.Json.GetProperty("totalFalhas").GetInt32()));
        var enviados = api.Resend.ReceivedCalls().Where(c => c.GetMethodInfo().Name == nameof(Resend.IResend.EmailSendAsync))
            .Select(c => c.GetArguments()[0]).OfType<Resend.EmailMessage>().Where(m => m.Subject == assunto).ToList();
        Assert.Equal(new[] { emailLoja, "extra@teste.com" }.Order(), enviados.SelectMany(m => m.To.Select(t => t.Email)).Order());
        Assert.Contains(nome, enviados.First(m => m.To.Any(t => t.Email == emailLoja)).HtmlBody);

        var item = (await super.Get("/api/admin/comunicados")).Json.EnumerateArray().First(c => c.GetProperty("assunto").GetString() == assunto);
        Assert.Equal(["id", "assunto", "corpoHtml", "todasLojas", "totalEnviados", "totalFalhas", "destinatariosSucesso", "destinatariosFalha", "enviadoEm"], item.EnumerateObject().Select(x => x.Name));
        Assert.Equal(2, item.GetProperty("destinatariosSucesso").GetArrayLength());
    }

    // ── Acesso como suporte, backup e exclusão ───────────────────────────────

    [FatoIntegracao]
    public async Task Acessar_como_suporte_devolve_o_token_do_admin_da_loja_e_recusa_loja_sem_admin()
    {
        var super = await SuperAsync();
        var (id, adminEmail, nome) = await CriarLojaAsync(super);

        var r = await super.Post($"/api/admin/lojas/{id}/acessar");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["token", "nome", "email", "role", "nomeLoja"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("Admin Novo", adminEmail, "admin", nome), (r.Json.GetProperty("nome").GetString()!, r.Json.GetProperty("email").GetString()!, r.Json.GetProperty("role").GetString()!, r.Json.GetProperty("nomeLoja").GetString()!));
        var http = api.Fabrica.CreateClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", r.Json.GetProperty("token").GetString());
        Assert.Equal(200, (await new ClienteApi(http).Get("/api/cliente/dashboard")).Codigo);                            // o token funciona como o admin da loja

        // e é marcado como acesso de suporte: usar a sessão não conta como login real do cliente
        var antigo = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await api.NoBancoAsync(db => db.Usuarios.Where(u => u.Email == adminEmail).ExecuteUpdateAsync(s => s.SetProperty(u => u.UltimoLoginEm, antigo)));
        Assert.Equal(200, (await new ClienteApi(http).Get("/api/auth/me")).Codigo);
        Assert.Equal(antigo, await api.NoBancoAsync(db => db.Usuarios.Where(u => u.Email == adminEmail).Select(u => u.UltimoLoginEm).SingleAsync()));

        Assert.Equal((404, "Loja não encontrada."), await Erro(super.Post($"/api/admin/lojas/{Guid.NewGuid()}/acessar")));
        await api.NoBancoAsync(db => db.UsuariosLoja.Where(ul => ul.LojaId == id).ExecuteUpdateAsync(s => s.SetProperty(ul => ul.Ativo, false)));
        Assert.Equal((404, "Esta loja não tem um administrador ativo."), await Erro(super.Post($"/api/admin/lojas/{id}/acessar")));
    }

    [FatoIntegracao]
    public async Task Backup_baixa_um_json_com_os_dados_da_loja_e_404_se_nao_existe()
    {
        var super = await SuperAsync();
        var loja = await NovaAsync();
        await InserirProdutoAsync(loja.Id, "Item de Backup");

        var r = await super.Get($"/api/admin/lojas/{loja.Id}/backup");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("application/json", r.TipoConteudo);
        var j = r.Json;
        Assert.Equal(["geradoEm", "loja", "categorias", "produtos", "clientes", "vendas", "trocas", "movimentos", "pagamentos", "totais"], j.EnumerateObject().Select(x => x.Name));
        Assert.Equal(loja.Id, j.GetProperty("loja").GetProperty("Id").GetGuid());                                       // o backup usa os nomes das propriedades como estão (sem camelCase)
        Assert.Equal(1, j.GetProperty("totais").GetProperty("produtos").GetInt32());
        Assert.Contains(j.GetProperty("produtos").EnumerateArray(), p => p.GetProperty("Nome").GetString() == "Item de Backup");
        Assert.Equal(404, (await super.Get($"/api/admin/lojas/{Guid.NewGuid()}/backup")).Codigo);
    }

    [FatoIntegracao]
    public async Task Deletar_produtos_e_vendas_apaga_o_catalogo_e_mantem_clientes_usuarios_e_a_loja()
    {
        var super = await SuperAsync();
        var loja = await NovaAsync();
        await InserirProdutoAsync(loja.Id, "Para apagar");
        (await loja.Painel.Post("/api/clientes", new { nome = "Cliente que fica", telefone = "67999990000" })).DeveSerSucesso();
        Assert.Equal(1, await api.NoBancoAsync(db => db.Produtos.CountAsync(p => p.LojaId == loja.Id)));

        var r = await super.Delete($"/api/admin/lojas/{loja.Id}/produtos-e-vendas");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Produtos, vendas, trocas e ajustes de caixa foram removidos. Clientes e usuários foram mantidos.", r.Json.GetProperty("mensagem").GetString());
        Assert.Empty(await api.NoBancoAsync(db => db.Produtos.Where(p => p.LojaId == loja.Id).ToListAsync()));
        Assert.Equal(1, await api.NoBancoAsync(db => db.Clientes.CountAsync(c => c.LojaId == loja.Id)));
        Assert.NotNull(await api.NoBancoAsync(db => db.Lojas.FirstOrDefaultAsync(l => l.Id == loja.Id)));
        Assert.Equal(404, (await super.Delete($"/api/admin/lojas/{Guid.NewGuid()}/produtos-e-vendas")).Codigo);
    }

    [FatoIntegracao]
    public async Task Deletar_loja_remove_a_loja_os_dados_e_os_usuarios_que_so_pertenciam_a_ela()
    {
        var super = await SuperAsync();
        var (id, adminEmail, _) = await CriarLojaAsync(super);
        await InserirProdutoAsync(id, "Some junto");
        Assert.Equal(1, await api.NoBancoAsync(db => db.Produtos.CountAsync(p => p.LojaId == id)));

        var r = await super.Delete($"/api/admin/lojas/{id}");

        Assert.Equal("Loja e todos os dados foram removidos.", r.Json.GetProperty("mensagem").GetString());
        Assert.Null(await api.NoBancoAsync(db => db.Lojas.FirstOrDefaultAsync(l => l.Id == id)));
        Assert.Empty(await api.NoBancoAsync(db => db.Produtos.Where(p => p.LojaId == id).ToListAsync()));
        Assert.Empty(await api.NoBancoAsync(db => db.Pagamentos.Where(p => p.LojaId == id).ToListAsync()));
        Assert.Null(await api.NoBancoAsync(db => db.Usuarios.FirstOrDefaultAsync(u => u.Email == adminEmail)));
        Assert.Equal(404, (await super.Delete($"/api/admin/lojas/{id}")).Codigo);
    }
}
