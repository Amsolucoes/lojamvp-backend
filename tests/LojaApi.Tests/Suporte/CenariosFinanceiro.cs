using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Suporte;

// Ajudantes dos testes do Financeiro (contas, lançamentos e cartões criados direto no banco para montar saldos).
public static class CenariosFinanceiro
{
    public const string Rota = "/api/financeiro";

    public static async Task<Guid> ContaPelaApiAsync(this LojaTeste loja, string nome = "Conta Teste", decimal saldoInicial = 0m, string? banco = null, decimal limite = 0m)
    {
        var r = await loja.Painel.Post($"{Rota}/contas", new { nome, saldoInicial, banco, limite });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static Task<ContaBancaria> ContaNoBancoAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.ContasBancarias.AsNoTracking().FirstAsync(c => c.Id == id));

    public static Task<List<AjusteContaBancaria>> AjustesDaContaNoBancoAsync(this ApiFixture api, Guid contaId) =>
        api.NoBancoAsync(db => db.AjustesContaBancaria.AsNoTracking().Where(a => a.ContaBancariaId == contaId).OrderBy(a => a.CriadoEm).ToListAsync());

    public static Task<List<CategoriaFinanceira>> CategoriasFinanceirasNoBancoAsync(this ApiFixture api, Guid lojaId) =>
        api.NoBancoAsync(db => db.CategoriasFinanceiras.AsNoTracking().Where(c => c.LojaId == lojaId).OrderBy(c => c.Nome).ToListAsync());

    public static Task LancamentoNoBancoAsync(this ApiFixture api, Guid lojaId, Guid contaId, string tipo, decimal valor, string status = "pago", bool naoAfetaSaldo = false, Guid? categoriaId = null) =>
        api.NoBancoAsync(async db =>
        {
            db.LancamentosFinanceiros.Add(new LancamentoFinanceiro
            {
                LojaId = lojaId, ContaBancariaId = contaId, Tipo = tipo, Descricao = $"Lançamento {tipo}", Valor = valor, Vencimento = DateTime.UtcNow,
                Status = status, PagoEm = status == "pago" ? DateTime.UtcNow : null, NaoAfetaSaldo = naoAfetaSaldo, CategoriaId = categoriaId,
            });
            await db.SaveChangesAsync();
        });

    public static Task AjusteNoBancoAsync(this ApiFixture api, Guid lojaId, Guid contaId, string tipo, decimal valor) =>
        api.NoBancoAsync(async db =>
        {
            db.AjustesContaBancaria.Add(new AjusteContaBancaria { LojaId = lojaId, ContaBancariaId = contaId, Tipo = tipo, Valor = valor });
            await db.SaveChangesAsync();
        });

    // Cartão ligado à conta, com uma fatura já paga (valorPago) — a fatura debita da conta dela (ou, sem conta, da conta do cartão)
    public static Task FaturaPagaNoBancoAsync(this ApiFixture api, Guid lojaId, Guid contaDoCartao, decimal valorPago, Guid? contaDaFatura = null, bool naoAfetaSaldo = false, decimal antecipado = 0m, Guid? contaDoAntecipado = null, string status = "pago") =>
        api.NoBancoAsync(async db =>
        {
            var cartao = new CartaoCredito { LojaId = lojaId, Nome = $"Cartão {Guid.NewGuid():N}"[..14], ContaBancariaId = contaDoCartao };
            db.CartoesCredito.Add(cartao);
            var fatura = new FaturaCartao
            {
                LojaId = lojaId, CartaoCreditoId = cartao.Id, MesReferencia = new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc), Vencimento = new DateTime(2031, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                ContaBancariaId = contaDaFatura, Total = valorPago, ValorPago = valorPago, Status = status, PagoEm = DateTime.UtcNow, NaoAfetaSaldo = naoAfetaSaldo,
            };
            db.FaturasCartao.Add(fatura);
            if (antecipado > 0)
                db.PagamentosAntecipadosFatura.Add(new PagamentoAntecipadoFatura { LojaId = lojaId, FaturaCartaoId = fatura.Id, ContaBancariaId = contaDoAntecipado ?? contaDoCartao, Valor = antecipado, Data = DateTime.UtcNow });
            await db.SaveChangesAsync();
        });

    // ── Lançamentos (parte 2) ────────────────────────────────────────────────

