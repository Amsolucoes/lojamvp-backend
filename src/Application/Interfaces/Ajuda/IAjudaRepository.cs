using LojaApi.src.Models;

namespace LojaApi.Application.Ajuda;

// Acesso aos dados da Central de Ajuda. O service decide as regras; aqui só se lê e grava.
// Implementado em Infrastructure/Ajuda (EF Core).
public interface IAjudaRepository
{
    // Categorias
    Task<List<CategoriaPublicaDto>> ListarCategoriasAtivasAsync();
    Task<List<CategoriaVideoAjuda>> ListarCategoriasAsync();
    Task<CategoriaVideoAjuda?> ObterCategoriaAsync(Guid id);
    Task<bool> ExisteCategoriaComNomeAsync(string nome);
    Task<bool> CategoriaEmUsoAsync(string nomeCategoria);
    void Adicionar(CategoriaVideoAjuda categoria);
    void Remover(CategoriaVideoAjuda categoria);

    // Vídeos
    Task<List<VideoPublicoDto>> ListarVideosAtivosAsync();
    Task<List<VideoAjuda>> ListarVideosAsync();
    Task<List<VideoAjuda>> ListarVideosDaCategoriaAsync(string nomeCategoria);
    Task<VideoAjuda?> ObterVideoAsync(Guid id);
    void Adicionar(VideoAjuda video);
    void Remover(VideoAjuda video);

    Task SalvarAsync();
}
