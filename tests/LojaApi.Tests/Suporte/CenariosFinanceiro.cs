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

    public static async Task SemVinculoAsync(this ApiFixture api, LojaTeste loja) =>
        await api.NoBancoAsync(db => db.UsuariosLoja.Where(ul => ul.LojaId == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(ul => ul.Ativo, false)));
}
