namespace LojaApi.Application.Ajuda;

public record CategoriaPublicaDto(Guid Id, string Nome, int Ordem, string? ModulosRelacionados);

public record VideoPublicoDto(Guid Id, string Titulo, string Categoria, string YoutubeId, int Ordem);
