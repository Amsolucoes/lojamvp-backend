using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.PlataformaTeste;

// Perfis de loja, preços dos módulos e notificações push (sem o servidor de push configurado, como no ambiente de testes).
[Collection(ColecaoApi.Nome)]
public class PerfisModulosPushApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    // Um perfil ativo que traz categorias (nem todos trazem)
    private async Task<string> PerfilComCategoriasAsync()
    {
        var lista = (await api.Fabrica.CreateClient().GetFromJsonAsync<System.Text.Json.JsonElement>("/api/perfis")).EnumerateArray();
        return lista.First(p => p.GetProperty("categorias").GetArrayLength() > 0).GetProperty("id").GetString()!;
    }

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    // ── Perfis ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Perfis_lista_so_os_ativos_sem_login_com_categorias_e_campos_ordenados()
    {
        var loja = await NovaAsync();
        var perfilId = await PerfilComCategoriasAsync();

        var r = await loja.Site.Get("/api/perfis");

        Assert.Equal(200, r.Codigo);
        var perfil = r.Json.EnumerateArray().Single(p => p.GetProperty("id").GetString() == perfilId);
        Assert.Equal(["id", "nome", "descricao", "icone", "tipoPlanoAplica", "categorias", "camposExtras"], perfil.EnumerateObject().Select(x => x.Name));
        var ordens = perfil.GetProperty("categorias").EnumerateArray().Select(c => c.GetProperty("ordem").GetInt32()).ToList();
        Assert.NotEmpty(ordens);
        Assert.Equal(ordens.Order().ToList(), ordens);

        // perfil inativo não aparece (cria um para o teste não depender do que já existe no banco)
        var nomeInativo = $"Inativo {Guid.NewGuid():N}"[..24];
        await api.NoBancoAsync(async db =>
        {
            db.PerfisLoja.Add(new LojaApi.Models.PerfilLoja { Nome = nomeInativo, Ativo = false });
            await db.SaveChangesAsync();
        });
        var nomes = (await loja.Site.Get("/api/perfis")).Json.EnumerateArray().Select(p => p.GetProperty("nome").GetString()).ToList();
        Assert.DoesNotContain(nomeInativo, nomes);
        Assert.Contains(r.Json.EnumerateArray().First().GetProperty("nome").GetString(), nomes);
    }

    [FatoIntegracao]
    public async Task Aplicar_exige_login_recusa_perfil_ja_definido_e_perfil_inexistente()
    {
        var perfilId = await PerfilComCategoriasAsync();
        var loja = await api.NovaLojaAsync(perfilId: perfilId);

        Assert.Equal(401, (await loja.Site.Post("/api/perfis/aplicar", new { perfilLojaId = perfilId })).Codigo);
        Assert.Equal((403, "O perfil da sua loja já foi definido. Para alterar, fale com o suporte da AldevSoftware."),
            await Erro(loja.Painel.Post("/api/perfis/aplicar", new { perfilLojaId = perfilId })));

        await api.NoBancoAsync(db => db.CategoriasLoja.Where(c => c.LojaId == loja.Id).ExecuteDeleteAsync());
        Assert.Equal((404, "Perfil não encontrado."), await Erro(loja.Painel.Post("/api/perfis/aplicar", new { perfilLojaId = Guid.NewGuid() })));
    }

    [FatoIntegracao]
    public async Task Aplicar_copia_categorias_e_campos_do_perfil_para_a_loja_sem_perfil()
    {
        var perfilId = await PerfilComCategoriasAsync();
        var loja = await NovaAsync();
        await api.NoBancoAsync(async db =>
        {
            await db.CategoriasLoja.Where(c => c.LojaId == loja.Id).ExecuteDeleteAsync();
            await db.CamposExtrasLoja.Where(c => c.LojaId == loja.Id).ExecuteDeleteAsync();
        });
        var esperadas = await api.NoBancoAsync(db => db.CategoriasPerfilLoja.Where(c => c.PerfilLojaId == Guid.Parse(perfilId)).Select(c => c.Nome).ToListAsync());

        var r = await loja.Painel.Post("/api/perfis/aplicar", new { perfilLojaId = perfilId });

        Assert.Equal(200, r.Codigo);
        Assert.StartsWith("Perfil '", r.Json.GetProperty("mensagem").GetString());
        Assert.EndsWith("' aplicado!", r.Json.GetProperty("mensagem").GetString());
        var categorias = await api.NoBancoAsync(db => db.CategoriasLoja.Where(c => c.LojaId == loja.Id).Select(c => c.Nome).ToListAsync());
        Assert.Equal(esperadas.Order(), categorias.Order());
        Assert.Equal(403, (await loja.Painel.Post("/api/perfis/aplicar", new { perfilLojaId = perfilId })).Codigo);      // agora já tem perfil
    }

    [FatoIntegracao]
    public async Task Aplicar_com_campos_extras_antigos_e_sem_categorias_substitui_os_campos()
    {
        var perfilId = await PerfilComCategoriasAsync();
        var loja = await NovaAsync();
        await api.NoBancoAsync(async db =>
        {
            await db.CategoriasLoja.Where(c => c.LojaId == loja.Id).ExecuteDeleteAsync();
            await db.CamposExtrasLoja.Where(c => c.LojaId == loja.Id).ExecuteDeleteAsync();
            db.CamposExtrasLoja.Add(new LojaApi.Models.CampoExtraLoja { LojaId = loja.Id, Chave = "zz_antigo", Label = "Antigo" });
            await db.SaveChangesAsync();
        });

        (await loja.Painel.Post("/api/perfis/aplicar", new { perfilLojaId = perfilId })).DeveSerSucesso();

        var chaves = await api.NoBancoAsync(db => db.CamposExtrasLoja.Where(c => c.LojaId == loja.Id).Select(c => c.Chave).ToListAsync());
        Assert.DoesNotContain("zz_antigo", chaves);                                                        // os campos antigos saem junto com as categorias
        var doPerfil = await api.NoBancoAsync(db => db.CamposExtrasPerfil.Where(c => c.PerfilLojaId == Guid.Parse(perfilId)).Select(c => c.Chave).ToListAsync());
        Assert.Equal(doPerfil.Order(), chaves.Order());
    }

    [FatoIntegracao]
    public async Task Categorias_e_campos_da_loja_listam_so_os_ativos_dela_e_adicionar_categoria_cria()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();

        var criada = await loja.Painel.Post("/api/perfis/loja/categorias", new { nome = "Zzz Nova", ordem = 99 });

        Assert.Equal(200, criada.Codigo);
        Assert.Equal(("Zzz Nova", true, 99), (criada.Json.GetProperty("nome").GetString()!, criada.Json.GetProperty("ativo").GetBoolean(), criada.Json.GetProperty("ordem").GetInt32()));
        Assert.Equal(["id", "nome", "ativo", "ordem"], criada.Json.EnumerateObject().Select(x => x.Name));

        var minhas = (await loja.Painel.Get("/api/perfis/loja/categorias")).Json.EnumerateArray().ToList();
        Assert.Equal("Zzz Nova", minhas.Last().GetProperty("nome").GetString());                                                    // ordem 99, a última
        Assert.Equal(["id", "nome", "ativo", "ordem", "tipoTamanho"], minhas[0].EnumerateObject().Select(x => x.Name));
        Assert.DoesNotContain((await outra.Painel.Get("/api/perfis/loja/categorias")).Json.EnumerateArray(), c => c.GetProperty("nome").GetString() == "Zzz Nova");

        await api.NoBancoAsync(db => db.CategoriasLoja.Where(c => c.LojaId == loja.Id && c.Nome == "Zzz Nova").ExecuteUpdateAsync(s => s.SetProperty(c => c.Ativo, false)));
        Assert.DoesNotContain((await loja.Painel.Get("/api/perfis/loja/categorias")).Json.EnumerateArray(), c => c.GetProperty("nome").GetString() == "Zzz Nova");

        Assert.Equal(200, (await loja.Painel.Get("/api/perfis/loja/campos-extras")).Codigo);
        Assert.Equal(401, (await loja.Site.Get("/api/perfis/loja/categorias")).Codigo);
        Assert.Equal(401, (await loja.Site.Get("/api/perfis/loja/campos-extras")).Codigo);
    }

    // ── Preços dos módulos ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Modulos_preco_exige_login_lista_por_nome_e_so_superadmin_altera()
    {
        var loja = await NovaAsync();
        var superadmin = await api.SuperadminAsync(loja);
        var operador = await api.OperadorAsync(loja);

        Assert.Equal(401, (await loja.Site.Get("/api/modulos-preco")).Codigo);
        var lista = (await operador.Get("/api/modulos-preco")).Json.EnumerateArray().ToList();
        Assert.NotEmpty(lista);
        var nomes = lista.Select(m => m.GetProperty("nome").GetString()!).ToList();
        Assert.Equal(nomes.OrderBy(n => n, StringComparer.Ordinal).Count(), nomes.Count);
        Assert.Equal(["id", "chave", "nome", "valor", "disponivelParaAtivar", "atualizadoEm"], lista[0].EnumerateObject().Select(x => x.Name));

        var id = lista[0].GetProperty("id").GetGuid();
        Assert.Equal(403, (await operador.Put($"/api/modulos-preco/{id}", new { valor = 1m, disponivelParaAtivar = true })).Codigo);
        Assert.Equal(404, (await superadmin.Put($"/api/modulos-preco/{Guid.NewGuid()}", new { valor = 1m, disponivelParaAtivar = true })).Codigo);
    }

    [FatoIntegracao]
    public async Task Superadmin_altera_valor_e_disponibilidade_do_modulo()
    {
        var loja = await NovaAsync();
        var superadmin = await api.SuperadminAsync(loja);
        var original = await api.NoBancoAsync(db => db.ModulosPreco.AsNoTracking().FirstAsync(m => m.Chave == "turmas"));
        try
        {
            var r = await superadmin.Put($"/api/modulos-preco/{original.Id}", new { valor = 12.34m, disponivelParaAtivar = !original.DisponivelParaAtivar });

            Assert.Equal(200, r.Codigo);
            Assert.Equal((12.34m, !original.DisponivelParaAtivar, "turmas"), (r.Json.GetProperty("valor").GetDecimal(), r.Json.GetProperty("disponivelParaAtivar").GetBoolean(), r.Json.GetProperty("chave").GetString()));
            var banco = await api.NoBancoAsync(db => db.ModulosPreco.AsNoTracking().FirstAsync(m => m.Id == original.Id));
            Assert.Equal((12.34m, !original.DisponivelParaAtivar), (banco.Valor, banco.DisponivelParaAtivar));
            Assert.True(banco.AtualizadoEm > original.AtualizadoEm);
        }
        finally
        {
            await api.NoBancoAsync(db => db.ModulosPreco.Where(m => m.Id == original.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Valor, original.Valor).SetProperty(m => m.DisponivelParaAtivar, original.DisponivelParaAtivar)));
        }
    }

    // ── Push ─────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Push_exige_login_e_sem_servidor_configurado_chave_e_teste_dao_503()
    {
        var loja = await NovaAsync();

        Assert.Equal(401, (await loja.Site.Get("/api/push/chave-publica")).Codigo);
        Assert.Equal(401, (await loja.Site.Post("/api/push/assinar", new { })).Codigo);
        Assert.Equal((503, "Notificações push não configuradas no servidor."), await Erro(loja.Painel.Get("/api/push/chave-publica")));
        Assert.Equal((503, "Notificações push não configuradas no servidor."), await Erro(loja.Painel.Post("/api/push/testar")));
    }

    [FatoIntegracao]
    public async Task Push_assinar_valida_cria_atualiza_o_mesmo_aparelho_e_cancelar_so_remove_o_proprio()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var endpoint = $"https://push.exemplo/{Guid.NewGuid():N}";

        Assert.Equal((400, "Assinatura inválida."), await Erro(loja.Painel.Post("/api/push/assinar", new { endpoint = " ", chavePublica = "p", chaveAuth = "a" })));
        Assert.Equal((400, "Assinatura inválida."), await Erro(loja.Painel.Post("/api/push/assinar", new { endpoint, chavePublica = "", chaveAuth = "a" })));

        var status0 = (await loja.Painel.Get($"/api/push/status?endpoint={Uri.EscapeDataString(endpoint)}")).Json;
        Assert.Equal((false, false), (status0.GetProperty("configurado").GetBoolean(), status0.GetProperty("ativo").GetBoolean()));

        Assert.True((await loja.Painel.Post("/api/push/assinar", new { endpoint, chavePublica = "p1", chaveAuth = "a1" })).Json.GetProperty("ativo").GetBoolean());
        Assert.True((await loja.Painel.Get($"/api/push/status?endpoint={Uri.EscapeDataString(endpoint)}")).Json.GetProperty("ativo").GetBoolean());
        Assert.False((await outra.Painel.Get($"/api/push/status?endpoint={Uri.EscapeDataString(endpoint)}")).Json.GetProperty("ativo").GetBoolean());
        Assert.False((await loja.Painel.Get("/api/push/status")).Json.GetProperty("ativo").GetBoolean());

        // o mesmo aparelho trocando de usuário/loja: atualiza em vez de duplicar
        await api.NoBancoAsync(db => db.AssinaturasPush.Where(a => a.Endpoint == endpoint).ExecuteUpdateAsync(s => s.SetProperty(a => a.UltimoAvisoEm, DateTime.UtcNow)));
        (await outra.Painel.Post("/api/push/assinar", new { endpoint, chavePublica = "p2", chaveAuth = "a2" })).DeveSerSucesso();
        var linha = await api.NoBancoAsync(db => db.AssinaturasPush.AsNoTracking().Where(a => a.Endpoint == endpoint).ToListAsync());
        var unica = Assert.Single(linha);
        Assert.Equal(("p2", "a2", null), (unica.ChavePublica, unica.ChaveAuth, (DateTime?)unica.UltimoAvisoEm));

        // quem não é dono não cancela; o dono cancela
        Assert.False((await loja.Painel.Post("/api/push/cancelar", new { endpoint })).Json.GetProperty("ativo").GetBoolean());
        Assert.Single(await api.NoBancoAsync(db => db.AssinaturasPush.Where(a => a.Endpoint == endpoint).ToListAsync()));
        Assert.False((await outra.Painel.Post("/api/push/cancelar", new { endpoint })).Json.GetProperty("ativo").GetBoolean());
        Assert.Empty(await api.NoBancoAsync(db => db.AssinaturasPush.Where(a => a.Endpoint == endpoint).ToListAsync()));
    }
}
