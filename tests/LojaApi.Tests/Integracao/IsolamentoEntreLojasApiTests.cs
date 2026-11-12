using LojaApi.Tests.Chacara.Dominio;

namespace LojaApi.Tests.Integracao;

// Cada chácara só enxerga e altera os próprios dados. Aqui a loja "B" tenta mexer no que é da loja "A".
[Collection(ColecaoApi.Nome)]
public class IsolamentoEntreLojasApiTests(ApiFixture api)
{
    [FatoIntegracao]
    public async Task Loja_nao_ve_as_reservas_de_outra_loja()
    {
        var a = await api.PrepararParaReservasAsync();
        var b = await api.PrepararParaReservasAsync();
        var reservaA = (await a.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado(), nome: "Cliente da A")).Id;

        Assert.Equal(0, (await b.Painel.Get("/api/chacara/reservas")).Json.GetArrayLength());
        Assert.Equal(1, (await a.Painel.Get("/api/chacara/reservas")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get("/api/chacara/dashboard")).Json.GetProperty("geral").GetProperty("totalReservas").GetInt32());
        Assert.DoesNotContain("Cliente da A", (await b.Painel.Get("/api/chacara/reservas")).Corpo);
        Assert.NotNull(reservaA);
    }

    [FatoIntegracao]
    public async Task Loja_nao_consegue_alterar_nem_apagar_reserva_de_outra_loja()
    {
        var a = await api.PrepararParaReservasAsync();
        var b = await api.PrepararParaReservasAsync();
        var id = (await a.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;   // pendente da loja A

        var tentativas = new List<Resposta>
        {
            await b.Painel.Patch($"/api/chacara/reservas/{id}/confirmar", new { valorPago = 1 }),
            await b.Painel.Patch($"/api/chacara/reservas/{id}/marcar-expirada"),
            await b.Painel.Patch($"/api/chacara/reservas/{id}/manter-negociacao"),
            await b.Painel.Patch($"/api/chacara/reservas/{id}/desfazer-negociacao"),
            await b.Painel.Patch($"/api/chacara/reservas/{id}/reativar"),
            await b.Painel.Patch($"/api/chacara/reservas/{id}/prejuizo", new { valor = 10, observacao = "x" }),
            await b.Painel.Patch($"/api/chacara/reservas/{id}/avaliar-cliente", new { nota = 1, comentario = "x" }),
            await b.Painel.Patch($"/api/chacara/reservas/{id}/registrar-pagamento", new { valor = 10 }),
            await b.Painel.Put($"/api/chacara/reservas/{id}", new { dataInicio = Datas.Texto(Datas.Sexta()), dataFim = Datas.Texto(Datas.Sabado()), pessoas = 30, clienteNome = "Invasor", clienteEmail = "i@i.com", clienteTelefone = "1" }),
            await b.Painel.Get($"/api/chacara/reservas/{id}/contrato-pdf"),
            await b.Painel.Post($"/api/chacara/reservas/{id}/enviar-contrato"),
            await b.Painel.Delete($"/api/chacara/reservas/{id}"),
        };

        Assert.All(tentativas, r => Assert.Equal(404, r.Codigo));

        var intacta = await api.ReservaNoBancoAsync(id);
        Assert.Equal("pendente_pagamento", intacta.Status);
        Assert.Equal("João Cliente", intacta.ClienteNome);
        Assert.Equal(0m, intacta.ValorPago);
        Assert.Null(intacta.NotaCliente);
        Assert.Null(intacta.ValorPrejuizo);
    }

    [FatoIntegracao]
    public async Task As_mesmas_datas_podem_ser_reservadas_em_lojas_diferentes()
    {
        var a = await api.PrepararParaReservasAsync();
        var b = await api.PrepararParaReservasAsync();
        await a.CriarReservaManualAsync(Datas.Sexta(), Datas.Domingo());

        Assert.Equal(200, (await b.CriarReservaManualAsync(Datas.Sexta(), Datas.Domingo())).Codigo);
        Assert.Equal(200, (await b.ReservarPeloSiteAsync(Datas.Sexta(1), Datas.Sabado(1))).Codigo);

        var disp = await b.Site.Get($"{b.Publico}/disponibilidade?dataInicio={Datas.Texto(Datas.Sexta())}&dataFim={Datas.Texto(Datas.Sexta())}");
        Assert.False(disp.Json.GetProperty("disponivel").GetBoolean());   // ocupada pela PRÓPRIA reserva da B

        var livreNaA = await a.Site.Get($"{a.Publico}/disponibilidade?dataInicio={Datas.Texto(Datas.Sexta(1))}&dataFim={Datas.Texto(Datas.Sexta(1))}");
        Assert.True(livreNaA.Json.GetProperty("disponivel").GetBoolean()); // a reserva da B não afeta a A
    }

    [FatoIntegracao]
    public async Task Cadastros_de_uma_loja_nao_aparecem_nem_podem_ser_alterados_por_outra()
    {
        var a = await api.PrepararParaReservasAsync();
        var b = await api.NovaLojaAsync();

        var faixa = (await a.Painel.Get("/api/chacara/faixas-preco")).Json[0].GetProperty("id").GetInt32();
        var horario = (await a.Painel.Post("/api/chacara/horarios", new { tipo = "entrada", hora = "09:00", ajuste = 0, ordem = 0 })).Id;
        var ini = Datas.Sexta(10);
        var periodo = (await a.Painel.Post("/api/chacara/periodos-especiais", new { nome = "Natal", dataInicio = Datas.Texto(ini), dataFim = Datas.Texto(ini.AddDays(1)), valorTotal = 1000 })).Id;
        var foto = (await a.Painel.Post("/api/chacara/fotos", new { url = "https://img.test/a.jpg" })).Id;

        Assert.Equal(0, (await b.Painel.Get("/api/chacara/faixas-preco")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get("/api/chacara/horarios")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get("/api/chacara/periodos-especiais")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get("/api/chacara/fotos")).Json.GetArrayLength());

        Assert.Equal(404, (await b.Painel.Put($"/api/chacara/faixas-preco/{faixa}", new { pessoasAte = 5, valorDiariaSemana = 1, valorDiariaFimSemana = 1, valorPacote2DiasFimSemana = 1 })).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/chacara/faixas-preco/{faixa}")).Codigo);
        Assert.Equal(404, (await b.Painel.Put($"/api/chacara/horarios/{horario}", new { tipo = "entrada", hora = "10:00", ajuste = 0, ordem = 0 })).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/chacara/horarios/{horario}")).Codigo);
        Assert.Equal(404, (await b.Painel.Put($"/api/chacara/periodos-especiais/{periodo}", new { nome = "X", dataInicio = Datas.Texto(ini), dataFim = Datas.Texto(ini), valorTotal = 1 })).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/chacara/fotos/{foto}")).Codigo);

        // nada mudou na loja A
        Assert.Equal(2, (await a.Painel.Get("/api/chacara/faixas-preco")).Json.GetArrayLength());
        Assert.Equal(1, (await a.Painel.Get("/api/chacara/fotos")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Configuracoes_de_cada_loja_sao_independentes()
    {
        var a = await api.NovaLojaAsync();
        var b = await api.NovaLojaAsync();
        await a.Painel.Put("/api/chacara/configuracao-preco", new { minimoPessoas = 9, limitePessoasParaTaxaLimpeza = 90, valorTaxaLimpeza = 900, valorMultaNaoLimpeza = 90, percentualEntradaMinimo = 90 });

        Assert.Equal(9, (await a.Painel.Get("/api/chacara/configuracao-preco")).Json.GetProperty("minimoPessoas").GetInt32());
        Assert.Equal(3, (await b.Painel.Get("/api/chacara/configuracao-preco")).Json.GetProperty("minimoPessoas").GetInt32());

        await a.Painel.Put("/api/chacara/info", new { descricao = "da A", endereco = "rua A", comodidades = "" });
        Assert.Equal("", (await b.Painel.Get("/api/chacara/info")).Json.GetProperty("descricao").GetString());
    }

    [FatoIntegracao]
    public async Task Site_publico_de_uma_loja_nao_alcanca_reservas_de_outra()
    {
        var a = await api.PrepararParaReservasAsync(comPagamentoOnline: true);
        var b = await api.PrepararParaReservasAsync(comPagamentoOnline: true);
        var id = (await a.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Domingo())).Id;

        // usar o endereço da loja B com o número da reserva da loja A
        string Rota(string fim) => $"{b.Publico}/reservas/{id}/{fim}";
        Assert.Equal(404, (await b.Site.Get(Rota("pagamento/status"))).Codigo);
        Assert.Equal(404, (await b.Site.Post(Rota("pagamento/escolher"), new { formaPagamento = "pix" })).Codigo);
        Assert.Equal(404, (await b.Site.Post(Rota("pagamento/pix"))).Codigo);
        Assert.Equal(404, (await b.Site.Post(Rota("pagamento/cartao"), new { token = "t", parcelas = 1 })).Codigo);
        Assert.Equal(404, (await b.Site.Post(Rota("cancelar"))).Codigo);
        Assert.Equal(404, (await b.Site.Get($"{b.Publico}/avaliacao/{id}")).Codigo);

        Assert.Equal("pendente_pagamento", (await api.ReservaNoBancoAsync(id)).Status);
        Assert.Null((await api.ReservaNoBancoAsync(id)).FormaPagamento);
    }

    [FatoIntegracao]
    public async Task Credencial_do_Mercado_Pago_de_uma_loja_nao_vale_para_outra()
    {
        var a = await api.NovaLojaAsync();
        var b = await api.NovaLojaAsync();
        (await a.Painel.Put("/api/chacara/info/pagamento", new { accessToken = "APP_USR-so-da-A", publicKey = "pk-a" })).DeveSerSucesso();

        Assert.True((await a.Painel.Get("/api/chacara/info/pagamento")).Json.GetProperty("configurado").GetBoolean());
        Assert.False((await b.Painel.Get("/api/chacara/info/pagamento")).Json.GetProperty("configurado").GetBoolean());
        Assert.False((await b.Site.Get($"{b.Publico}/dados")).Json.GetProperty("pagamentoOnline").GetBoolean());
    }
}
