using LojaApi.Application.Carteira;
using LojaApi.Domain.Carteira;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.Tests.Suporte.Falsos;

namespace LojaApi.Tests.Application.Services.CarteiraTeste;

// Service da carteira do cliente, testado com repositório em memória (sem banco, sem HTTP).
public class CarteiraServiceTests
{
    private readonly RepositorioCarteiraFalso _repo = new();
    private readonly Guid _loja = Guid.NewGuid();
    private CarteiraService Servico() => new(_repo);

    private Cliente NovoCliente(decimal? limite = null, Guid? loja = null)
    {
        var c = new Cliente { LojaId = loja ?? _loja, Nome = "Maria", Telefone = "67999990000", LimiteCarteira = limite };
        _repo.Clientes.Add(c);
        return c;
    }

    private MovimentoCarteira Compra(Cliente c, decimal valor, Guid? venda = null)
    {
        var m = MovimentoCarteira.DeCompra(_loja, c.Id, venda ?? Guid.NewGuid(), valor, DateTime.UtcNow);
        _repo.Movimentos.Add(m);
        return m;
    }

    private static ReceberCarteiraRequest Recebimento(decimal valor = 50m, string forma = "pix", Guid? conta = null) =>
        new(valor, forma, null, null, conta);

    // ── Compra vinda da venda (quem salva é o repository de Vendas) ───────────

    [Fact]
    public async Task Compra_da_venda_sem_cliente_e_recusada_e_nao_deixa_rastro()
    {
        var r = await Servico().LancarCompraDaVendaAsync(_loja, null, Guid.NewGuid(), 100m, DateTime.UtcNow);

        Assert.Equal(TipoResultado.Invalido, r.Tipo);
        Assert.Equal("Selecione um cliente para vender na carteira.", r.Mensagem);
        Assert.Empty(_repo.Movimentos);
    }

    [Fact]
    public async Task Compra_da_venda_de_cliente_inexistente_ou_de_outra_loja_e_recusada()
    {
        var deOutraLoja = NovoCliente(loja: Guid.NewGuid());

        var inexistente = await Servico().LancarCompraDaVendaAsync(_loja, Guid.NewGuid(), Guid.NewGuid(), 100m, DateTime.UtcNow);
        var alheio = await Servico().LancarCompraDaVendaAsync(_loja, deOutraLoja.Id, Guid.NewGuid(), 100m, DateTime.UtcNow);

        Assert.Equal(("Cliente não encontrado.", "Cliente não encontrado."), (inexistente.Mensagem, alheio.Mensagem));
        Assert.Empty(_repo.Movimentos);
    }

    [Fact]
    public async Task Compra_da_venda_acima_do_limite_e_recusada()
    {
        var c = NovoCliente(limite: 150m);
        Compra(c, 100m);

        var r = await Servico().LancarCompraDaVendaAsync(_loja, c.Id, Guid.NewGuid(), 60m, DateTime.UtcNow);

        Assert.Equal(TipoResultado.Invalido, r.Tipo);
        Assert.Single(_repo.Movimentos);
    }

    [Fact]
    public async Task Compra_da_venda_dentro_do_limite_fica_pendente_no_repositorio_sem_salvar()
    {
        var c = NovoCliente(limite: 150m);
        var venda = Guid.NewGuid();

        var r = await Servico().LancarCompraDaVendaAsync(_loja, c.Id, venda, 150m, DateTime.UtcNow);

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        var compra = Assert.Single(_repo.Movimentos);
        Assert.Equal((c.Id, venda, 150m, true), (compra.ClienteId, compra.VendaId, compra.Valor, compra.EhCompra()));
        Assert.Same(compra, r.Valor);
        Assert.Equal(0, _repo.Salvamentos);   // a venda salva tudo junto, na mesma transação
    }

    // ── Excluir a venda ──────────────────────────────────────────────────────

    [Fact]
    public async Task Excluir_venda_sem_compra_na_carteira_nao_faz_nada()
    {
        Assert.Null(await Servico().RemoverCompraDaVendaAsync(_loja, Guid.NewGuid()));
    }

