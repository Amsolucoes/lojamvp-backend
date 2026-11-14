using static LojaApi.Tests.Suporte.CenariosProdutos;

namespace LojaApi.Tests.Api.ProdutosTeste;

// Uma loja nunca mexe nos dados de outra: marca e fornecedor do produto, e variações (editar, desativar e ajustar o estoque).
// (Antes, o cadastro aceitava marca/fornecedor de outra loja e as variações de qualquer loja podiam ser editadas pelo id.)
[Collection(ColecaoApi.Nome)]
public class IsolamentoProdutosApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    [FatoIntegracao]
    public async Task Criar_e_atualizar_produto_recusam_marca_e_fornecedor_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var marcaDela = await outra.MarcaIdAsync("Dela");
        var fornecedorDela = await outra.FornecedorIdAsync("Dela");
        var meuMarca = await loja.MarcaIdAsync("Minha");
        var id = await loja.ProdutoPelaApiAsync("Camisa", marca: meuMarca);

        Assert.Equal((400, "Marca não encontrada."), await Erro(loja.Painel.Post(Rota, CorpoProduto("X", marca: marcaDela))));
        Assert.Equal((400, "Fornecedor não encontrado."), await Erro(loja.Painel.Post(Rota, CorpoProduto("X", fornecedor: fornecedorDela))));
        Assert.Equal((400, "Marca não encontrada."), await Erro(loja.Painel.Put($"{Rota}/{id}", CorpoProduto("Camisa", marca: marcaDela))));
        Assert.Equal((400, "Fornecedor não encontrado."), await Erro(loja.Painel.Put($"{Rota}/{id}", CorpoProduto("Camisa", fornecedor: fornecedorDela))));

        Assert.Single((await loja.Painel.Get(Rota)).Json.EnumerateArray());                         // nada novo foi criado
        Assert.Equal(meuMarca, (await api.ProdutoDaLojaNoBancoAsync(id)).MarcaId);                  // o produto continua com a marca dele
    }

    [FatoIntegracao]
    public async Task Editar_e_desativar_variacao_de_outra_loja_da_404_e_nao_muda_nada()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var produtoDela = await outra.ProdutoPelaApiAsync("Dela", 0m);
        var variacaoDela = await outra.VariacaoPelaApiAsync(produtoDela, "P", "Azul", 5);

        var editar = await loja.Painel.Put($"{Rota}/{produtoDela}/variacoes/{variacaoDela}", CorpoVariacao("G", "Preto", null, null, 99));
        var remover = await loja.Painel.Delete($"{Rota}/{produtoDela}/variacoes/{variacaoDela}");

        Assert.Equal((404, 404), (editar.Codigo, remover.Codigo));
        var depois = await api.VariacaoNoBancoDoProdutoAsync(variacaoDela);
        Assert.Equal(("P", "Azul", 5, true), (depois.Tamanho, depois.Cor, depois.Estoque, depois.Ativo));
        Assert.Empty(await api.MovimentosDoProdutoNoBancoAsync(produtoDela));
    }

    [FatoIntegracao]
    public async Task Ajuste_de_estoque_de_variacao_de_outra_loja_da_404_e_nao_muda_nada()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var produtoDela = await outra.ProdutoPelaApiAsync("Dela", 0m);
        var variacaoDela = await outra.VariacaoPelaApiAsync(produtoDela, "P", "Azul", 5);

        var r = await loja.Painel.Post("/api/estoque/ajuste-variacao", new { produtoId = produtoDela, variacaoId = variacaoDela, tipo = "saida", quantidade = 3 });

        Assert.Equal((404, "Variação não encontrada."), (r.Codigo, r.Erro));
        Assert.Equal(5, (await api.VariacaoNoBancoDoProdutoAsync(variacaoDela)).Estoque);
        Assert.Empty(await api.MovimentosDoProdutoNoBancoAsync(produtoDela));
    }

    [FatoIntegracao]
    public async Task Marca_e_fornecedor_da_propria_loja_e_variacao_propria_continuam_funcionando()
    {
        var loja = await NovaAsync();
        var marca = await loja.MarcaIdAsync("Bosch");
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var id = await loja.ProdutoPelaApiAsync("Camisa", 0m, marca: marca, fornecedor: fornecedor);
        var v = await loja.VariacaoPelaApiAsync(id, "P", "Azul", 5);

        Assert.Equal(200, (await loja.Painel.Put($"{Rota}/{id}", CorpoProduto("Camisa 2", marca: marca, fornecedor: fornecedor))).Codigo);
        Assert.Equal(200, (await loja.Painel.Put($"{Rota}/{id}/variacoes/{v}", CorpoVariacao("P", "Azul", null, null, 6))).Codigo);
        Assert.Equal(200, (await loja.Painel.Post("/api/estoque/ajuste-variacao", new { produtoId = id, variacaoId = v, tipo = "saida", quantidade = 1 })).Codigo);
        Assert.Equal(5, (await api.VariacaoNoBancoDoProdutoAsync(v)).Estoque);
        Assert.Equal(204, (await loja.Painel.Delete($"{Rota}/{id}/variacoes/{v}")).Codigo);
    }
}
