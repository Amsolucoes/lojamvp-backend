using LojaApi.Application.Vendas;
using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Vendas;

// Acesso aos dados de Vendas (EF Core). Só lê e grava; as regras ficam nos handlers.
// Nas buscas "(id, lojaId)": com loja, só devolve o que é dela; sem loja (usuário sem vínculo), não filtra — igual ao comportamento antigo.
public class VendasRepository(AppDbContext db) : IVendasRepository
{
    // ── Vendas ──

    public async Task<List<Venda>> ListarVendasAsync(Guid? lojaId, DateTime? de, DateTime? ate, Guid? clienteId)
    {
        var q = db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .AsQueryable();

        if (lojaId.HasValue) q = q.Where(v => v.LojaId == lojaId);
        if (de.HasValue) q = q.Where(v => v.CriadaEm >= de.Value);
        if (ate.HasValue) q = q.Where(v => v.CriadaEm <= ate.Value.AddDays(1));
        if (clienteId.HasValue) q = q.Where(v => v.ClienteId == clienteId);

        return await q.OrderByDescending(v => v.CriadaEm).ToListAsync();
    }

    public Task<Venda?> ObterVendaCompletaAsync(Guid id) =>
        db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == id);

    public Task<Venda?> ObterVendaComItensAsync(Guid id) =>
        db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.Id == id);

    public Task<bool> TemNotaFiscalAutorizadaAsync(Guid vendaId) =>
        db.EmissoesFiscais.AnyAsync(e => e.VendaId == vendaId && e.Status == "autorizada");

    public Task<ComissaoFuncionario?> ObterComissaoDaVendaAsync(Guid vendaId) =>
        db.ComissoesFuncionario.FirstOrDefaultAsync(c => c.OrigemTipo == "venda" && c.OrigemId == vendaId);

    public Task<List<Agendamento>> ListarAgendamentosDaVendaAsync(Guid vendaId) =>
        db.Agendamentos.Where(a => a.VendaId == vendaId).ToListAsync();

    public Task RemoverConsumosDePlanoDaVendaAsync(Guid vendaId) =>
        db.ConsumosPlano.Where(c => c.VendaId == vendaId).ExecuteDeleteAsync();

    public void Adicionar(Venda venda) => db.Vendas.Add(venda);
    public void Adicionar(ItemVenda item) => db.ItensVenda.Add(item);
    public void Adicionar(ConsumoPlano consumo) => db.ConsumosPlano.Add(consumo);
    public void Adicionar(MovimentoEstoque movimento) => db.Movimentos.Add(movimento);
    public void Adicionar(ComissaoFuncionario comissao) => db.ComissoesFuncionario.Add(comissao);
    public void Remover(Venda venda) => db.Vendas.Remove(venda);
    public void Remover(ComissaoFuncionario comissao) => db.ComissoesFuncionario.Remove(comissao);
    public void RemoverItens(IEnumerable<ItemVenda> itens) => db.ItensVenda.RemoveRange(itens);

    // ── Dados de outros módulos ──

    public Task<Produto?> ObterProdutoAsync(Guid id, Guid? lojaId) =>
        db.Produtos.FirstOrDefaultAsync(p => p.Id == id && (lojaId == null || p.LojaId == lojaId));

    public Task<ProdutoVariacao?> ObterVariacaoAsync(Guid id, Guid? lojaId) =>
        db.ProdutoVariacoes.FirstOrDefaultAsync(v => v.Id == id && (lojaId == null || v.Produto.LojaId == lojaId));

    public Task<Servico?> ObterServicoAsync(Guid id, Guid? lojaId) =>
        db.Servicos.FirstOrDefaultAsync(s => s.Id == id && (lojaId == null || s.LojaId == lojaId));

    public Task<Cliente?> ObterClienteAsync(Guid id, Guid? lojaId) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id && (lojaId == null || c.LojaId == lojaId));

    // A assinatura sempre foi buscada pela loja (mesmo nula: nesse caso não acha nada)
    public Task<AssinaturaCliente?> ObterAssinaturaAsync(Guid id, Guid? lojaId) =>
        db.AssinaturasCliente.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);

    public Task<Plano?> ObterPlanoAsync(Guid id) => db.Planos.FirstOrDefaultAsync(p => p.Id == id);

    public Task<int> ContarConsumosDeAssinaturaAsync(Guid assinaturaId, DateTime inicioUtc, DateTime fimUtc) =>
        db.ConsumosPlano.CountAsync(c => c.AssinaturaId == assinaturaId && c.CriadoEm >= inicioUtc && c.CriadoEm < fimUtc);

    public Task<Agendamento?> ObterAgendamentoAsync(Guid id) => db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id);

    // O funcionário sempre foi buscado pela loja (mesmo nula)
    public Task<Profissional?> ObterFuncionarioDaLojaAsync(Guid id, Guid? lojaId) =>
        db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);

    // ── Trocas ──

    public Task<List<TrocaListaDto>> ListarTrocasAsync(Guid? lojaId)
    {
        var q = db.Trocas.AsQueryable();
        if (lojaId.HasValue) q = q.Where(t => t.LojaId == lojaId);

        return q.OrderByDescending(t => t.CriadaEm).Take(50)
            .Select(t => new TrocaListaDto(
                t.Id, t.ClienteId, t.Cliente.Nome, t.TotalDevolvido, t.TotalNovo, t.Diferenca,
                t.CreditoGerado, t.FormaPagamento, t.CriadaEm,
                t.Itens.Select(i => new TrocaItemDto(i.NomeProduto, i.Quantidade, i.PrecoUnitario, i.Tipo, i.VoltaEstoque)).ToList()))
            .ToListAsync();
    }

    public void Adicionar(Troca troca) => db.Trocas.Add(troca);
    public void Adicionar(ItemTroca item) => db.ItensTroca.Add(item);

    // ── Caixa ──

    public Task<List<MovimentoCaixaDto>> ListarMovimentosCaixaAsync(Guid lojaId, DateTime? deUtc, DateTime? ateUtc)
    {
        var q = db.MovimentosCaixa.Where(m => m.LojaId == lojaId);
        if (deUtc.HasValue) q = q.Where(m => m.Data >= deUtc.Value);
        if (ateUtc.HasValue) q = q.Where(m => m.Data <= ateUtc.Value);

        return q.OrderByDescending(m => m.Data).ThenByDescending(m => m.CriadoEm)
            .Select(m => new MovimentoCaixaDto(
                m.Id, m.Tipo, m.Valor, m.Data, m.OrigemVendaId, m.OrigemNome, m.Observacao, m.ContaBancariaId, m.CriadoEm))
            .ToListAsync();
    }

    public Task<MovimentoCaixa?> ObterMovimentoCaixaAsync(Guid id, Guid? lojaId) =>
        db.MovimentosCaixa.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);

    public Task<ContaBancaria?> ObterContaBancariaAsync(Guid id, Guid? lojaId) =>
        db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public Task<AjusteContaBancaria?> ObterAjusteDaContaAsync(Guid id) =>
        db.AjustesContaBancaria.FirstOrDefaultAsync(a => a.Id == id);

    public void Adicionar(MovimentoCaixa movimento) => db.MovimentosCaixa.Add(movimento);
    public void Remover(MovimentoCaixa movimento) => db.MovimentosCaixa.Remove(movimento);
    public void Adicionar(AjusteContaBancaria ajuste) => db.AjustesContaBancaria.Add(ajuste);
    public void Remover(AjusteContaBancaria ajuste) => db.AjustesContaBancaria.Remove(ajuste);

    // ── Origens de venda ──

    public Task<List<OrigemVendaResumoDto>> ListarOrigensAtivasAsync(Guid lojaId) =>
        db.OrigensVenda
            .Where(o => o.LojaId == lojaId && o.Ativa)
            .OrderBy(o => o.Ordem).ThenBy(o => o.Nome)
            .Select(o => new OrigemVendaResumoDto(o.Id, o.Nome, o.Ordem))
            .ToListAsync();

    public Task<List<OrigemVenda>> ListarOrigensAsync(Guid lojaId) =>
        db.OrigensVenda
            .Where(o => o.LojaId == lojaId)
            .OrderBy(o => o.Ordem).ThenBy(o => o.Nome)
            .ToListAsync();

    public Task<OrigemVenda?> ObterOrigemAsync(Guid id, Guid? lojaId) =>
        db.OrigensVenda.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);

    public Task<bool> OrigemComNomeExisteAsync(Guid? lojaId, string nome) =>
        db.OrigensVenda.AnyAsync(o => o.LojaId == lojaId && o.Nome == nome);

    public Task<bool> OrigemEmUsoAsync(Guid id) => db.Vendas.AnyAsync(v => v.OrigemVendaId == id);

    public Task<bool> LojaTemOrigensAsync(Guid? lojaId) => db.OrigensVenda.AnyAsync(o => o.LojaId == lojaId);

    public void Adicionar(OrigemVenda origem) => db.OrigensVenda.Add(origem);
    public void Remover(OrigemVenda origem) => db.OrigensVenda.Remove(origem);

    // ── Relatórios ──

    public async Task<List<Venda>> ListarVendasComItensDoPeriodoAsync(Guid? lojaId, DateTime? de, DateTime? ate)
    {
        var q = db.Vendas.AsQueryable();
        if (lojaId.HasValue) q = q.Where(v => v.LojaId == lojaId);
        if (de.HasValue) q = q.Where(v => v.CriadaEm >= de.Value);
        if (ate.HasValue) q = q.Where(v => v.CriadaEm <= ate.Value.AddDays(1));

        return await q.Include(v => v.Itens).ToListAsync();
    }

    public async Task<List<ItemVenda>> ListarItensVendidosAsync(Guid? lojaId, DateTime? de, DateTime? ate)
    {
        var q = db.Vendas.AsQueryable();
        if (lojaId.HasValue) q = q.Where(v => v.LojaId == lojaId);
        if (de.HasValue) q = q.Where(v => v.CriadaEm >= de.Value);
        if (ate.HasValue) q = q.Where(v => v.CriadaEm <= ate.Value.AddDays(1));

        return await q
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .SelectMany(v => v.Itens)
            .ToListAsync();
    }

    public async Task<List<Venda>> ListarVendasEntreAsync(Guid? lojaId, DateTime deInclusivo, DateTime ateExclusivo)
    {
        var q = db.Vendas.Where(v => v.CriadaEm >= deInclusivo && v.CriadaEm < ateExclusivo);
        if (lojaId.HasValue) q = q.Where(v => v.LojaId == lojaId);
        return await q.ToListAsync();
    }

    public Task SalvarAsync() => db.SaveChangesAsync();
}
