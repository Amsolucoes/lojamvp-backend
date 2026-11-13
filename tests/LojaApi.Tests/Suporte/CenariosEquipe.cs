using LojaApi.src.Models.Funcionarios;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Suporte;

// Preparos da Equipe (funcionários/profissionais e comissões).
public static class CenariosEquipe
{
    public static Task<Resposta> NovoFuncionarioAsync(this LojaTeste loja, string nome = "Joana Tosadora", decimal? comissao = 10m, string tipo = "comissao",
        decimal? salario = null, decimal? diaria = null, Guid? conta = null, int? dia = null, bool ativo = true, string baseCalculo = "total",
        bool comissaoVendas = false, decimal? percentualVendas = null, string? telefone = null, string? cep = null, string? endereco = null,
        string? numero = null, string? bairro = null, string? cidade = null, string? uf = null) =>
        loja.Painel.Post("/api/funcionarios", Corpo(nome, comissao, tipo, salario, diaria, conta, dia, ativo, baseCalculo, comissaoVendas, percentualVendas, telefone, cep, endereco, numero, bairro, cidade, uf));

    public static object Corpo(string nome = "Joana Tosadora", decimal? comissao = 10m, string tipo = "comissao", decimal? salario = null, decimal? diaria = null,
        Guid? conta = null, int? dia = null, bool ativo = true, string baseCalculo = "total", bool comissaoVendas = false, decimal? percentualVendas = null,
        string? telefone = null, string? cep = null, string? endereco = null, string? numero = null, string? bairro = null, string? cidade = null, string? uf = null) => new
    {
        nome, comissaoPadraoPercentual = comissao, ativo, diaPagamentoPadrao = dia, tipoRemuneracao = tipo, salarioFixo = salario, contaBancariaId = conta,
        telefone, cep, endereco, comissaoBaseCalculo = baseCalculo, valorDiaria = diaria, comissaoVendasAtiva = comissaoVendas, comissaoVendasPercentual = percentualVendas,
        numero, bairro, cidade, uf,
    };

    public static async Task<Guid> FuncionarioIdAsync(this LojaTeste loja, string nome = "Joana Tosadora", decimal? comissao = 10m, string tipo = "comissao",
        decimal? salario = null, decimal? diaria = null, Guid? conta = null, int? dia = null)
    {
        var r = await loja.NovoFuncionarioAsync(nome, comissao, tipo, salario, diaria, conta, dia);
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static async Task<Guid> ContaBancariaIdAsync(this LojaTeste loja, string nome = "Conta da Equipe")
    {
        var r = await loja.Painel.Post("/api/financeiro/contas", new { nome, saldoInicial = 0, banco = "Banco", limite = 0 });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    // Agenda um serviço com o profissional e conclui: nasce uma comissão pendente (preço x percentual do profissional)
    public static async Task<Guid> ComissaoDeAgendamentoAsync(this LojaTeste loja, Guid servicoId, Guid profissionalId, decimal preco = 100m, string? cliente = "Cliente Avulso", int hora = 9)
    {
        var ag = await loja.AgendarAsync(servicoId, $"{Datas.Texto(Datas.Segunda())}T{hora:00}:00:00", nomeCliente: cliente, preco: preco, profissionalId: profissionalId);
        ag.DeveSerSucesso();
        var id = ag.Json.GetProperty("id").GetGuid();
        (await loja.Painel.Patch($"/api/agendamentos/{id}/status", new { status = "concluido" })).DeveSerSucesso();
        return id;
    }

    public static Task<List<ComissaoFuncionario>> ComissoesDoProfissionalAsync(this ApiFixture api, Guid profissionalId) =>
        api.NoBancoAsync(db => db.ComissoesFuncionario.AsNoTracking().Where(c => c.ProfissionalId == profissionalId).OrderBy(c => c.CriadoEm).ToListAsync());
}
