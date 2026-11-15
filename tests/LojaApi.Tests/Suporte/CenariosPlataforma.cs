using System.Net.Http.Json;
using System.Text;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Suporte;

// Ajudantes dos testes da Plataforma (faturas, assinatura e lojas criadas pelo superadmin).
public static class CenariosPlataforma
{
    public static async Task<Pagamento> FaturaPendenteAsync(this ApiFixture api, Guid lojaId) =>
        await api.NoBancoAsync(db => db.Pagamentos.AsNoTracking().Where(p => p.LojaId == lojaId && p.Status == "pendente").OrderBy(p => p.Vencimento).FirstAsync());

    public static Task<Pagamento> PagamentoNoBancoAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.Pagamentos.AsNoTracking().FirstAsync(p => p.Id == id));

    public static Task<Loja> LojaNoBancoAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.Lojas.AsNoTracking().FirstAsync(l => l.Id == id));

    public static Task<List<Pagamento>> PagamentosDaLojaNoBancoAsync(this ApiFixture api, Guid lojaId) =>
        api.NoBancoAsync(db => db.Pagamentos.AsNoTracking().Where(p => p.LojaId == lojaId).OrderBy(p => p.Vencimento).ToListAsync());

    // O webhook do Mercado Pago é um POST anônimo com o JSON cru no corpo
    public static async Task<HttpResponseMessage> WebhookAsync(this ApiFixture api, string json)
    {
        var http = api.Fabrica.CreateClient();
        return await http.PostAsync("/api/pagamentos/webhook", new StringContent(json, Encoding.UTF8, "application/json"));
    }

    public static string EventoMp(string tipo, string id) => "{\"type\":\"" + tipo + "\",\"data\":{\"id\":\"" + id + "\"}}";

    public static object CriarFatura(Guid pagamentoId, string forma, string? cardToken = null, int? parcelas = null) =>
        new { pagamentoId, formaPagamento = forma, cardToken, parcelas, cpfPagador = (string?)null, nomePagador = (string?)null, emailPagador = (string?)null };
}
