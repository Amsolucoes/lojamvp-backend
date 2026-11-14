using LojaApi.Models;
using LojaApi.Shared.Application;
using static LojaApi.Application.Produtos.ProdutosApoio;

namespace LojaApi.Application.Produtos;

// Cadastra um produto; se já nasce com estoque, registra o movimento de "Estoque inicial".
public class CriarProdutoCommandHandler(IProdutosRepository repositorio) : ICommandHandler<CriarProdutoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarProdutoCommand command)
    {
        var (lojaId, req) = command;

        if (!string.IsNullOrWhiteSpace(req.CodigoBarras) && await repositorio.CodigoDeBarrasDeProdutoEmUsoAsync(req.CodigoBarras, lojaId, null))
            return Resultado.Conflito($"O código de barras '{req.CodigoBarras}' já está cadastrado em outro produto da sua loja.");

        var erroReferencia = await ValidarMarcaEFornecedorAsync(repositorio, lojaId, req.MarcaId, req.FornecedorId);
        if (erroReferencia != null) return Resultado.Invalido(erroReferencia);

        var produto = new Produto
        {
            Nome = req.Nome,
            Descricao = req.Descricao,
            Categoria = req.Categoria,
            PrecoCusto = req.PrecoCusto,
            PrecoVenda = req.PrecoVenda,
            Estoque = req.Estoque,
            EstoqueMinimo = req.EstoqueMinimo,
            CodigoBarras = req.CodigoBarras,
            Ativo = req.Ativo,
            TipoVenda = req.TipoVenda,
            UnidadeMedida = req.UnidadeMedida,
            MarcaId = req.MarcaId,
            FornecedorId = req.FornecedorId,
            LojaId = lojaId,
        };
        repositorio.Adicionar(produto);

        if (req.Estoque > 0)
            repositorio.Adicionar(new MovimentoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = "entrada",
                Quantidade = req.Estoque,
                Observacao = "Estoque inicial",
                LojaId = lojaId,
            });

        await repositorio.SalvarAsync();

        var criado = await repositorio.ObterProdutoCompletoAsync(produto.Id);
        return Resultado.Ok(ParaDto(criado!));
    }
}
