using LojaApi.Domain.Ajuda;
using LojaApi.Shared.Application;
using LojaApi.src.Models;

namespace LojaApi.Application.Ajuda;

// Vídeos da Central de Ajuda (gestão só do superadmin; a lista de ativos é pública).
public class VideosAjudaService(IAjudaRepository repositorio)
{
    public async Task<Resultado<object>> ListarAtivosAsync() => Resultado.Ok(await repositorio.ListarVideosAtivosAsync());

    public async Task<Resultado<object>> ListarTodosAsync() => Resultado.Ok(await repositorio.ListarVideosAsync());

    public async Task<Resultado<object>> CriarAsync(SalvarVideoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Titulo) || string.IsNullOrWhiteSpace(req.Categoria) || string.IsNullOrWhiteSpace(req.YoutubeId))
            return Resultado.Invalido("Preencha título, categoria e ID do YouTube.");

        var video = new VideoAjuda
        {
            Titulo = req.Titulo.Trim(),
            Categoria = req.Categoria.Trim(),
            YoutubeId = YoutubeId.Extrair(req.YoutubeId.Trim()),
            Ordem = req.Ordem,
            Ativo = req.Ativo,
        };
        repositorio.Adicionar(video);
        await repositorio.SalvarAsync();

        return Resultado.Ok(video);
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid id, SalvarVideoRequest req)
    {
        var video = await repositorio.ObterVideoAsync(id);
        if (video is null) return Resultado.NaoEncontrado();

        video.Titulo = req.Titulo.Trim();
        video.Categoria = req.Categoria.Trim();
        video.YoutubeId = YoutubeId.Extrair(req.YoutubeId.Trim());
        video.Ordem = req.Ordem;
        video.Ativo = req.Ativo;

        await repositorio.SalvarAsync();
        return Resultado.Ok(video);
    }

    public async Task<Resultado<object>> AlternarAtivoAsync(Guid id)
    {
        var video = await repositorio.ObterVideoAsync(id);
        if (video is null) return Resultado.NaoEncontrado();

        video.Ativo = !video.Ativo;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { video.Id, video.Ativo });
    }

    public async Task<Resultado<object>> ExcluirAsync(Guid id)
    {
        var video = await repositorio.ObterVideoAsync(id);
        if (video is null) return Resultado.NaoEncontrado();

        repositorio.Remover(video);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Vídeo excluído." });
    }
}
