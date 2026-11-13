using LojaApi.Domain.Fiscal;

namespace LojaApi.Tests.Domain.AggregatesModel.FiscalTeste;

public class FiscalRegrasTests
{
    [Theory]
    [InlineData("homologacao", true)]
    [InlineData("producao", true)]
    [InlineData("Producao", false)]
    [InlineData("teste", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Ambiente_valido(string? ambiente, bool esperado) => Assert.Equal(esperado, FiscalRegras.AmbienteValido(ambiente));

    [Theory]
    [InlineData("2", "2")]
    [InlineData("  3 ", "3")]
    [InlineData("", "1")]
    [InlineData("   ", "1")]
    [InlineData(null, "1")]
    public void Serie_em_branco_vira_1(string? serie, string esperado) => Assert.Equal(esperado, FiscalRegras.SerieOuPadrao(serie));

    [Theory]
    [InlineData("focus", "token", true)]
    [InlineData("focus", "", false)]
    [InlineData("focus", null, false)]
    [InlineData("", "token", false)]
    [InlineData("  ", "token", false)]
    [InlineData(null, null, false)]
    public void Configuracao_so_fica_ativa_com_provedor_e_credencial(string? provedor, string? credencial, bool esperado) =>
        Assert.Equal(esperado, FiscalRegras.ConfiguracaoAtiva(provedor, credencial));

    [Theory]
    [InlineData("nfce", true)]
    [InlineData("chacara_reservas,nfce", true)]
    [InlineData("chacara_reservas , nfce ,agenda", true)]
    [InlineData("chacara_reservas", false)]
    [InlineData("nfce_extra", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Modulo_ativo_na_lista(string? modulos, bool esperado) => Assert.Equal(esperado, FiscalRegras.ModuloAtivo(modulos, FiscalRegras.ModuloNfce));

    [Theory]
    [InlineData("Cliente desistiu da compra", true)]
    [InlineData("123456789012345", true)]           // 15 caracteres
    [InlineData("12345678901234", false)]           // 14
    [InlineData("  12345678901234  ", false)]       // espaços não contam
    [InlineData("               ", false)]
    [InlineData(null, false)]
    public void Justificativa_precisa_de_15_caracteres(string? justificativa, bool esperado) =>
        Assert.Equal(esperado, FiscalRegras.JustificativaValida(justificativa));

    [Fact]
    public void So_nota_autorizada_pode_ser_cancelada()
    {
        Assert.True(FiscalRegras.PodeCancelar("autorizada"));
        Assert.All(new[] { "erro", "processando", "cancelada", "rejeitada", "pendente" }, s => Assert.False(FiscalRegras.PodeCancelar(s)));
    }
}
