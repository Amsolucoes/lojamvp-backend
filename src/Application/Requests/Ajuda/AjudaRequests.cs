namespace LojaApi.Application.Ajuda;

public record SalvarCategoriaRequest(string Nome, int Ordem, bool Ativa, string? ModulosRelacionados = null);

public record SalvarVideoRequest(string Titulo, string Categoria, string YoutubeId, int Ordem, bool Ativo);
