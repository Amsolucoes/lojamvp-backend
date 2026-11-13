using LojaApi.Application.Ajuda;
using LojaApi.Data;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Ajuda;

// Implementação EF Core do acesso aos dados da Central de Ajuda.
public class AjudaRepository(AppDbContext db) : IAjudaRepository
{
    public Task<List<CategoriaPublicaDto>> ListarCategoriasAtivasAsync() =>
        db.CategoriasVideoAjuda
            .Where(c => c.Ativa)
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .Select(c => new CategoriaPublicaDto(c.Id, c.Nome, c.Ordem, c.ModulosRelacionados))
            .ToListAsync();

    public Task<List<CategoriaVideoAjuda>> ListarCategoriasAsync() =>
        db.CategoriasVideoAjuda.OrderBy(c => c.Ordem).ThenBy(c => c.Nome).ToListAsync();

    public Task<CategoriaVideoAjuda?> ObterCategoriaAsync(Guid id) => db.CategoriasVideoAjuda.FindAsync(id).AsTask();

    public Task<bool> ExisteCategoriaComNomeAsync(string nome) => db.CategoriasVideoAjuda.AnyAsync(c => c.Nome == nome);

    public Task<bool> CategoriaEmUsoAsync(string nomeCategoria) => db.VideosAjuda.AnyAsync(v => v.Categoria == nomeCategoria);

    public void Adicionar(CategoriaVideoAjuda categoria) => db.CategoriasVideoAjuda.Add(categoria);
    public void Remover(CategoriaVideoAjuda categoria) => db.CategoriasVideoAjuda.Remove(categoria);

    public Task<List<VideoPublicoDto>> ListarVideosAtivosAsync() =>
        db.VideosAjuda
            .Where(v => v.Ativo)
            .OrderBy(v => v.Categoria).ThenBy(v => v.Ordem)
            .Select(v => new VideoPublicoDto(v.Id, v.Titulo, v.Categoria, v.YoutubeId, v.Ordem))
            .ToListAsync();

    public Task<List<VideoAjuda>> ListarVideosAsync() =>
        db.VideosAjuda.OrderBy(v => v.Categoria).ThenBy(v => v.Ordem).ToListAsync();

    public Task<List<VideoAjuda>> ListarVideosDaCategoriaAsync(string nomeCategoria) =>
        db.VideosAjuda.Where(v => v.Categoria == nomeCategoria).ToListAsync();

    public Task<VideoAjuda?> ObterVideoAsync(Guid id) => db.VideosAjuda.FindAsync(id).AsTask();

    public void Adicionar(VideoAjuda video) => db.VideosAjuda.Add(video);
    public void Remover(VideoAjuda video) => db.VideosAjuda.Remove(video);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