    public static async Task<Guid> AvulsoPelaApiAsync(this LojaTeste loja, Guid contaId, string tipo = "pagar", string descricao = "Conta avulsa", decimal valor = 100m, string vencimento = "2032-03-10", Guid? categoriaId = null, bool jaPago = false)
    {
        var r = await loja.Painel.Post($"{Rota}/lancamentos/avulso", new { contaBancariaId = contaId, tipo, descricao, categoriaId, valor, vencimento, observacao = (string?)null, jaPago });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static async Task<Guid> ParceladoPelaApiAsync(this LojaTeste loja, Guid contaId, int parcelas = 3, decimal valorParcela = 50m, string primeiroVencimento = "2032-01-15", string tipo = "pagar", string descricao = "Financiamento")
    {
        var r = await loja.Painel.Post($"{Rota}/lancamentos/parcelado", new { contaBancariaId = contaId, tipo, descricao, valorParcela, totalParcelas = parcelas, primeiroVencimento });
        r.DeveSerSucesso();
        return r.Json.GetProperty("grupoParcelamentoId").GetGuid();
    }

    public static Task<List<LancamentoFinanceiro>> LancamentosDaLojaNoBancoAsync(this ApiFixture api, Guid lojaId) =>
        api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().Where(l => l.LojaId == lojaId).OrderBy(l => l.Vencimento).ThenBy(l => l.NumeroParcela).ToListAsync());

    public static Task<LancamentoFinanceiro> LancamentoNoBancoPorIdAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().FirstAsync(l => l.Id == id));

    public static Task<List<LancamentoFinanceiro>> GrupoNoBancoAsync(this ApiFixture api, Guid grupoId) =>
        api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().Where(l => l.GrupoParcelamentoId == grupoId).OrderBy(l => l.NumeroParcela).ToListAsync());

    public static Task<List<LancamentoFixo>> FixosNoBancoAsync(this ApiFixture api, Guid lojaId) =>
        api.NoBancoAsync(db => db.LancamentosFixos.AsNoTracking().Where(f => f.LojaId == lojaId).ToListAsync());

    public static Task<Guid> CartaoNoBancoAsync(this ApiFixture api, Guid lojaId, Guid contaId, string nome = "Cartão Teste") =>
        api.NoBancoAsync(async db =>
        {
            var cartao = new CartaoCredito { LojaId = lojaId, Nome = nome, ContaBancariaId = contaId };
            db.CartoesCredito.Add(cartao);
            await db.SaveChangesAsync();
            return cartao.Id;
        });

    public static Task<List<LancamentoCartao>> LancamentosDeCartaoNoBancoAsync(this ApiFixture api, Guid lojaId) =>
        api.NoBancoAsync(db => db.LancamentosCartao.AsNoTracking().Where(l => l.LojaId == lojaId).OrderBy(l => l.DataCompra).ToListAsync());

    // ── Cartões (parte 3a) ───────────────────────────────────────────────────

    public static async Task<Guid> CartaoPelaApiAsync(this LojaTeste loja, Guid contaId, string nome = "Cartão Teste", int diaFechamento = 10, int diaVencimento = 15, string? bandeira = null)
    {
        var r = await loja.Painel.Post($"{Rota}/cartoes", new { nome, limite = 1000m, diaFechamento, diaVencimento, contaBancariaId = contaId, taxaJurosMensal = 0m, bandeira });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static Task<CartaoCredito> CartaoNoBancoPorIdAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.CartoesCredito.AsNoTracking().FirstAsync(c => c.Id == id));

    public static Task<List<LancamentoCartao>> ComprasDoCartaoNoBancoAsync(this ApiFixture api, Guid cartaoId) =>
        api.NoBancoAsync(db => db.LancamentosCartao.AsNoTracking().Where(l => l.CartaoCreditoId == cartaoId).OrderBy(l => l.DataCompra).ThenBy(l => l.NumeroParcela).ToListAsync());

    public static Task<List<CartaoLancamentoFixo>> FixosDoCartaoNoBancoAsync(this ApiFixture api, Guid cartaoId) =>
        api.NoBancoAsync(db => db.CartaoLancamentosFixos.AsNoTracking().Where(f => f.CartaoCreditoId == cartaoId).ToListAsync());

    // Fatura já paga do mês de vencimento informado (as compras do ciclo dela ficam travadas)
    public static Task FaturaPagaDoMesNoBancoAsync(this ApiFixture api, Guid lojaId, Guid cartaoId, int ano, int mes) =>
        api.NoBancoAsync(async db =>
        {
            db.FaturasCartao.Add(new FaturaCartao
            {
                LojaId = lojaId, CartaoCreditoId = cartaoId, MesReferencia = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc),
                Vencimento = new DateTime(ano, mes, 15, 12, 0, 0, DateTimeKind.Utc), Total = 1m, ValorPago = 1m, Status = "pago", PagoEm = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

    public static async Task SemVinculoAsync(this ApiFixture api, LojaTeste loja) =>
        await api.NoBancoAsync(db => db.UsuariosLoja.Where(ul => ul.LojaId == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(ul => ul.Ativo, false)));
}
