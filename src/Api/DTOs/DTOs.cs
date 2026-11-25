namespace LojaApi.DTOs;

// ── Usuario ───────────────────────────────────────────────────────
public record UsuarioDto(Guid Id, string Nome, string Email, string Role, bool Ativo);
public record CriarUsuarioRequest(string Nome, string Email, string Senha, string Role);
public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
// ── Perfis ────────────────────────────────────────────────────────
public record PerfilLojaDto(
    Guid Id, string Nome, string? Descricao, string Icone,
    List<CategoriaPerfilDto> Categorias,
    List<CampoExtraPerfilDto> CamposExtras
);
public record CategoriaPerfilDto(Guid Id, string Nome, int Ordem);
public record CampoExtraPerfilDto(
    Guid Id, string Chave, string Label,
    string Tipo, string? Opcoes,
    bool Obrigatorio, int Ordem
);
public record CategoriaLojaDto(Guid Id, string Nome, bool Ativo, int Ordem);
public record CampoExtraLojaDto(
    Guid Id, string Chave, string Label,
    string Tipo, string? Opcoes,
    bool Obrigatorio, bool Ativo, int Ordem
);

