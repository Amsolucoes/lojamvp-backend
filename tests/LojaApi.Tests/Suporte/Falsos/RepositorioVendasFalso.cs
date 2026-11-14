using LojaApi.Application.Vendas;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Tests.Suporte.Falsos;

// Repositório de Vendas em memória: permite testar os HANDLERS (regras) sem banco.
// "Salvamentos" conta as gravações pedidas pelo handler (zero = nada foi gravado).
// As buscas (id, lojaId) filtram pela loja como o repositório de verdade: com loja, só o que é dela.
public sealed class RepositorioVendasFalso : IVendasRepository
{
    public List<Venda> Vendas { get; } = [];
    public List<ItemVenda> ItensVenda { get; } = [];
    public List<Produto> Produtos { get; } = [];
    public List<ProdutoVariacao> Variacoes { get; } = [];
    public List<Servico> Servicos { get; } = [];
    public List<Cliente> Clientes { get; } = [];
    public List<Profissional> Funcionarios { get; } = [];
    public List<MovimentoEstoque> MovimentosEstoque { get; } = [];
    public List<ComissaoFuncionario> Comissoes { get; } = [];
    public List<Troca> Trocas { get; } = [];
    public List<ItemTroca> ItensTroca { get; } = [];
    public List<MovimentoCaixa> MovimentosCaixa { get; } = [];
    public List<ContaBancaria> Contas { get; } = [];
    public List<AjusteContaBancaria> Ajustes { get; } = [];
    public List<OrigemVenda> Origens { get; } = [];
    public bool NotaAutorizada { get; set; }
    public int Salvamentos { get; private set; }

    // ── Vendas ──
    public Task<List<Venda>> ListarVendasAsync(Guid? lojaId, DateTime? de, DateTime? ate, Guid? clienteId) => Task.FromResult(
        Vendas.Where(v => (lojaId == null || v.LojaId == lojaId) && (clienteId == null || v.ClienteId == clienteId)).OrderByDescending(v => v.CriadaEm).ToList());
    public Task<Venda?> ObterVendaCompletaAsync(Guid id) => Task.FromResult(Vendas.FirstOrDefault(v => v.Id == id));
    public Task<Venda?> ObterVendaComItensAsync(Guid id) => Task.FromResult(Vendas.FirstOrDefault(v => v.Id == id));
    public Task<bool> TemNotaFiscalAutorizadaAsync(Guid vendaId) => Task.FromResult(NotaAutorizada);
    public Task<ComissaoFuncionario?> ObterComissaoDaVendaAsync(Guid vendaId) => Task.FromResult(Comissoes.FirstOrDefault(c => c.OrigemTipo == "venda" && c.OrigemId == vendaId));
    public Task<List<Agendamento>> ListarAgendamentosDaVendaAsync(Guid vendaId) => Task.FromResult(new List<Agendamento>());
    public Task RemoverConsumosDePlanoDaVendaAsync(Guid vendaId) => Task.CompletedTask;
    public void Adicionar(Venda venda) => Vendas.Add(venda);
    public void Adicionar(ItemVenda item) => ItensVenda.Add(item);
    public void Adicionar(ConsumoPlano consumo) { }
    public void Adicionar(MovimentoEstoque movimento) => MovimentosEstoque.Add(movimento);
    public void Adicionar(ComissaoFuncionario comissao) => Comissoes.Add(comissao);
    public void Remover(Venda venda) => Vendas.Remove(venda);
    public void Remover(ComissaoFuncionario comissao) => Comissoes.Remove(comissao);
    public void RemoverItens(IEnumerable<ItemVenda> itens) { }

    // ── Outros módulos ──
    public Task<Produto?> ObterProdutoAsync(Guid id, Guid? lojaId) => Task.FromResult(Produtos.FirstOrDefault(p => p.Id == id && (lojaId == null || p.LojaId == lojaId)));
    public Task<ProdutoVariacao?> ObterVariacaoAsync(Guid id, Guid? lojaId) => Task.FromResult(
        Variacoes.FirstOrDefault(v => v.Id == id && (lojaId == null || Produtos.Any(p => p.Id == v.ProdutoId && p.LojaId == lojaId))));
    public Task<Servico?> ObterServicoAsync(Guid id, Guid? lojaId) => Task.FromResult(Servicos.FirstOrDefault(s => s.Id == id && (lojaId == null || s.LojaId == lojaId)));
    public Task<Cliente?> ObterClienteAsync(Guid id, Guid? lojaId) => Task.FromResult(Clientes.FirstOrDefault(c => c.Id == id && (lojaId == null || c.LojaId == lojaId)));
    public Task<AssinaturaCliente?> ObterAssinaturaAsync(Guid id, Guid? lojaId) => Task.FromResult<AssinaturaCliente?>(null);
    public Task<Plano?> ObterPlanoAsync(Guid id) => Task.FromResult<Plano?>(null);
    public Task<int> ContarConsumosDeAssinaturaAsync(Guid assinaturaId, DateTime inicioUtc, DateTime fimUtc) => Task.FromResult(0);
    public Task<Agendamento?> ObterAgendamentoAsync(Guid id) => Task.FromResult<Agendamento?>(null);
    public Task<Profissional?> ObterFuncionarioDaLojaAsync(Guid id, Guid? lojaId) => Task.FromResult(Funcionarios.FirstOrDefault(p => p.Id == id && p.LojaId == lojaId));

