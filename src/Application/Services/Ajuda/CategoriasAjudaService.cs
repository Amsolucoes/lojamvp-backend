using LojaApi.Shared.Application;
using LojaApi.src.Models;

namespace LojaApi.Application.Ajuda;

// Categorias dos vídeos da Central de Ajuda (gestão só do superadmin; a lista de ativas é pública).
public class CategoriasAjudaService(IAjudaRepository repositorio)
{
    public async Task<Resultado<object>> ListarAtivasAsync() => Resultado.Ok(await repositorio.ListarCategoriasAtivasAsync());

    public async Task<Resultado<object>> ListarTodasAsync() => Resultado.Ok(await repositorio.ListarCategoriasAsync());

    public async Task<Resultado<object>> CriarAsync(SalvarCategoriaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome)) return Resultado.Invalido("Informe o nome da categoria.");

        if (await repositorio.ExisteCategoriaComNomeAsync(req.Nome.Trim()))
            return Resultado.Invalido("Já existe uma categoria com esse nome.");

        var categoria = new CategoriaVideoAjuda
        {
            Nome = req.Nome.Trim(),
            Ordem = req.Ordem,
            Ativa = req.Ativa,
            ModulosRelacionados = req.ModulosRelacionados,
        };
        repositorio.Adicionar(categoria);
        await repositorio.SalvarAsync();

        return Resultado.Ok(categoria);
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid id, SalvarCategoriaRequest req)
    {
        var categoria = await repositorio.ObterCategoriaAsync(id);
        if (categoria is null) return Resultado.NaoEncontrado();

        var nomeAnterior = categoria.Nome;

        categoria.Nome = req.Nome.Trim();
        categoria.Ordem = req.Ordem;
        categoria.Ativa = req.Ativa;
        categoria.ModulosRelacionados = req.ModulosRelacionados;
        await repositorio.SalvarAsync();

        // Mantém os vídeos já cadastrados apontando pro nome novo da categoria
        if (nomeAnterior != categoria.Nome)
        {
            foreach (var v in await repositorio.ListarVideosDaCategoriaAsync(nomeAnterior)) v.Categoria = categoria.Nome;
            await repositorio.SalvarAsync();
        }

        return Resultado.Ok(categoria);
    }

    // Categoria com vídeo não some (os vídeos perderiam o agrupamento): é desativada.
    public async Task<Resultado<object>> ExcluirAsync(Guid id)
    {
        var categoria = await repositorio.ObterCategoriaAsync(id);
        if (categoria is null) return Resultado.NaoEncontrado();

        if (await repositorio.CategoriaEmUsoAsync(categoria.Nome))
        {
            categoria.Ativa = false;
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Categoria em uso por vídeo(s) — foi desativada em vez de excluída." });
        }

        repositorio.Remover(categoria);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Categoria excluída." });
    }
}
