using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using LojaApi.src.Models.OrdemServico;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Suporte;

// Preparos de Ordem de Serviço (orçamentos, checklist e patrimônio).
public static class CenariosOrdemServico
{
    public const string Rota = "/api/ordemservico";

    public static object Peca(Guid? produto, string descricao = "Pastilha de freio", int quantidade = 1, decimal valor = 100m) =>
        new { tipo = "peca", produtoId = produto, servicoId = (Guid?)null, descricao, quantidade, valorUnitario = valor };

    public static object MaoDeObra(string descricao = "Alinhamento", int quantidade = 1, decimal valor = 80m, Guid? servico = null) =>
        new { tipo = "servico", produtoId = (Guid?)null, servicoId = servico, descricao, quantidade, valorUnitario = valor };

    public static object Mecanico(Guid profissional, decimal percentual = 0m) => new { profissionalId = profissional, comissaoPercentual = percentual };

    public static object Resposta(Guid itemChecklist, string estado = "bom", string? observacao = null) => new { checklistItemId = itemChecklist, estado, observacao };

    public static object CorpoOrcamento(Guid cliente, object[] itens, object[]? mecanicos = null, object[]? checklist = null,
        string? veiculo = "Gol 2015", string? placa = "abc1d23", string? observacoes = null) => new
    {
        clienteId = cliente, veiculoDescricao = veiculo, placa, observacoes, itens, mecanicos = mecanicos ?? [], checklistRespostas = checklist,
    };

    public static async Task<Guid> OrcamentoAsync(this LojaTeste loja, Guid cliente, object[] itens, object[]? mecanicos = null, object[]? checklist = null,
        string? veiculo = "Gol 2015", string? placa = "abc1d23")
    {
        var r = await loja.Painel.Post($"{Rota}/orcamentos", CorpoOrcamento(cliente, itens, mecanicos, checklist, veiculo, placa));
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static async Task<Guid> OrcamentoEmAndamentoAsync(this LojaTeste loja, Guid cliente, object[] itens, object[]? mecanicos = null)
    {
        var id = await loja.OrcamentoAsync(cliente, itens, mecanicos);
        (await loja.Painel.Patch($"{Rota}/orcamentos/{id}/aprovar")).DeveSerSucesso();
        return id;
    }

    public static Task<Resposta> ConcluirAsync(this LojaTeste loja, Guid orcamento, Guid conta, DateTime? vencimento = null, bool jaPago = false,
        string? forma = null, int? parcelas = null) =>
        loja.Painel.Patch($"{Rota}/orcamentos/{orcamento}/concluir", new { contaBancariaId = conta, vencimento, jaPago, formaPagamento = forma, parcelas });

    public static async Task<Guid> ChecklistCategoriaAsync(this LojaTeste loja, string nome = "Freios", int ordem = 0)
    {
        var r = await loja.Painel.Post($"{Rota}/checklist-categorias", new { nome, ordem, ativa = true });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static async Task<Guid> ChecklistItemAsync(this LojaTeste loja, Guid categoria, string nome = "Pastilha", int ordem = 0)
    {
        var r = await loja.Painel.Post($"{Rota}/checklist-itens", new { categoriaId = categoria, nome, ordem, ativo = true });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static Task<OrcamentoServico> OrcamentoNoBancoAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.OrcamentosServico.AsNoTracking().FirstAsync(o => o.Id == id));

    public static Task<LancamentoFinanceiro> LancamentoNoBancoAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().FirstAsync(l => l.Id == id));

    public static Task<List<ComissaoFuncionario>> ComissoesDaOrdemAsync(this ApiFixture api, Guid orcamento) =>
        api.NoBancoAsync(db => db.ComissoesFuncionario.AsNoTracking().Where(c => c.OrigemTipo == "ordem_servico" && c.OrigemId == orcamento).OrderBy(c => c.CriadoEm).ToListAsync());

    public static Task DefinirStatusAsync(this ApiFixture api, Guid orcamento, string status) =>
        api.NoBancoAsync(db => db.OrcamentosServico.Where(o => o.Id == orcamento).ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, status)));
}