    [Fact]
    public async Task Excluir_venda_remove_a_compra_do_repositorio_sem_salvar()
    {
        var c = NovoCliente();
        var venda = Guid.NewGuid();
        var compra = Compra(c, 100m, venda);

        var erro = await Servico().RemoverCompraDaVendaAsync(_loja, venda);

        Assert.Null(erro);
        Assert.DoesNotContain(compra, _repo.Movimentos);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Excluir_venda_ja_paga_em_parte_e_recusado_e_a_compra_continua()
    {
        var c = NovoCliente();
        var venda = Guid.NewGuid();
        var compra = Compra(c, 100m, venda);
        _repo.Movimentos.Add(MovimentoCarteira.DePagamento(_loja, c.Id, 30m, "pix", null, DateTime.UtcNow, Guid.NewGuid()));

        var erro = await Servico().RemoverCompraDaVendaAsync(_loja, venda);

        Assert.StartsWith("Não é possível excluir: esta venda foi lançada na carteira", erro);
        Assert.Contains(compra, _repo.Movimentos);
    }

    // ── Receber e desfazer ───────────────────────────────────────────────────

    [Fact]
    public async Task Receber_sem_loja_ou_de_cliente_inexistente_nao_grava_nada()
    {
        var c = NovoCliente();
        Compra(c, 100m);

        var semLoja = await Servico().ReceberAsync(null, c.Id, Recebimento());
        var inexistente = await Servico().ReceberAsync(_loja, Guid.NewGuid(), Recebimento());

        Assert.Equal((TipoResultado.Invalido, "Loja não encontrada."), (semLoja.Tipo, semLoja.Mensagem));
        Assert.Equal((TipoResultado.NaoEncontrado, "Cliente não encontrado."), (inexistente.Tipo, inexistente.Mensagem));
        Assert.Equal(0, _repo.Salvamentos);
        Assert.Empty(_repo.Entradas);
    }

    [Fact]
    public async Task Receber_grava_o_pagamento_e_a_entrada_no_caixa_e_salva_uma_vez()
    {
        var c = NovoCliente();
        Compra(c, 100m);

        var r = await Servico().ReceberAsync(_loja, c.Id, Recebimento(40m, "dinheiro"));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        var entrada = Assert.Single(_repo.Entradas);
        Assert.Equal(("entrada", 40m), (entrada.Tipo, entrada.Valor));
        Assert.Null(entrada.ContaBancariaId);
        var pagamento = Assert.Single(_repo.Movimentos, m => !m.EhCompra());
        Assert.Equal((40m, "dinheiro", entrada.Id, DateTimeKind.Utc), (pagamento.Valor, pagamento.FormaPagamento, pagamento.MovimentoCaixaId!.Value, pagamento.Data.Kind));
        Assert.Equal(12, pagamento.Data.Hour);
        Assert.Equal(1, _repo.Salvamentos);
        Assert.Empty(_repo.Ajustes);
    }

    [Fact]
    public async Task Receber_numa_conta_da_loja_espelha_a_entrada_na_conta_e_numa_conta_alheia_ignora()
    {
        var c = NovoCliente();
        Compra(c, 200m);
        var conta = new ContaBancaria { LojaId = _loja, Nome = "Pix" };
        var alheia = new ContaBancaria { LojaId = Guid.NewGuid(), Nome = "Outra" };
        _repo.Contas.AddRange([conta, alheia]);

        await Servico().ReceberAsync(_loja, c.Id, Recebimento(60m, "pix", conta.Id));
        await Servico().ReceberAsync(_loja, c.Id, Recebimento(40m, "pix", alheia.Id));

        var ajuste = Assert.Single(_repo.Ajustes);
        Assert.Equal((conta.Id, "entrada", 60m), (ajuste.ContaBancariaId, ajuste.Tipo, ajuste.Valor));
        var comConta = Assert.Single(_repo.Entradas, e => e.ContaBancariaId.HasValue);
        Assert.Equal((conta.Id, ajuste.Id), (comConta.ContaBancariaId!.Value, comConta.AjusteContaBancariaId!.Value));
        Assert.Equal(2, _repo.Entradas.Count);
    }

    [Fact]
    public async Task Receber_mais_que_o_saldo_e_recusado_e_nao_salva()
    {
        var c = NovoCliente();
        Compra(c, 100m);

        var r = await Servico().ReceberAsync(_loja, c.Id, Recebimento(100.01m));

        Assert.Equal(TipoResultado.Invalido, r.Tipo);
        Assert.Equal(0, _repo.Salvamentos);
        Assert.Empty(_repo.Entradas);
    }

    [Fact]
    public async Task Desfazer_recebimento_remove_pagamento_entrada_e_ajuste_e_salva()
    {
        var c = NovoCliente();
        Compra(c, 100m);
        var conta = new ContaBancaria { LojaId = _loja, Nome = "Pix" };
        _repo.Contas.Add(conta);
        var recebido = await Servico().ReceberAsync(_loja, c.Id, Recebimento(100m, "pix", conta.Id));
        var pagamentoId = (Guid)recebido.Valor!.GetType().GetProperty("id")!.GetValue(recebido.Valor)!;
        var ajusteId = Assert.Single(_repo.Ajustes).Id;
        var salvamentosAntes = _repo.Salvamentos;

        var r = await Servico().DesfazerRecebimentoAsync(_loja, c.Id, pagamentoId);

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        Assert.Empty(_repo.Entradas);
        Assert.Equal([ajusteId], _repo.AjustesExcluidos);
        Assert.DoesNotContain(_repo.Movimentos, m => m.Id == pagamentoId);
        Assert.Equal(salvamentosAntes + 1, _repo.Salvamentos);
    }

    [Fact]
    public async Task Desfazer_recebimento_inexistente_ou_de_uma_compra_nao_acha()
    {
        var c = NovoCliente();
        var compra = Compra(c, 100m);

        var inexistente = await Servico().DesfazerRecebimentoAsync(_loja, c.Id, Guid.NewGuid());
        var daCompra = await Servico().DesfazerRecebimentoAsync(_loja, c.Id, compra.Id);
        var semLoja = await Servico().DesfazerRecebimentoAsync(null, c.Id, compra.Id);

        Assert.Equal((TipoResultado.NaoEncontrado, "Recebimento não encontrado."), (inexistente.Tipo, inexistente.Mensagem));
        Assert.Equal(TipoResultado.NaoEncontrado, daCompra.Tipo);
        Assert.Equal(TipoResultado.Invalido, semLoja.Tipo);
        Assert.Contains(compra, _repo.Movimentos);
    }

    // ── Limite ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Definir_limite_grava_e_salva_e_limite_negativo_nao_salva()
    {
        var c = NovoCliente();

        var ok = await Servico().DefinirLimiteAsync(_loja, c.Id, new DefinirLimiteCarteiraRequest(300m));
        var negativo = await Servico().DefinirLimiteAsync(_loja, c.Id, new DefinirLimiteCarteiraRequest(-1m));

        Assert.Equal(TipoResultado.Ok, ok.Tipo);
        Assert.Equal((300m, 1), (c.LimiteCarteira, _repo.Salvamentos));
        Assert.Equal(TipoResultado.Invalido, negativo.Tipo);
    }
}
