using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using LojaApi.Tests.Chacara.Dominio;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Integracao;

// Preparos do módulo Serviços/Agenda (serviços, agendamentos, planos de assinatura e agendamento online).
public static class CenariosServicos
{
    // Os endpoints de Serviços/Agenda não dependem do perfil da loja. Usa-se um perfil que NÃO cadastra serviços padrão
    // (o "Banho e Tosa" cadastra 5 no signup), para as listas dos testes serem exatas.
    public static async Task<LojaTeste> NovaLojaDeServicosAsync(this ApiFixture api) =>
        await api.NovaLojaAsync(moduloReservasAtivo: false);

    // Loja com a página de agendamento online ligada (agenda das 8h às 12h por padrão, para contas simples).
    public static async Task<LojaTeste> NovaLojaComAgendamentoOnlineAsync(this ApiFixture api, string confirmacao = "aprovacao",
        int horaInicio = 8, int horaFim = 12, DateTime? pausaAte = null, string? pausaMensagem = null)
    {
        var loja = await api.NovaLojaDeServicosAsync();
        await api.NoBancoAsync(async db =>
        {
            var l = await db.Lojas.FirstAsync(x => x.Id == loja.Id);
            l.AgendamentoOnlineAtivo = true;
            l.AgendamentoOnlineConfirmacao = confirmacao;
            l.AgendaHoraInicio = horaInicio;
            l.AgendaHoraFim = horaFim;
            l.PausaAte = pausaAte;
            l.PausaMensagem = pausaMensagem;
            await db.SaveChangesAsync();
        });
        return loja;
    }

    public static async Task<Resposta> CriarServicoAsync(this LojaTeste loja, string nome = "Banho", decimal preco = 80, int duracaoMin = 60,
        string categoria = "Banho e Tosa", bool ativo = true) =>
        await loja.Painel.Post("/api/servicos", new { nome, categoria, preco, duracaoMin, ativo });

    public static async Task<Guid> ServicoIdAsync(this LojaTeste loja, string nome = "Banho", decimal preco = 80, int duracaoMin = 60)
    {
        var r = await loja.CriarServicoAsync(nome, preco, duracaoMin);
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static async Task<Guid> ClienteIdAsync(this LojaTeste loja, string nome = "Maria Cliente", string telefone = "67999990000")
    {
        var r = await loja.Painel.Post("/api/clientes", new { nome, telefone });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static string Hora(this DateTime dia, int h, int min = 0) => $"{Datas.Texto(dia)}T{h:00}:{min:00}:00";

    public static object Agendamento(Guid servicoId, string dataHora, Guid? clienteId = null, string? nomeCliente = null,
        decimal preco = 0, int duracaoMin = 0, string? observacao = null, Guid? profissionalId = null) => new
    {
        servicoId, clienteId, nomeCliente, preco, dataHora, duracaoMin, observacao, profissionalId,
    };

    public static async Task<Resposta> AgendarAsync(this LojaTeste loja, Guid servicoId, string dataHora, Guid? clienteId = null,
        string? nomeCliente = null, decimal preco = 0, int duracaoMin = 0, Guid? profissionalId = null) =>
        await loja.Painel.Post("/api/agendamentos", Agendamento(servicoId, dataHora, clienteId, nomeCliente, preco, duracaoMin, null, profissionalId));

    public static async Task<Guid> ProfissionalAsync(this ApiFixture api, LojaTeste loja, decimal? comissaoPadrao = 10m, string nome = "Joana Tosadora")
    {
        var p = new Profissional { LojaId = loja.Id, Nome = nome, ComissaoPadraoPercentual = comissaoPadrao };
        await api.NoBancoAsync(async db => { db.Profissionais.Add(p); await db.SaveChangesAsync(); });
        return p.Id;
    }

    public static async Task<Agendamento> AgendamentoNoBancoAsync(this ApiFixture api, Guid id) =>
        await api.NoBancoAsync(db => db.Agendamentos.AsNoTracking().FirstAsync(a => a.Id == id));

    public static Task MarcarComoPagoAsync(this ApiFixture api, Guid agendamentoId) =>
        api.NoBancoAsync(db => db.Agendamentos.Where(a => a.Id == agendamentoId).ExecuteUpdateAsync(s => s.SetProperty(a => a.Pago, true)));
}
