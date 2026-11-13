using System.Text.Json;
using LojaApi.Models;
using LojaApi.src.Models.Fiscal;
using LojaApi.Application.Fiscal;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Integracao;

// Emissão fiscal (NFC-e): configuração da loja, emitir, listar e cancelar. O provedor é de mentira (nada sai para a internet).
[Collection(ColecaoApi.Nome)]
public class FiscalApiTests(ApiFixture api)
{
    private async Task<LojaTeste> NovaAsync(bool moduloNfce = true)
    {
        var loja = await api.NovaLojaAsync(moduloReservasAtivo: false);
        if (moduloNfce)
            await api.NoBancoAsync(db => db.Lojas.Where(l => l.Id == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.ModulosAtivos, "nfce")));
        return loja;
    }

    private async Task ConfigurarAsync(LojaTeste loja) =>
        (await loja.Painel.Put("/api/fiscal/configuracao", new { provedor = "focus", ambiente = "homologacao", credencialToken = "token-secreto", inscricaoEstadual = "123", regimeTributario = "simples", serieNfce = "1" })).DeveSerSucesso();

    private Task<Guid> VendaAsync(LojaTeste loja) => api.NoBancoAsync(async db =>
    {
        var v = new Venda { LojaId = loja.Id, Total = 50, TotalFinal = 50, FormaPagamento = "pix" };
        db.Vendas.Add(v);
        await db.SaveChangesAsync();
        return v.Id;
    });

    private Task<EmissaoFiscal> EmissaoNoBanco(Guid id) => api.NoBancoAsync(db => db.EmissoesFiscais.AsNoTracking().FirstAsync(e => e.Id == id));

    // ── Configuração ─────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Configuracao_so_para_admin()
    {
        var loja = await NovaAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/fiscal/configuracao")).Codigo);

        var operador = await api.OperadorAsync(loja);
        Assert.Equal(403, (await operador.Get("/api/fiscal/configuracao")).Codigo);
        Assert.Equal(403, (await operador.Put("/api/fiscal/configuracao", new { provedor = "x", ambiente = "producao", serieNfce = "1" })).Codigo);
    }

    [FatoIntegracao]
    public async Task Sem_configuracao_devolve_os_padroes_sem_gravar_nada()
    {
        var loja = await NovaAsync();

        var c = (await loja.Painel.Get("/api/fiscal/configuracao")).Json;

        Assert.False(c.GetProperty("ativo").GetBoolean());
        Assert.Equal(JsonValueKind.Null, c.GetProperty("provedor").ValueKind);
        Assert.Equal(("homologacao", "1", 1, false),
            (c.GetProperty("ambiente").GetString(), c.GetProperty("serieNfce").GetString(), c.GetProperty("proximoNumeroNfce").GetInt32(), c.GetProperty("credencialConfigurada").GetBoolean()));
        Assert.Equal(0, await api.NoBancoAsync(db => db.ConfiguracoesFiscais.CountAsync(x => x.LojaId == loja.Id)));
    }

    [FatoIntegracao]
    public async Task Salvar_valida_o_ambiente_e_so_fica_ativo_com_provedor_e_credencial()
    {
        var loja = await NovaAsync();
        var invalido = await loja.Painel.Put("/api/fiscal/configuracao", new { provedor = "focus", ambiente = "teste", serieNfce = "1" });
        Assert.Equal((400, "Ambiente inválido."), (invalido.Codigo, invalido.Erro));

        var semCredencial = await loja.Painel.Put("/api/fiscal/configuracao", new { provedor = "focus", ambiente = "producao", serieNfce = "1" });
        Assert.False(semCredencial.Json.GetProperty("ativo").GetBoolean());
        Assert.False(semCredencial.Json.GetProperty("credencialConfigurada").GetBoolean());

        var completa = await loja.Painel.Put("/api/fiscal/configuracao", new { provedor = "focus", ambiente = "producao", credencialToken = "abc", inscricaoEstadual = "999", regimeTributario = "lucro", serieNfce = "  2 " });
        Assert.True(completa.Json.GetProperty("ativo").GetBoolean());
        Assert.True(completa.Json.GetProperty("credencialConfigurada").GetBoolean());
        Assert.Equal(("producao", "2", "999", "lucro"), (completa.Json.GetProperty("ambiente").GetString(), completa.Json.GetProperty("serieNfce").GetString(),
            completa.Json.GetProperty("inscricaoEstadual").GetString(), completa.Json.GetProperty("regimeTributario").GetString()));
        Assert.False(completa.Corpo.Contains("abc"));   // o token nunca volta na resposta
    }

    [FatoIntegracao]
    public async Task Salvar_sem_token_mantem_o_antigo_e_serie_vazia_vira_1()
    {
        var loja = await NovaAsync();
        await ConfigurarAsync(loja);

        var r = await loja.Painel.Put("/api/fiscal/configuracao", new { provedor = "focus", ambiente = "homologacao", credencialToken = " ", serieNfce = "" });

        Assert.True(r.Json.GetProperty("ativo").GetBoolean());   // o token continua gravado
        Assert.Equal("1", r.Json.GetProperty("serieNfce").GetString());
        Assert.Equal("token-secreto", (await api.NoBancoAsync(db => db.ConfiguracoesFiscais.AsNoTracking().FirstAsync(x => x.LojaId == loja.Id))).CredencialToken);

        // tirar o provedor desativa
        var semProvedor = await loja.Painel.Put("/api/fiscal/configuracao", new { provedor = "", ambiente = "homologacao", serieNfce = "1" });
        Assert.False(semProvedor.Json.GetProperty("ativo").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Cada_loja_tem_a_sua_configuracao()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        await ConfigurarAsync(a);

        Assert.False((await b.Painel.Get("/api/fiscal/configuracao")).Json.GetProperty("ativo").GetBoolean());
        Assert.True((await a.Painel.Get("/api/fiscal/configuracao")).Json.GetProperty("ativo").GetBoolean());
    }

    // ── Emitir ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Emitir_exige_admin_modulo_ativo_venda_da_loja_e_configuracao_ativa()
    {
        var semModulo = await NovaAsync(moduloNfce: false);
        var vendaSemModulo = await VendaAsync(semModulo);
        var r1 = await semModulo.Painel.Post($"/api/fiscal/vendas/{vendaSemModulo}/emitir");
        Assert.Equal((400, "O módulo de emissão fiscal (NFC-e) não está ativo para esta loja."), (r1.Codigo, r1.Erro));

        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        Assert.Equal(403, (await operador.Post($"/api/fiscal/vendas/{Guid.NewGuid()}/emitir")).Codigo);

        var inexistente = await loja.Painel.Post($"/api/fiscal/vendas/{Guid.NewGuid()}/emitir");
        Assert.Equal((404, "Venda não encontrada."), (inexistente.Codigo, inexistente.Erro));

        var outra = await NovaAsync();
        var vendaDaOutra = await VendaAsync(outra);
        Assert.Equal(404, (await loja.Painel.Post($"/api/fiscal/vendas/{vendaDaOutra}/emitir")).Codigo);

        var venda = await VendaAsync(loja);
        var semConfig = await loja.Painel.Post($"/api/fiscal/vendas/{venda}/emitir");
        Assert.Equal((400, "Emissão fiscal ainda não está configurada. Contrate e configure um provedor antes de emitir."), (semConfig.Codigo, semConfig.Erro));

        await loja.Painel.Put("/api/fiscal/configuracao", new { provedor = "focus", ambiente = "homologacao", serieNfce = "1" });   // sem credencial: inativa
        Assert.Equal(400, (await loja.Painel.Post($"/api/fiscal/vendas/{venda}/emitir")).Codigo);
        Assert.Equal(0, (await loja.Painel.Get($"/api/fiscal/vendas/{venda}/emissoes")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Emitir_registra_a_recusa_do_provedor_como_erro()
    {
        var loja = await NovaAsync();
        await ConfigurarAsync(loja);
        var venda = await VendaAsync(loja);
        api.ProvedorFiscal.Restaurar();

        var r = await loja.Painel.Post($"/api/fiscal/vendas/{venda}/emitir");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("erro", "nfce", "focus", "Nenhum provedor de emissão fiscal está configurado para esta loja."),
            (r.Json.GetProperty("status").GetString(), r.Json.GetProperty("tipo").GetString(), r.Json.GetProperty("provedor").GetString(), r.Json.GetProperty("motivoErro").GetString()));
        Assert.Equal(venda, r.Json.GetProperty("vendaId").GetGuid());
        var gravada = await EmissaoNoBanco(r.Json.GetProperty("id").GetGuid());
        Assert.Equal((loja.Id, "erro"), (gravada.LojaId, gravada.Status));
        Assert.NotNull(gravada.ProcessadaEm);
        Assert.NotNull(gravada.SolicitadaPorUsuarioId);
    }

    [FatoIntegracao]
    public async Task Emitir_guarda_os_dados_da_nota_autorizada()
    {
        var loja = await NovaAsync();
        await ConfigurarAsync(loja);
        var venda = await VendaAsync(loja);
        api.ProvedorFiscal.Responder(
            new ResultadoEmissaoFiscal(true, "autorizada", "CHAVE44", "123", "1", "PROT1", "http://danfe", "http://xml"),
            new ResultadoEmissaoFiscal(true, "cancelada"));
        try
        {
            var r = await loja.Painel.Post($"/api/fiscal/vendas/{venda}/emitir");

            Assert.Equal(200, r.Codigo);
            var j = r.Json;
            Assert.Equal(("autorizada", "CHAVE44", "123", "1", "PROT1", "http://danfe", "http://xml"),
                (j.GetProperty("status").GetString(), j.GetProperty("chaveAcesso").GetString(), j.GetProperty("numeroNota").GetString(), j.GetProperty("serieNota").GetString(),
                 j.GetProperty("protocolo").GetString(), j.GetProperty("urlDanfe").GetString(), j.GetProperty("urlXml").GetString()));
        }
        finally { api.ProvedorFiscal.Restaurar(); }
    }

    [FatoIntegracao]
    public async Task Listar_emissoes_vem_da_mais_nova_para_a_mais_antiga_e_so_da_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        await ConfigurarAsync(loja);
        var venda = await VendaAsync(loja);
        api.ProvedorFiscal.Restaurar();
        var primeira = (await loja.Painel.Post($"/api/fiscal/vendas/{venda}/emitir")).Json.GetProperty("id").GetGuid();
        var segunda = (await loja.Painel.Post($"/api/fiscal/vendas/{venda}/emitir")).Json.GetProperty("id").GetGuid();

        var lista = (await loja.Painel.Get($"/api/fiscal/vendas/{venda}/emissoes")).Json;

        Assert.Equal([segunda, primeira], lista.EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList());
        Assert.Equal(0, (await outra.Painel.Get($"/api/fiscal/vendas/{venda}/emissoes")).Json.GetArrayLength());
    }

    // ── Cancelar ─────────────────────────────────────────────────────────────

    private async Task<(LojaTeste Loja, Guid Emissao)> EmissaoAutorizadaAsync()
    {
        var loja = await NovaAsync();
        await ConfigurarAsync(loja);
        var venda = await VendaAsync(loja);
        var emissao = new EmissaoFiscal { LojaId = loja.Id, VendaId = venda, Tipo = "nfce", Status = "autorizada", Provedor = "focus", ChaveAcesso = "C" };
        await api.NoBancoAsync(async db => { db.EmissoesFiscais.Add(emissao); await db.SaveChangesAsync(); });
        return (loja, emissao.Id);
    }

    [FatoIntegracao]
    public async Task Cancelar_valida_status_justificativa_e_a_resposta_do_provedor()
    {
        var (loja, id) = await EmissaoAutorizadaAsync();
        var operador = await api.OperadorAsync(loja);
        Assert.Equal(403, (await operador.Post($"/api/fiscal/emissoes/{id}/cancelar", new { justificativa = new string('x', 20) })).Codigo);

        var curta = await loja.Painel.Post($"/api/fiscal/emissoes/{id}/cancelar", new { justificativa = "muito curta" });
        Assert.Equal((400, "Justificativa deve ter pelo menos 15 caracteres."), (curta.Codigo, curta.Erro));
        Assert.Equal(400, (await loja.Painel.Post($"/api/fiscal/emissoes/{id}/cancelar", new { justificativa = "               " })).Codigo);

        api.ProvedorFiscal.Restaurar();
        var recusada = await loja.Painel.Post($"/api/fiscal/emissoes/{id}/cancelar", new { justificativa = "Cliente desistiu da compra" });
        Assert.Equal((400, "Nenhum provedor de emissão fiscal está configurado para esta loja."), (recusada.Codigo, recusada.Erro));
        Assert.Equal("autorizada", (await EmissaoNoBanco(id)).Status);

        Assert.Equal(404, (await loja.Painel.Post($"/api/fiscal/emissoes/{Guid.NewGuid()}/cancelar", new { justificativa = new string('x', 20) })).Codigo);
    }

    [FatoIntegracao]
    public async Task Cancelar_nota_autorizada_marca_como_cancelada()
    {
        var (loja, id) = await EmissaoAutorizadaAsync();
        api.ProvedorFiscal.Responder(new ResultadoEmissaoFiscal(true, "autorizada"), new ResultadoEmissaoFiscal(true, "cancelada"));
        try
        {
            var r = await loja.Painel.Post($"/api/fiscal/emissoes/{id}/cancelar", new { justificativa = "  Cliente desistiu da compra  " });

            Assert.Equal(200, r.Codigo);
            Assert.Equal("cancelada", r.Json.GetProperty("status").GetString());
            Assert.Equal("cancelada", (await EmissaoNoBanco(id)).Status);

            var denovo = await loja.Painel.Post($"/api/fiscal/emissoes/{id}/cancelar", new { justificativa = "Cliente desistiu da compra" });
            Assert.Equal((400, "Só é possível cancelar uma nota autorizada."), (denovo.Codigo, denovo.Erro));
        }
        finally { api.ProvedorFiscal.Restaurar(); }
    }

    [FatoIntegracao]
    public async Task Outra_loja_nao_cancela_nota_alheia()
    {
        var (_, id) = await EmissaoAutorizadaAsync();
        var invasora = await NovaAsync();
        api.ProvedorFiscal.Responder(new ResultadoEmissaoFiscal(true, "autorizada"), new ResultadoEmissaoFiscal(true, "cancelada"));
        try
        {
            Assert.Equal(404, (await invasora.Painel.Post($"/api/fiscal/emissoes/{id}/cancelar", new { justificativa = "Tentativa de cancelar nota alheia" })).Codigo);
            Assert.Equal("autorizada", (await EmissaoNoBanco(id)).Status);
        }
        finally { api.ProvedorFiscal.Restaurar(); }
    }
}
