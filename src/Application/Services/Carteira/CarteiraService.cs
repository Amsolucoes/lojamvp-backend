using LojaApi.Models;
using LojaApi.Domain.Carteira;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Carteira;

// Carteira do cliente ("fiado"): compra agora, paga depois. O saldo é sempre calculado a partir dos lançamentos.
// As regras estão aqui e em RegrasCarteira (Domain); o acesso ao banco fica no ICarteiraRepository.
public class CarteiraService(ICarteiraRepository repositorio)
{
    private const string LojaNaoEncontrada = "Loja não encontrada.";
    private const string ClienteNaoEncontrado = "Cliente não encontrado.";

    private Task<Cliente?> BuscarClienteAsync(Guid lojaId, Guid clienteId) => repositorio.ObterClienteAsync(lojaId, clienteId);

    public Task<decimal> SaldoAsync(Guid lojaId, Guid clienteId) => repositorio.SaldoAsync(lojaId, clienteId);

    // Saldo pelo id do cliente (o id já é único em todas as lojas): usado na listagem/cadastro de clientes
    public Task<decimal> SaldoPorClienteAsync(Guid clienteId) => repositorio.SaldoPorClienteAsync(clienteId);

    // ── Usado pela venda (o VendasController salva tudo junto, na mesma transação) ──

    // Confere cliente e limite e já deixa a compra pronta para ser salva junto com a venda.
    public async Task<Resultado<MovimentoCarteira>> LancarCompraDaVendaAsync(
        Guid lojaId, Guid? clienteId, Guid vendaId, decimal valor, DateTime data)
    {
        if (clienteId is null) return Resultado<MovimentoCarteira>.Invalido("Selecione um cliente para vender na carteira.");

        var cliente = await BuscarClienteAsync(lojaId, clienteId.Value);
        if (cliente is null) return Resultado<MovimentoCarteira>.Invalido(ClienteNaoEncontrado);

        var saldo = await SaldoAsync(lojaId, cliente.Id);
        var erro = RegrasCarteira.ValidarCompra(saldo, valor, cliente.LimiteCarteira);
        if (erro != null) return Resultado<MovimentoCarteira>.Invalido(erro);

        var compra = MovimentoCarteira.DeCompra(lojaId, cliente.Id, vendaId, valor, data);
        repositorio.Adicionar(compra);
        return Resultado<MovimentoCarteira>.Ok(compra);
    }

    // Excluir a venda tira a compra da carteira — a não ser que o cliente já tenha pago e o saldo ficaria negativo.
    public async Task<string?> RemoverCompraDaVendaAsync(Guid lojaId, Guid vendaId)
    {
        var compra = await repositorio.ObterCompraDaVendaAsync(lojaId, vendaId);
        if (compra is null) return null;

        var saldo = await SaldoAsync(lojaId, compra.ClienteId);
        if (saldo - compra.Valor < 0)
            return "Não é possível excluir: esta venda foi lançada na carteira e o cliente já pagou parte do saldo. Desfaça o recebimento na carteira do cliente antes.";

        repositorio.Remover(compra);
        return null;
    }

    public async Task<bool> TemSaldoAsync(Guid lojaId, Guid clienteId) => await SaldoAsync(lojaId, clienteId) > 0;

    // ── Consultas ────────────────────────────────────────────────────────────

