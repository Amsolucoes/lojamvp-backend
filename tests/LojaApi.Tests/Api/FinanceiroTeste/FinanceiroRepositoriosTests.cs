using LojaApi.Infrastructure.Financeiro;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosFinanceiro;

namespace LojaApi.Tests.Api.FinanceiroTeste;

// Isolamento entre lojas direto nos repositórios do Financeiro: os handlers já barram pelo item da loja, mas as consultas de
// grupo e de fixo também filtram pela loja (um id de outra loja nunca traz dados dela). Cenário só montável direto no banco.
[Collection(ColecaoApi.Nome)]
public class FinanceiroRepositoriosTests(ApiFixture api)
{
    private static DateTime Utc(int ano, int mes, int dia) => new(ano, mes, dia, 12, 0, 0, DateTimeKind.Utc);

    private async Task<(LojaTeste Loja, Guid Conta, Guid Cartao)> LojaComCartaoAsync()
    {
        var loja = await api.NovaLojaAsync();
        var conta = await loja.ContaPelaApiAsync();
        return (loja, conta, await api.CartaoNoBancoAsync(loja.Id, conta));
    }

    [FatoIntegracao]
    public async Task Descricoes_recentes_do_cartao_limitam_a_50()
    {
        var (loja, _, cartao) = await LojaComCartaoAsync();
        await api.NoBancoAsync(async db =>
        {
            for (int i = 1; i <= 51; i++)
                db.LancamentosCartao.Add(new LancamentoCartao { LojaId = loja.Id, CartaoCreditoId = cartao, Descricao = $"Compra {i:00}", Valor = 1m, DataCompra = Utc(2032, 1, 5), CriadoEm = new DateTime(2032, 1, 1, 0, 0, i, DateTimeKind.Utc) });
            await db.SaveChangesAsync();
        });

        var lista = (await loja.Painel.Get($"{Rota}/cartoes/lancamentos/descricoes")).Json.EnumerateArray().Select(x => x.GetString()).ToList();

        // O Distinct depois do OrderBy faz o Postgres descartar a ordenação: o limite vale, mas "as mais recentes" não é garantido (comportamento antigo)
        Assert.Equal(50, lista.Count);
        Assert.Equal(50, lista.Distinct().Count());
    }

    [FatoIntegracao]
    public async Task Repositorio_de_cartoes_so_traz_grupo_compras_do_fixo_e_fixo_da_propria_loja()
    {
        var (loja, _, cartao) = await LojaComCartaoAsync();
        var (outra, _, cartaoAlheio) = await LojaComCartaoAsync();
        var grupo = Guid.NewGuid();
        var fixoId = Guid.NewGuid();
        await api.NoBancoAsync(async db =>
        {
            db.CartaoLancamentosFixos.Add(new CartaoLancamentoFixo { Id = fixoId, LojaId = outra.Id, CartaoCreditoId = cartaoAlheio, Descricao = "Fixo alheio", Valor = 1m });
            await db.SaveChangesAsync();
            db.LancamentosCartao.Add(new LancamentoCartao { LojaId = loja.Id, CartaoCreditoId = cartao, Descricao = "Minha", Valor = 1m, DataCompra = Utc(2032, 2, 5), GrupoParcelamentoId = grupo });
            db.LancamentosCartao.Add(new LancamentoCartao { LojaId = outra.Id, CartaoCreditoId = cartaoAlheio, Descricao = "Alheia", Valor = 1m, DataCompra = Utc(2032, 2, 5), GrupoParcelamentoId = grupo, CartaoFixoId = fixoId, Modo = "fixa" });
            await db.SaveChangesAsync();
        });

        var (grupoDaLoja, fixoDaLoja, fixoPorLoja, fixoDaOutra) = await api.NoBancoAsync(async db =>
        {
            var repo = new CartoesRepository(db);
            return (await repo.ListarGrupoDeComprasAsync(grupo, loja.Id), await repo.ListarComprasDoFixoAPartirDeAsync(fixoId, loja.Id, Utc(2020, 1, 1)),
                await repo.ObterFixoPorLojaAsync(fixoId, loja.Id), await repo.ObterFixoPorLojaAsync(fixoId, outra.Id));
        });

        Assert.Equal(["Minha"], grupoDaLoja.Select(c => c.Descricao));
        Assert.Empty(fixoDaLoja);
        Assert.Null(fixoPorLoja);
        Assert.NotNull(fixoDaOutra);
    }

    [FatoIntegracao]
    public async Task Repositorio_de_lancamentos_so_traz_pendentes_do_grupo_e_do_fixo_da_propria_loja()
    {
        var loja = await api.NovaLojaAsync();
        var outra = await api.NovaLojaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var contaAlheia = await outra.ContaPelaApiAsync();
        var grupo = Guid.NewGuid();
        var fixoId = Guid.NewGuid();
        await api.NoBancoAsync(async db =>
        {
            db.LancamentosFixos.Add(new LancamentoFixo { Id = fixoId, LojaId = outra.Id, ContaBancariaId = contaAlheia, Descricao = "Fixo alheio", Valor = 1m });
            await db.SaveChangesAsync();
            LancamentoFinanceiro Novo(Guid lojaId, Guid contaId, string desc) => new()
            {
                LojaId = lojaId, ContaBancariaId = contaId, Tipo = "pagar", Descricao = desc, Valor = 1m, Vencimento = Utc(2032, 3, 5), GrupoParcelamentoId = grupo, LancamentoFixoId = fixoId,
            };
            db.LancamentosFinanceiros.Add(Novo(loja.Id, conta, "Minha"));
            db.LancamentosFinanceiros.Add(Novo(outra.Id, contaAlheia, "Alheia"));
            await db.SaveChangesAsync();
        });

        var (grupoDaLoja, fixoDaLoja, fixoPorLoja, fixoDaOutra, grupoDaOutra) = await api.NoBancoAsync(async db =>
        {
            var repo = new LancamentosRepository(db);
            return (await repo.ListarPendentesDoGrupoAsync(grupo, loja.Id, null), await repo.ListarPendentesDoFixoAsync(fixoId, loja.Id, Utc(2020, 1, 1)),
                await repo.ObterFixoAsync(fixoId, loja.Id), await repo.ObterFixoAsync(fixoId, outra.Id), await repo.ListarPendentesDoGrupoAsync(grupo, outra.Id, null));
        });

        Assert.Equal(["Minha"], grupoDaLoja.Select(l => l.Descricao));
        Assert.Empty(fixoDaLoja.Where(l => l.Descricao == "Alheia"));
        Assert.Equal(["Minha"], fixoDaLoja.Select(l => l.Descricao));
        Assert.Null(fixoPorLoja);
        Assert.NotNull(fixoDaOutra);
        Assert.Equal(["Alheia"], grupoDaOutra.Select(l => l.Descricao));
    }
}
