using LojaApi.Domain.Financeiro;

namespace LojaApi.Tests.Domain.AggregatesModel.FinanceiroTeste;

// Regras puras do Financeiro: nome de categoria, categorias padrão, ajuste de saldo e texto das transferências (sem banco, sem HTTP).
public class FinanceiroRegrasTests
{
    [Theory]
    [InlineData("  Água ", "agua")]
    [InlineData("SALÁRIO/Folha", "salario/folha")]
    [InlineData(null, "")]
    public void Nome_da_categoria_ignora_maiuscula_acento_e_espacos(string? nome, string esperado) =>
        Assert.Equal(esperado, FinanceiroRegras.NormalizarNomeCategoria(nome));

    [Theory]
    [InlineData("Água", "pagar", "agua", "pagar", true)]
    [InlineData("Água", "pagar", "agua", "receber", false)]
    [InlineData("Água", "ambos", "agua", "receber", true)]
    [InlineData("Água", "pagar", "agua", "ambos", true)]
    [InlineData("Luz", "pagar", "agua", "pagar", false)]
    public void Categoria_equivalente_precisa_do_mesmo_nome_e_de_tipo_compativel(string nomeExistente, string tipoExistente, string nomeNovo, string tipoNovo, bool esperado) =>
        Assert.Equal(esperado, FinanceiroRegras.CategoriaEquivalente(nomeExistente, tipoExistente, nomeNovo, tipoNovo));

    [Fact]
    public void Categorias_padrao_sao_10_e_tem_pagar_receber_e_ambos()
    {
        Assert.Equal(10, FinanceiroRegras.CategoriasPadrao.Length);
        Assert.Equal(7, FinanceiroRegras.CategoriasPadrao.Count(c => c.Tipo == "pagar"));
        Assert.Equal(2, FinanceiroRegras.CategoriasPadrao.Count(c => c.Tipo == "receber"));
        Assert.Equal("Outros", FinanceiroRegras.CategoriasPadrao.Single(c => c.Tipo == "ambos").Nome);
        Assert.Equal(FinanceiroRegras.CategoriasPadrao.Length, FinanceiroRegras.CategoriasPadrao.Select(c => c.Nome).Distinct().Count());
    }

    [Fact]
    public void Valor_do_ajuste_leva_o_saldo_ate_o_novo_valor_ou_usa_o_informado()
    {
        Assert.Equal(80m, FinanceiroRegras.ValorDoAjuste("ajuste", 180m, null, 100m));          // registra a diferença até o novo saldo
        Assert.Equal(-50m, FinanceiroRegras.ValorDoAjuste("ajuste", 50m, 999m, 100m));          // no ajuste o valor informado não vale
        Assert.Equal(10m, FinanceiroRegras.ValorDoAjuste("entrada", 0m, 10m, 100m));
        Assert.Equal(5m, FinanceiroRegras.ValorDoAjuste("saida", 0m, 5m, 100m));
        Assert.Equal(0m, FinanceiroRegras.ValorDoAjuste("entrada", 0m, null, 100m));            // sem valor: registra zero
    }

    [Fact]
    public void Texto_da_transferencia_so_tem_o_traco_quando_ha_observacao()
    {
        Assert.Equal("Transferência para Caixa — Reforço", FinanceiroRegras.ObservacaoDaTransferencia("para", "Caixa", "Reforço"));
        Assert.Equal("Transferência de Banco", FinanceiroRegras.ObservacaoDaTransferencia("de", "Banco", null));
        Assert.Equal("Transferência de Banco", FinanceiroRegras.ObservacaoDaTransferencia("de", "Banco", "  "));
    }
}
