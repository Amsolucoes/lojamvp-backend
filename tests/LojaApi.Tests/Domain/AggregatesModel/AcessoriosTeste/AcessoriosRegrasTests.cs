using LojaApi.Domain.Acessorios;

namespace LojaApi.Tests.Domain.AggregatesModel.AcessoriosTeste;

// Regras puras da loja de acessórios: frete, prazo, vitrine e chave das categorias (sem banco, sem HTTP).
public class AcessoriosRegrasTests
{
    [Theory]
    [InlineData("MS", 15)]
    [InlineData("SP", 25)]
    [InlineData("MT", 25)]
    [InlineData("AM", 35)]      // estado fora da tabela: frete padrão
    [InlineData("", 35)]
    public void Frete_por_estado_usa_a_tabela_e_o_padrao_para_os_demais(string uf, decimal esperado)
    {
        Assert.Equal(esperado, AcessoriosRegras.FreteDaUf(uf));
        Assert.Equal(esperado, AcessoriosRegras.CalcularFrete(10_000m, uf));   // o frete grátis está desligado: o subtotal não muda nada
    }

    [Fact]
    public void Frete_gratis_esta_desligado_e_o_prazo_e_de_30_minutos()
    {
        Assert.Null(AcessoriosRegras.FreteGratisAcimaDe);
        Assert.Equal(TimeSpan.FromMinutes(30), AcessoriosRegras.PrazoPagamento);
        Assert.Equal(35m, AcessoriosRegras.FretePadrao);
    }

    [Fact]
    public void Produto_e_novo_ate_14_dias_de_criado()
    {
        var agora = new DateTime(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc), AcessoriosRegras.LimiteProdutoNovo(agora));
    }

    [Theory]
    [InlineData("Leitor de código", "leitor_de_codigo")]
    [InlineData("  Câmeras & Áudio  ", "cameras_audio")]
    [InlineData("Impressora-Fiscal 2", "impressora_fiscal_2")]
    [InlineData("A___B", "a_b")]
    [InlineData("!!! ???", "")]
    [InlineData("São João", "sao_joao")]
    public void Chave_da_categoria_e_minuscula_sem_acento_e_sem_simbolos_repetidos(string nome, string chave) =>
        Assert.Equal(chave, AcessoriosRegras.GerarChaveCategoria(nome));

    [Fact]
    public void Status_validos_do_pedido()
    {
        Assert.Equal(["aguardando_pagamento", "pago", "enviado", "entregue", "cancelado"], StatusPedidoAcessorio.Validos);
    }
}
