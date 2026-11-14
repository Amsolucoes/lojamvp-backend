using LojaApi.Shared.Application;
using static LojaApi.Application.Produtos.ProdutosApoio;

namespace LojaApi.Application.Produtos;

// Atualiza o cadastro de um produto (o estoque só muda pela tela de Estoque).
public class AtualizarProdutoCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AtualizarProdutoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarProdutoCommand command)
    {
        var (lojaId, id, req) = command;
        var produto = await repositorio.ObterProdutoAsync(id);
        if (produto is null || (lojaId.HasValue && produto.LojaId != lojaId)) return Resultado.NaoEncontrado();

        if (!string.IsNullOrWhiteSpace(req.CodigoBarras) && req.CodigoBarras != produto.CodigoBarras
            && await repositorio.CodigoDeBarrasDeProdutoEmUsoAsync(req.CodigoBarras, lojaId, id))
            return Resultado.Conflito($"O código de barras '{req.CodigoBarras}' já está cadastrado em outro produto da sua loja.");

        var erroReferencia = await ValidarMarcaEFornecedorAsync(repositorio, lojaId, req.MarcaId, req.FornecedorId);
        if (erroReferencia != null) return Resultado.Invalido(erroReferencia);

        produto.Nome = req.Nome; produto.Descricao = req.Descricao;
        produto.Categoria = req.Categoria; produto.PrecoCusto = req.PrecoCusto;
        produto.PrecoVenda = req.PrecoVenda; produto.EstoqueMinimo = req.EstoqueMinimo;
        produto.CodigoBarras = req.CodigoBarras; produto.Ativo = req.Ativo;
        produto.TipoVenda = req.TipoVenda;
        produto.UnidadeMedida = req.UnidadeMedida;
        produto.MarcaId = req.MarcaId;
        produto.FornecedorId = req.FornecedorId;
        produto.AtualizadoEm = DateTime.UtcNow;

        await repositorio.SalvarAsync();

        var atualizado = await repositorio.ObterProdutoCompletoAsync(produto.Id);
        return Resultado.Ok(ParaDto(atualizado!));
    }
}
