using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LojaApi.Tests.Api.PlataformaTeste;

// Login, sessão, cadastro público de loja (signup) e troca de senha.
[Collection(ColecaoApi.Nome)]
public class AuthApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    private static string EmailDe(LojaTeste loja) => $"dono-{loja.Slug[2..]}@teste.com";

    private static object Signup(string nomeLoja, string email, string senha = "123456", string perfilId = "zero", string[]? extras = null, string responsavel = "Maria Souza") =>
        new { nomeLoja, perfilId, nomeResponsavel = responsavel, email, senha, telefone = "67999990000", modulosExtras = extras };

    // E-mails enviados (o Resend é um falso): o aviso interno tem o nome da loja no assunto e a boas-vindas vai para o e-mail do cadastro
    private List<Resend.EmailMessage> EmailsDa(string nomeLoja, string email) => api.Resend.ReceivedCalls()
        .Where(c => c.GetMethodInfo().Name == nameof(Resend.IResend.EmailSendAsync))
        .Select(c => c.GetArguments()[0]).OfType<Resend.EmailMessage>()
        .Where(m => (m.Subject ?? "").Contains(nomeLoja) || m.To.Any(t => t.Email == email)).ToList();

    // ── Login ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Login_devolve_token_nome_email_e_papel_aceita_caixa_diferente_e_marca_o_ultimo_acesso()
    {
        var loja = await NovaAsync();
        var email = EmailDe(loja);
        var antes = await api.NoBancoAsync(db => db.Usuarios.AsNoTracking().Where(u => u.Email == email).Select(u => u.UltimoLoginEm).SingleAsync());

        var r = await loja.Site.Post("/api/auth/login", new { email = email.ToUpper(), senha = "123456" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["token", "nome", "email", "role"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("Dono", email, "admin"), (r.Json.GetProperty("nome").GetString()!, r.Json.GetProperty("email").GetString()!, r.Json.GetProperty("role").GetString()!));
        Assert.Contains('.', r.Json.GetProperty("token").GetString());
        var depois = await api.NoBancoAsync(db => db.Usuarios.AsNoTracking().Where(u => u.Email == email).Select(u => u.UltimoLoginEm).SingleAsync());
        Assert.True(depois > antes || antes is null);
    }

    [FatoIntegracao]
    public async Task Login_recusa_senha_errada_email_inexistente_e_usuario_inativo_com_a_mesma_mensagem()
    {
        var loja = await NovaAsync();
        var email = EmailDe(loja);

        Assert.Equal((401, "E-mail ou senha incorretos."), await Erro(loja.Site.Post("/api/auth/login", new { email, senha = "errada" })));
        Assert.Equal((401, "E-mail ou senha incorretos."), await Erro(loja.Site.Post("/api/auth/login", new { email = "ninguem@teste.com", senha = "123456" })));

        await api.NoBancoAsync(db => db.Usuarios.Where(u => u.Email == email).ExecuteUpdateAsync(s => s.SetProperty(u => u.Ativo, false)));
        Assert.Equal((401, "E-mail ou senha incorretos."), await Erro(loja.Site.Post("/api/auth/login", new { email, senha = "123456" })));
    }

    [FatoIntegracao]
    public async Task Login_de_loja_com_trial_vencido_passa_mas_ja_grava_o_bloqueio()
    {
        var loja = await NovaAsync();
        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.TrialAte, DateTime.UtcNow.AddDays(-3))));

        var r = await loja.Site.Post("/api/auth/login", new { email = EmailDe(loja), senha = "123456" });

        Assert.Equal(200, r.Codigo);                                                                                    // o login não é barrado aqui (o bloqueio vale nas telas)
        Assert.Equal(LojaApi.Models.StatusLoja.Bloqueado, await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == loja.Id).Select(l => l.Status).SingleAsync()));
    }

    // ── Sessão ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Me_exige_login_devolve_a_identidade_e_atualiza_o_ultimo_acesso_menos_no_acesso_de_suporte()
    {
        var loja = await NovaAsync();
        var email = EmailDe(loja);
        Assert.Equal(401, (await loja.Site.Get("/api/auth/me")).Codigo);

        var j = (await loja.Painel.Get("/api/auth/me")).Json;
        Assert.Equal(["id", "nome", "email", "role"], j.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("Dono", email, "admin"), (j.GetProperty("nome").GetString()!, j.GetProperty("email").GetString()!, j.GetProperty("role").GetString()!));

        // acesso de suporte (superadmin entrando como o cliente) não conta como login real
        await api.NoBancoAsync(db => db.Usuarios.Where(u => u.Email == email).ExecuteUpdateAsync(s => s.SetProperty(u => u.UltimoLoginEm, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc))));
        var tokenSuporte = await api.ServicoAsync<TokenService, string>(async t =>
        {
            var u = await api.NoBancoAsync(db => db.Usuarios.AsNoTracking().FirstAsync(x => x.Email == email));
            return t.GerarToken(u, ehSuporte: true);
        });
        var http = api.Fabrica.CreateClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", tokenSuporte);
        Assert.Equal(200, (await new ClienteApi(http).Get("/api/auth/me")).Codigo);
        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), await api.NoBancoAsync(db => db.Usuarios.Where(u => u.Email == email).Select(u => u.UltimoLoginEm).SingleAsync()));

        await loja.Painel.Get("/api/auth/me");
        Assert.True(await api.NoBancoAsync(db => db.Usuarios.Where(u => u.Email == email).Select(u => u.UltimoLoginEm).SingleAsync()) > new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [FatoIntegracao]
    public async Task Gerar_hash_e_publico_e_devolve_um_hash_bcrypt_valido()
    {
        var loja = await NovaAsync();

        var r = await loja.Site.Get("/api/auth/gerar-hash/minhaSenha");

        Assert.Equal(200, r.Codigo);
        Assert.True(BCrypt.Net.BCrypt.Verify("minhaSenha", r.Json.GetProperty("hash").GetString()));
    }

    // ── Cadastro público ─────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Signup_valida_campos_senha_curta_e_email_ja_cadastrado_de_usuario_ou_de_loja()
    {
        var loja = await NovaAsync();
        var email = EmailDe(loja);
        var anonimo = loja.Site;

        Assert.Equal((400, "Preencha todos os campos obrigatórios."), await Erro(anonimo.Post("/api/auth/signup", Signup(" ", "x@teste.com"))));
        Assert.Equal((400, "Preencha todos os campos obrigatórios."), await Erro(anonimo.Post("/api/auth/signup", Signup("Loja", " "))));
        Assert.Equal((400, "Preencha todos os campos obrigatórios."), await Erro(anonimo.Post("/api/auth/signup", Signup("Loja", "x@teste.com", senha: ""))));
        Assert.Equal((400, "A senha deve ter pelo menos 6 caracteres."), await Erro(anonimo.Post("/api/auth/signup", Signup("Loja", "x@teste.com", senha: "12345"))));
        Assert.Equal((409, "Este e-mail já está cadastrado."), await Erro(anonimo.Post("/api/auth/signup", Signup("Loja", email.ToUpper()))));

        var soDaLoja = $"so-loja-{Guid.NewGuid():N}@teste.com";                                                         // e-mail só na Loja (sem usuário)
        await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.Email, soDaLoja)));
        Assert.Equal((409, "Este e-mail já está cadastrado."), await Erro(anonimo.Post("/api/auth/signup", Signup("Loja", soDaLoja.ToUpper()))));
    }

    [FatoIntegracao]
    public async Task Signup_sem_perfil_cria_loja_em_trial_admin_vinculo_e_primeira_fatura_e_ja_devolve_o_login()
    {
        var loja = await NovaAsync();
        var sufixo = Guid.NewGuid().ToString("N")[..10];
        var nome = $"Loja Nova {sufixo}";
        var email = $"novo-{sufixo}@teste.com";
        var totalAntes = await api.NoBancoAsync(db => db.Lojas.CountAsync(l => !l.EhTeste && l.TipoPlano != "financeiro"));

        var r = await loja.Site.Post("/api/auth/signup", Signup(nome, email, responsavel: "Joana Lima"));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["token", "nome", "email", "role"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("Joana Lima", email, "admin"), (r.Json.GetProperty("nome").GetString()!, r.Json.GetProperty("email").GetString()!, r.Json.GetProperty("role").GetString()!));

        var l = await api.NoBancoAsync(db => db.Lojas.AsNoTracking().FirstAsync(x => x.Nome == nome));
        var promocional = totalAntes < 10;
        Assert.Equal((LojaApi.Models.StatusLoja.Trial, "#c38228", promocional, promocional ? 89.90m : 119.90m, promocional ? 3 : 0), (l.Status, l.CorPrimaria, l.Promocional, l.MensalidadeValor, l.MesesPromocional));
        Assert.Equal((promocional ? 89.90m : (decimal?)null, promocional ? 119.90m : (decimal?)null), (l.ValorPromocional, l.ValorPosPromocional));
        Assert.InRange((l.TrialAte - DateTime.UtcNow).TotalDays, 6.9, 7.01);
        Assert.Equal(DateTime.UtcNow.Day, l.MensalidadeDia);
        Assert.StartsWith("loja_loja_nova_", l.SchemaNome);
        Assert.Equal("", l.ModulosAtivos ?? "");

        var usuario = await api.NoBancoAsync(db => db.Usuarios.AsNoTracking().FirstAsync(u => u.Email == email));
        Assert.Equal(("admin", true), (usuario.Role, BCrypt.Net.BCrypt.Verify("123456", usuario.SenhaHash)));
        var vinculo = await api.NoBancoAsync(db => db.UsuariosLoja.AsNoTracking().SingleAsync(ul => ul.UsuarioId == usuario.Id));
        Assert.Equal((l.Id, "admin"), (vinculo.LojaId, vinculo.Role));
        var fatura = await api.NoBancoAsync(db => db.Pagamentos.AsNoTracking().SingleAsync(p => p.LojaId == l.Id));
        Assert.Equal(("pendente", l.MensalidadeValor, l.TrialAte), (fatura.Status, fatura.Valor, fatura.Vencimento));
        Assert.Empty(await api.NoBancoAsync(db => db.CategoriasLoja.Where(c => c.LojaId == l.Id).ToListAsync()));

        var http = api.Fabrica.CreateClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", r.Json.GetProperty("token").GetString());
        Assert.Equal(200, (await new ClienteApi(http).Get("/api/cliente/dashboard")).Codigo);                          // o token já vale

        var emails = EmailsDa(nome, email);
        Assert.Equal(2, emails.Count);
        Assert.Contains(emails, m => m.Subject == $"🎉 Novo cadastro: {nome}");
        Assert.Contains(emails, m => m.Subject == "Bem-vindo(a) ao AldevSoftware, Joana!" && m.To.Any(t => t.Email == email));
    }

    [FatoIntegracao]
    public async Task Signup_com_perfil_copia_categorias_servicos_e_define_plano_e_modulos()
    {
        var loja = await NovaAsync();
        var perfis = (await loja.Site.Get("/api/perfis")).Json.EnumerateArray().ToList();
        var comCategorias = perfis.First(p => p.GetProperty("categorias").GetArrayLength() > 0);
        var perfilId = comCategorias.GetProperty("id").GetString()!;
        var sufixo = Guid.NewGuid().ToString("N")[..10];
        var nome = $"Loja Perfil {sufixo}";

        var emailPerfil = $"perfil-{sufixo}@teste.com";
        (await loja.Site.Post("/api/auth/signup", Signup(nome, emailPerfil, perfilId: perfilId))).DeveSerSucesso();

        var l = await api.NoBancoAsync(db => db.Lojas.AsNoTracking().FirstAsync(x => x.Nome == nome));
        var perfil = await api.NoBancoAsync(db => db.PerfisLoja.AsNoTracking().Include(p => p.Categorias).Include(p => p.Servicos).FirstAsync(p => p.Id == Guid.Parse(perfilId)));
        Assert.Equal(perfil.TipoPlanoAplica, l.TipoPlano);
        var categorias = await api.NoBancoAsync(db => db.CategoriasLoja.Where(c => c.LojaId == l.Id).Select(c => c.Nome).ToListAsync());
        Assert.Equal(perfil.Categorias.Select(c => c.Nome).Order(), categorias.Order());
        var servicos = await api.NoBancoAsync(db => db.Servicos.Where(s => s.LojaId == l.Id).Select(s => s.Nome).ToListAsync());
        Assert.Equal(perfil.Servicos.Select(s => s.Nome).Order(), servicos.Order());
        var esperado = perfil.TipoPlanoAplica is "servicos" or "loja_modulos" ? "servicos" : perfil.Nome == "Corretora" ? "corretora" : perfil.Nome.StartsWith("Pilates") ? "turmas" : "";
        Assert.Equal(esperado, l.ModulosAtivos ?? "");
        Assert.Contains(EmailsDa(nome, emailPerfil), m => m.Subject!.StartsWith("Bem-vindo(a)"));

        var inexistente = $"Loja Sem Perfil {sufixo}";
        (await loja.Site.Post("/api/auth/signup", Signup(inexistente, $"semperfil-{sufixo}@teste.com", perfilId: Guid.NewGuid().ToString()))).DeveSerSucesso();        // perfil que não existe é ignorado
        Assert.Empty(await api.NoBancoAsync(db => db.CategoriasLoja.Where(c => c.Loja!.Nome == inexistente).ToListAsync()));
    }

    [FatoIntegracao]
    public async Task Signup_com_modulos_extras_so_aceita_os_validos_e_disponiveis_e_soma_o_preco()
    {
        var loja = await NovaAsync();
        var financeiro = await api.NoBancoAsync(db => db.ModulosPreco.AsNoTracking().FirstAsync(m => m.Chave == "financeiro"));
        var nf = await api.NoBancoAsync(db => db.ModulosPreco.AsNoTracking().FirstAsync(m => m.Chave == "nf"));
        await api.NoBancoAsync(db => db.ModulosPreco.Where(m => m.Chave == "financeiro" || m.Chave == "nf")
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.DisponivelParaAtivar, true).SetProperty(m => m.Valor, 10m)));
        try
        {
            var sufixo = Guid.NewGuid().ToString("N")[..10];
            var nome = $"Loja Extras {sufixo}";
            var totalAntes = await api.NoBancoAsync(db => db.Lojas.CountAsync(l => !l.EhTeste && l.TipoPlano != "financeiro"));

            (await loja.Site.Post("/api/auth/signup", Signup(nome, $"extras-{sufixo}@teste.com", extras: ["financeiro", "nf", "nf", "inventado", "turmas"]))).DeveSerSucesso();

            var l = await api.NoBancoAsync(db => db.Lojas.AsNoTracking().FirstAsync(x => x.Nome == nome));
            Assert.Equal(["financeiro", "nf"], (l.ModulosAtivos ?? "").Split(',').Order());
            var baseValor = totalAntes < 10 ? 89.90m : 119.90m;
            Assert.Equal(baseValor + 20m, l.MensalidadeValor);
            Assert.Equal(totalAntes < 10 ? 119.90m + 20m : (decimal?)null, l.ValorPosPromocional);

            await api.NoBancoAsync(db => db.ModulosPreco.Where(m => m.Chave == "nf").ExecuteUpdateAsync(s => s.SetProperty(m => m.DisponivelParaAtivar, false)));
            var nome2 = $"Loja Extras2 {sufixo}";
            (await loja.Site.Post("/api/auth/signup", Signup(nome2, $"extras2-{sufixo}@teste.com", extras: ["financeiro", "nf"]))).DeveSerSucesso();
            Assert.Equal("financeiro", (await api.NoBancoAsync(db => db.Lojas.AsNoTracking().FirstAsync(x => x.Nome == nome2))).ModulosAtivos);          // "nf" indisponível some
        }
        finally
        {
            await api.NoBancoAsync(async db =>
            {
                await db.ModulosPreco.Where(m => m.Id == financeiro.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.DisponivelParaAtivar, financeiro.DisponivelParaAtivar).SetProperty(m => m.Valor, financeiro.Valor));
                await db.ModulosPreco.Where(m => m.Id == nf.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.DisponivelParaAtivar, nf.DisponivelParaAtivar).SetProperty(m => m.Valor, nf.Valor));
            });
        }
    }

    [FatoIntegracao]
    public async Task Signup_de_perfil_financeiro_puro_tem_preco_fixo_fora_da_promocao_e_nao_cobra_os_modulos_extras()
    {
        var loja = await NovaAsync();
        var perfilId = await api.NoBancoAsync(async db =>
        {
            var existente = await db.PerfisLoja.Where(p => p.TipoPlanoAplica == "financeiro").Select(p => (Guid?)p.Id).FirstOrDefaultAsync();
            if (existente is not null) return existente.Value;
            var novo = new LojaApi.Models.PerfilLoja { Nome = $"Financeiro Teste {Guid.NewGuid():N}"[..30], TipoPlanoAplica = "financeiro", Ativo = true };
            db.PerfisLoja.Add(novo);
            await db.SaveChangesAsync();
            return novo.Id;
        });
        var sufixo = Guid.NewGuid().ToString("N")[..10];
        var nome = $"Loja Fin {sufixo}";

        (await loja.Site.Post("/api/auth/signup", Signup(nome, $"fin-{sufixo}@teste.com", perfilId: perfilId.ToString(), extras: ["nf"]))).DeveSerSucesso();

        var l = await api.NoBancoAsync(db => db.Lojas.AsNoTracking().FirstAsync(x => x.Nome == nome));
        Assert.Equal((39.90m, false, "financeiro", null), (l.MensalidadeValor, l.Promocional, l.TipoPlano, l.ValorPromocional));
        // defeito antigo mantido: o preço dos extras não entra (preço fixo), mas o módulo escolhido é gravado na loja do mesmo jeito
        Assert.Equal("nf", l.ModulosAtivos);
    }

    // ── Trocar senha ─────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Trocar_senha_exige_login_senha_atual_e_minimo_de_8_caracteres_e_vale_no_proximo_login()
    {
        var loja = await NovaAsync();
        var email = EmailDe(loja);
        Assert.Equal(401, (await loja.Site.Post("/api/auth/trocar-senha", new { senhaAtual = "123456", novaSenha = "abcdefgh" })).Codigo);

        Assert.Equal((400, "Senha atual incorreta."), await Erro(loja.Painel.Post("/api/auth/trocar-senha", new { senhaAtual = "errada", novaSenha = "abcdefgh" })));
        Assert.Equal((400, "A nova senha deve ter pelo menos 8 caracteres."), await Erro(loja.Painel.Post("/api/auth/trocar-senha", new { senhaAtual = "123456", novaSenha = "1234567" })));

        var ok = await loja.Painel.Post("/api/auth/trocar-senha", new { senhaAtual = "123456", novaSenha = "abcdefgh" });
        Assert.Equal("Senha alterada com sucesso.", ok.Json.GetProperty("mensagem").GetString());
        Assert.Equal(401, (await loja.Site.Post("/api/auth/login", new { email, senha = "123456" })).Codigo);
        Assert.Equal(200, (await loja.Site.Post("/api/auth/login", new { email, senha = "abcdefgh" })).Codigo);
    }
}