    public async Task<Resultado<object>> ObterAsync(Guid? lojaId, Guid clienteId)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);
        var cliente = await BuscarClienteAsync(lojaId.Value, clienteId);
        if (cliente is null) return Resultado.NaoEncontrado(ClienteNaoEncontrado);

        var movimentos = await repositorio.ListarMovimentosDoClienteAsync(lojaId.Value, clienteId);
        // Por dia e, dentro do dia, na ordem em que foram lançados (o pagamento fica gravado ao meio-dia,
        // mas a compra tem a hora exata; ordenar pela hora colocaria o pagamento antes de uma compra feita à tarde).
        movimentos = movimentos.OrderBy(m => m.Data.Date).ThenBy(m => m.CriadoEm).ToList();

        // extrato com o saldo depois de cada lançamento (do mais antigo para o mais novo)
        decimal acumulado = 0;
        var extrato = new List<object>();
        foreach (var m in movimentos)
        {
            acumulado += m.EhCompra() ? m.Valor : -m.Valor;
            extrato.Add(new { m.Id, m.Tipo, m.Valor, m.Data, m.VendaId, m.FormaPagamento, m.Observacao, saldoApos = acumulado });
        }
        extrato.Reverse();

        var totalPago = movimentos.Where(m => !m.EhCompra()).Sum(m => m.Valor);
        var compras = movimentos.Where(m => m.EhCompra()).Select(m => (m.VendaId ?? Guid.Empty, m.Data, m.Valor));
        var emAberto = RegrasCarteira.ComprasEmAberto(compras, totalPago)
            .Select(c => new { c.VendaId, c.Data, c.Valor, c.EmAberto })
            .ToList();

        var saldo = acumulado;
        return Resultado.Ok(new
        {
            clienteId = cliente.Id,
            cliente.Nome,
            cliente.Telefone,
            saldo,
            limite = cliente.LimiteCarteira,
            disponivel = cliente.LimiteCarteira.HasValue ? Math.Max(0, cliente.LimiteCarteira.Value - saldo) : (decimal?)null,
            emAberto,
            movimentos = extrato,
        });
    }

    // Clientes que estão devendo, do maior saldo para o menor (para cobrar no fim do mês).
    public async Task<Resultado<object>> DevedoresAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var itens = (await repositorio.ListarDevedoresAsync(lojaId.Value))
            .Select(d => new { clienteId = d.ClienteId, d.Nome, d.Telefone, saldo = d.Saldo, limite = d.Limite })
            .OrderByDescending(x => x.saldo)
            .ToList();

        return Resultado.Ok(new { total = itens.Sum(i => i.saldo), clientes = itens });
    }

    // Para o fluxo de caixa: cada venda na carteira com o que falta receber (e por onde já foi paga) e cada recebimento
    // com as vendas que ele quitou. Assim a tela mostra "Carteira → Pix" em vez de um "ajuste de caixa" solto.
    public async Task<Resultado<object>> MovimentacaoAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var movimentos = await repositorio.ListarMovimentosDaLojaAsync(lojaId.Value);
        var nomes = await repositorio.NomesDosClientesComMovimentoAsync(lojaId.Value);

        var vendas = new List<object>();
        var recebimentos = new List<object>();

        // cada cliente tem a sua própria fila: o pagamento de um não quita a compra de outro
        foreach (var grupo in movimentos.GroupBy(m => m.ClienteId))
        {
            var compras = grupo.Where(m => m.EhCompra() && m.VendaId.HasValue)
                .Select(m => new RegrasCarteira.CompraParaAlocar(m.VendaId!.Value, m.Data, m.CriadoEm, m.Valor));
            var pagamentos = grupo.Where(m => !m.EhCompra())
                .Select(m => new RegrasCarteira.PagamentoParaAlocar(m.Id, m.Data, m.CriadoEm, m.Valor, m.FormaPagamento ?? ""));
            var (comprasAlocadas, pagamentosAlocados) = RegrasCarteira.AlocarPagamentos(compras, pagamentos);
            var nome = nomes.GetValueOrDefault(grupo.Key, "");

            foreach (var c in comprasAlocadas)
                vendas.Add(new
                {
                    vendaId = c.VendaId, clienteId = grupo.Key, valor = c.Valor, emAberto = c.EmAberto,
                    pagamentos = c.Pagamentos.Select(p => new { pagamentoId = p.PagamentoId, data = p.Data, forma = p.Forma, valor = p.Valor }),
                });

            foreach (var pg in grupo.Where(m => !m.EhCompra()))
            {
                var alocado = pagamentosAlocados.First(a => a.PagamentoId == pg.Id);
                recebimentos.Add(new
                {
                    id = pg.Id, movimentoCaixaId = pg.MovimentoCaixaId, clienteId = grupo.Key, clienteNome = nome,
                    forma = pg.FormaPagamento, valor = pg.Valor, data = pg.Data,
                    abateu = alocado.Abateu.Select(a => new { vendaId = a.VendaId, valor = a.Valor }),
                });
            }
        }
        return Resultado.Ok(new { vendas, recebimentos });
    }

    // ── Ações ────────────────────────────────────────────────────────────────

    public async Task<Resultado<object>> DefinirLimiteAsync(Guid? lojaId, Guid clienteId, DefinirLimiteCarteiraRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);
        var cliente = await BuscarClienteAsync(lojaId.Value, clienteId);
        if (cliente is null) return Resultado.NaoEncontrado(ClienteNaoEncontrado);

        var erro = RegrasCarteira.ValidarLimite(req.Limite);
        if (erro != null) return Resultado.Invalido(erro);

        cliente.LimiteCarteira = req.Limite;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { limite = cliente.LimiteCarteira });
    }

    // O cliente pagou (total ou parcial): baixa a dívida e lança a entrada no caixa na data do recebimento.
    public async Task<Resultado<object>> ReceberAsync(Guid? lojaId, Guid clienteId, ReceberCarteiraRequest req)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);
        var cliente = await BuscarClienteAsync(lojaId.Value, clienteId);
        if (cliente is null) return Resultado.NaoEncontrado(ClienteNaoEncontrado);

        var saldo = await SaldoAsync(lojaId.Value, clienteId);
        var dia = (req.Data ?? DateTime.UtcNow).Date;
        var erro = RegrasCarteira.ValidarRecebimento(saldo, req.Valor, req.FormaPagamento, dia, DateTime.UtcNow);
        if (erro != null) return Resultado.Invalido(erro);

        var dataUtc = DateTime.SpecifyKind(dia, DateTimeKind.Utc).AddHours(12);
        var rotuloForma = req.FormaPagamento switch { "pix" => "Pix", "dinheiro" => "Dinheiro", "credito" => "Crédito", _ => "Débito" };
        var observacaoCaixa = $"Recebimento da carteira — {cliente.Nome} ({rotuloForma})" +
                              (string.IsNullOrWhiteSpace(req.Observacao) ? "" : $" — {req.Observacao.Trim()}");

        var entradaCaixa = new MovimentoCaixa
        {
            LojaId = lojaId.Value, Tipo = "entrada", Valor = req.Valor, Data = dataUtc,
            Observacao = observacaoCaixa.Length > 300 ? observacaoCaixa[..300] : observacaoCaixa,
        };

        // Com o Financeiro ativo e uma conta escolhida, espelha a entrada no saldo da conta (igual ao reforço de caixa)
        if (req.ContaBancariaId.HasValue)
        {
            var conta = await repositorio.ObterContaAsync(lojaId.Value, req.ContaBancariaId.Value);
            if (conta != null)
            {
                var ajuste = new AjusteContaBancaria
                {
                    LojaId = lojaId.Value, ContaBancariaId = conta.Id, Tipo = "entrada", Valor = req.Valor,
                    Observacao = $"Recebimento da carteira — {cliente.Nome}", CriadoEm = dataUtc,
                };
                repositorio.Adicionar(ajuste);
                entradaCaixa.ContaBancariaId = conta.Id;
                entradaCaixa.AjusteContaBancariaId = ajuste.Id;
            }
        }
        repositorio.Adicionar(entradaCaixa);

        var pagamento = MovimentoCarteira.DePagamento(lojaId.Value, clienteId, req.Valor, req.FormaPagamento, req.Observacao, dataUtc, entradaCaixa.Id);
        repositorio.Adicionar(pagamento);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { id = pagamento.Id, saldo = saldo - req.Valor, quitado = saldo - req.Valor <= 0 });
    }

    // Recebimento lançado por engano: desfaz o pagamento e a entrada que ele criou no caixa.
    public async Task<Resultado<object>> DesfazerRecebimentoAsync(Guid? lojaId, Guid clienteId, Guid movimentoId)
    {
        if (lojaId is null) return Resultado.Invalido(LojaNaoEncontrada);

        var pagamento = await repositorio.ObterPagamentoAsync(lojaId.Value, clienteId, movimentoId);
        if (pagamento is null) return Resultado.NaoEncontrado("Recebimento não encontrado.");

        if (pagamento.MovimentoCaixaId.HasValue)
        {
            var entrada = await repositorio.ObterEntradaDeCaixaAsync(lojaId.Value, pagamento.MovimentoCaixaId.Value);
            if (entrada != null)
            {
                if (entrada.AjusteContaBancariaId.HasValue)
                    await repositorio.ExcluirAjusteDeContaAsync(lojaId.Value, entrada.AjusteContaBancariaId.Value);
                repositorio.Remover(entrada);
            }
        }
        repositorio.Remover(pagamento);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { saldo = await SaldoAsync(lojaId.Value, clienteId) });
    }
}
