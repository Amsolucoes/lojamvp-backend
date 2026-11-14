using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Cadastra um produto (superadmin).
public class CriarProdutoAcessorioCommandHandler(IAcessoriosRepository repositorio) : ICommandHandler<CriarProdutoAcessorioCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarProdutoAcessorioCommand command)
    {
        var req = command.Req;
        if (string.IsNullOrWhiteSpace(req.Nome) || req.Preco <= 0)
            return Resultado.Invalido("Preencha nome e preço válido.");

        var produto = new ProdutoAcessorio
        {
            Nome = req.Nome.Trim(),
            Descricao = req.Descricao,
            Preco = req.Preco,
            PrecoPromocional = req.PrecoPromocional,
            Estoque = req.Estoque,
            Categoria = req.Categoria,
            ImagensUrls = req.ImagensUrls,
            PesoKg = req.PesoKg,
            Ativo = req.Ativo,
            Destaque = req.Destaque,
            Ordem = req.Ordem,
        };
        repositorio.Adicionar(produto);
        await repositorio.SalvarAsync();

        return Resultado.Ok(produto);
    }
}
