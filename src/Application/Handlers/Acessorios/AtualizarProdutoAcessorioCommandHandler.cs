using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Edita um produto (superadmin).
public class AtualizarProdutoAcessorioCommandHandler(IAcessoriosRepository repositorio) : ICommandHandler<AtualizarProdutoAcessorioCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarProdutoAcessorioCommand command)
    {
        var (id, req) = command;
        var produto = await repositorio.ObterProdutoAsync(id);
        if (produto is null) return Resultado.NaoEncontrado();

        produto.Nome = req.Nome.Trim();
        produto.Descricao = req.Descricao;
        produto.Preco = req.Preco;
        produto.PrecoPromocional = req.PrecoPromocional;
        produto.Estoque = req.Estoque;
        produto.Categoria = req.Categoria;
        produto.ImagensUrls = req.ImagensUrls;
        produto.PesoKg = req.PesoKg;
        produto.Ativo = req.Ativo;
        produto.Destaque = req.Destaque;
        produto.Ordem = req.Ordem;
        produto.AtualizadoEm = DateTime.UtcNow;

        await repositorio.SalvarAsync();
        return Resultado.Ok(produto);
    }
}
