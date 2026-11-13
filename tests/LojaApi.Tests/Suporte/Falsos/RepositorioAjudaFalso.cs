using LojaApi.Application.Ajuda;
using LojaApi.src.Models;

namespace LojaApi.Tests.Suporte.Falsos;

// Repositório da Central de Ajuda em memória: permite testar o SERVICE (as regras) sem banco.
// "Salvamentos" conta quantas vezes o service mandou gravar (zero = nada foi gravado).
public sealed class RepositorioAjudaFalso : IAjudaRepository
{
    public List<CategoriaVideoAjuda> Categorias { get; } = [];
    public List<VideoAjuda> Videos { get; } = [];
    public int Salvamentos { get; private set; }

    public Task<List<CategoriaPublicaDto>> ListarCategoriasAtivasAsync() => Task.FromResult(
        Categorias.Where(c => c.Ativa).OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .Select(c => new CategoriaPublicaDto(c.Id, c.Nome, c.Ordem, c.ModulosRelacionados)).ToList());

    public Task<List<CategoriaVideoAjuda>> ListarCategoriasAsync() => Task.FromResult(Categorias.OrderBy(c => c.Ordem).ThenBy(c => c.Nome).ToList());
    public Task<CategoriaVideoAjuda?> ObterCategoriaAsync(Guid id) => Task.FromResult(Categorias.FirstOrDefault(c => c.Id == id));
    public Task<bool> ExisteCategoriaComNomeAsync(string nome) => Task.FromResult(Categorias.Any(c => c.Nome == nome));
    public Task<bool> CategoriaEmUsoAsync(string nomeCategoria) => Task.FromResult(Videos.Any(v => v.Categoria == nomeCategoria));
    public void Adicionar(CategoriaVideoAjuda categoria) => Categorias.Add(categoria);
    public void Remover(CategoriaVideoAjuda categoria) => Categorias.Remove(categoria);

    public Task<List<VideoPublicoDto>> ListarVideosAtivosAsync() => Task.FromResult(
        Videos.Where(v => v.Ativo).OrderBy(v => v.Categoria).ThenBy(v => v.Ordem)
            .Select(v => new VideoPublicoDto(v.Id, v.Titulo, v.Categoria, v.YoutubeId, v.Ordem)).ToList());

    public Task<List<VideoAjuda>> ListarVideosAsync() => Task.FromResult(Videos.OrderBy(v => v.Categoria).ThenBy(v => v.Ordem).ToList());
    public Task<List<VideoAjuda>> ListarVideosDaCategoriaAsync(string nomeCategoria) => Task.FromResult(Videos.Where(v => v.Categoria == nomeCategoria).ToList());
    public Task<VideoAjuda?> ObterVideoAsync(Guid id) => Task.FromResult(Videos.FirstOrDefault(v => v.Id == id));
    public void Adicionar(VideoAjuda video) => Videos.Add(video);
    public void Remover(VideoAjuda video) => Videos.Remove(video);

    public Task SalvarAsync() { Salvamentos++; return Task.CompletedTask; }
}
