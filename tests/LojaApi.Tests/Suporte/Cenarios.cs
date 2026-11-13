using LojaApi.Domain.Chacara;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Suporte;

// Preparos que vários testes repetem.
public static class Cenarios
{
    // Dados completos do locador (sem isso o site público não aceita reservas) + faixas de preço:
    //   até 50 pessoas: semana 500 / fim de semana 800 / pacote sex+sáb 1.400
    //   até 100 pessoas: semana 700 / fim de semana 1.000 / pacote 1.800
    // Mínimo de 3 pessoas e taxa de limpeza de R$ 250 acima de 50 pessoas (padrões).
    public static async Task<LojaTeste> PrepararParaReservasAsync(this ApiFixture api, bool comPagamentoOnline = false)
    {
        var loja = await api.NovaLojaAsync();

        (await loja.Painel.Put("/api/chacara/info", new
        {
            descricao = "Chácara linda", endereco = "Rua A, 10", comodidades = "piscina,wifi",
            horaEntrada = "08:00", horaSaida = "22:00",
            locadorNome = "Fulano de Tal", locadorCpf = "123.456.789-00", locadorEndereco = "Rua do Locador, 5",
            locadorTelefone = "67999990000", cidadeAssinatura = "Campo Grande",
        })).DeveSerSucesso();

        (await loja.Painel.Post("/api/chacara/faixas-preco", new { pessoasAte = 50, valorDiariaSemana = 500, valorDiariaFimSemana = 800, valorPacote2DiasFimSemana = 1400 })).DeveSerSucesso();
        (await loja.Painel.Post("/api/chacara/faixas-preco", new { pessoasAte = 100, valorDiariaSemana = 700, valorDiariaFimSemana = 1000, valorPacote2DiasFimSemana = 1800 })).DeveSerSucesso();

        if (comPagamentoOnline)
            (await loja.Painel.Put("/api/chacara/info/pagamento", new { accessToken = "APP_USR-token-de-teste", publicKey = "APP_USR-chave-publica" })).DeveSerSucesso();

        return loja;
    }

    public static void DeveSerSucesso(this Resposta r)
    {
        if (!r.Sucesso) throw new Xunit.Sdk.XunitException($"Esperava sucesso, mas a API respondeu {r.Codigo}: {r.Corpo}");
    }

    public static object ReservaManual(DateTime ini, DateTime fim, int pessoas = 30, string nome = "Maria Silva",
        decimal valor = 2200, decimal? valorPago = null, string? email = "maria@x.com") => new
    {
        dataInicio = Datas.Texto(ini), dataFim = Datas.Texto(fim), pessoas,
        clienteNome = nome, clienteEmail = email, clienteTelefone = "(67) 99999-0000",
        valor, valorPago,
    };

    public static async Task<Resposta> CriarReservaManualAsync(this LojaTeste loja, DateTime ini, DateTime fim, int pessoas = 30,
        string nome = "Maria Silva", decimal valor = 2200, decimal? valorPago = null, string? email = "maria@x.com") =>
        await loja.Painel.Post("/api/chacara/reservas", ReservaManual(ini, fim, pessoas, nome, valor, valorPago, email));

    // Reserva feita pelo cliente no site público (fica pendente).
    public static object ReservaDoSite(DateTime ini, DateTime fim, int pessoas = 30, string nome = "João Cliente",
        string email = "joao@x.com", string? documento = "111.222.333-44", string? horaEntrada = null, string? horaSaida = null) => new
    {
        dataInicio = Datas.Texto(ini), dataFim = Datas.Texto(fim), pessoas,
        clienteNome = nome, clienteEmail = email, clienteTelefone = "(67) 98888-7777",
        clienteDocumento = documento, horaEntrada, horaSaida,
    };

    public static async Task<Resposta> ReservarPeloSiteAsync(this LojaTeste loja, DateTime ini, DateTime fim, int pessoas = 30,
        string nome = "João Cliente", string email = "joao@x.com", string? documento = "111.222.333-44") =>
        await loja.Site.Post($"{loja.Publico}/reservar", ReservaDoSite(ini, fim, pessoas, nome, email, documento));

    public static async Task<Reserva> ReservaNoBancoAsync(this ApiFixture api, int id) =>
        await api.NoBancoAsync(db => db.Reservas.AsNoTracking().FirstAsync(r => r.Id == id));

    public static async Task<JsonElement> ReservaNaListaAsync(this LojaTeste loja, int id)
    {
        var lista = (await loja.Painel.Get("/api/chacara/reservas")).Json;
        return lista.EnumerateArray().First(r => r.GetProperty("id").GetInt32() == id);
    }
}