    // ── Trocas ──
    public Task<List<TrocaListaDto>> ListarTrocasAsync(Guid? lojaId) => Task.FromResult(new List<TrocaListaDto>());
    public void Adicionar(Troca troca) => Trocas.Add(troca);
    public void Adicionar(ItemTroca item) => ItensTroca.Add(item);

    // ── Caixa ──
    public Task<List<MovimentoCaixaDto>> ListarMovimentosCaixaAsync(Guid lojaId, DateTime? deUtc, DateTime? ateUtc) => Task.FromResult(
        MovimentosCaixa.Where(m => m.LojaId == lojaId && (deUtc == null || m.Data >= deUtc) && (ateUtc == null || m.Data <= ateUtc))
            .Select(m => new MovimentoCaixaDto(m.Id, m.Tipo, m.Valor, m.Data, m.OrigemVendaId, m.OrigemNome, m.Observacao, m.ContaBancariaId, m.CriadoEm)).ToList());
    public Task<MovimentoCaixa?> ObterMovimentoCaixaAsync(Guid id, Guid? lojaId) => Task.FromResult(MovimentosCaixa.FirstOrDefault(m => m.Id == id && m.LojaId == lojaId));
    public Task<ContaBancaria?> ObterContaBancariaAsync(Guid id, Guid? lojaId) => Task.FromResult(Contas.FirstOrDefault(c => c.Id == id && c.LojaId == lojaId));
    public Task<AjusteContaBancaria?> ObterAjusteDaContaAsync(Guid id) => Task.FromResult(Ajustes.FirstOrDefault(a => a.Id == id));
    public void Adicionar(MovimentoCaixa movimento) => MovimentosCaixa.Add(movimento);
    public void Remover(MovimentoCaixa movimento) => MovimentosCaixa.Remove(movimento);
    public void Adicionar(AjusteContaBancaria ajuste) => Ajustes.Add(ajuste);
    public void Remover(AjusteContaBancaria ajuste) => Ajustes.Remove(ajuste);

    // ── Origens ──
    public Task<List<OrigemVendaResumoDto>> ListarOrigensAtivasAsync(Guid lojaId) => Task.FromResult(
        Origens.Where(o => o.LojaId == lojaId && o.Ativa).OrderBy(o => o.Ordem).ThenBy(o => o.Nome).Select(o => new OrigemVendaResumoDto(o.Id, o.Nome, o.Ordem)).ToList());
    public Task<List<OrigemVenda>> ListarOrigensAsync(Guid lojaId) => Task.FromResult(Origens.Where(o => o.LojaId == lojaId).OrderBy(o => o.Ordem).ThenBy(o => o.Nome).ToList());
    public Task<OrigemVenda?> ObterOrigemAsync(Guid id, Guid? lojaId) => Task.FromResult(Origens.FirstOrDefault(o => o.Id == id && o.LojaId == lojaId));
    public Task<bool> OrigemComNomeExisteAsync(Guid? lojaId, string nome) => Task.FromResult(Origens.Any(o => o.LojaId == lojaId && o.Nome == nome));
    public Task<bool> OrigemEmUsoAsync(Guid id) => Task.FromResult(Vendas.Any(v => v.OrigemVendaId == id));
    public Task<bool> LojaTemOrigensAsync(Guid? lojaId) => Task.FromResult(Origens.Any(o => o.LojaId == lojaId));
    public void Adicionar(OrigemVenda origem) => Origens.Add(origem);
    public void Remover(OrigemVenda origem) => Origens.Remove(origem);

    // ── Relatórios ──
    public Task<List<Venda>> ListarVendasComItensDoPeriodoAsync(Guid? lojaId, DateTime? de, DateTime? ate) => Task.FromResult(
        Vendas.Where(v => lojaId == null || v.LojaId == lojaId).ToList());
    public Task<List<ItemVenda>> ListarItensVendidosAsync(Guid? lojaId, DateTime? de, DateTime? ate) => Task.FromResult(
        Vendas.Where(v => lojaId == null || v.LojaId == lojaId).SelectMany(v => v.Itens).ToList());
    public Task<List<Venda>> ListarVendasEntreAsync(Guid? lojaId, DateTime deInclusivo, DateTime ateExclusivo) => Task.FromResult(
        Vendas.Where(v => (lojaId == null || v.LojaId == lojaId) && v.CriadaEm >= deInclusivo && v.CriadaEm < ateExclusivo).ToList());

    public Task SalvarAsync() { Salvamentos++; return Task.CompletedTask; }
}
