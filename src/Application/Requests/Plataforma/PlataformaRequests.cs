namespace LojaApi.Application.Plataforma;

// O que a API da Plataforma RECEBE (os nomes das propriedades são o JSON do corpo)

// ── Auth ──
public record LoginRequest(string Email, string Senha);

public record SignupRequest(
    string NomeLoja,
    string PerfilId,
    string NomeResponsavel,
    string Email,
    string Senha,
    string? Telefone,
    List<string>? ModulosExtras = null
);

public record TrocarSenhaRequest(string SenhaAtual, string NovaSenha);
public record TrocarMeuEmailRequest(string NovoEmail, string SenhaAtual);

// ── Perfis ──
public record AplicarPerfilRequest(Guid PerfilLojaId);
public record SalvarCategoriaRequest(string Nome, int Ordem);

// ── Preços dos módulos ──
public record AtualizarModuloPrecoRequest(decimal Valor, bool DisponivelParaAtivar);

// ── Cliente e Loja ──
public record AtualizarConfigRequest(string? Nome, string? CorPrimaria, string? LogoUrl, string? Endereco, string? Telefone);
public record AgendaHorarioRequest(int HoraInicio, int HoraFim);
public record AgendamentoOnlineConfigRequest(bool Ativo, string? Confirmacao, string? Slug);
public record AlternarModuloRequest(string Chave, bool Ativar);
public record PausaAgendamentoRequest(bool Ativar, DateTime? PausaAte, string? Mensagem);
public record DefinirSlugRequest(string Slug);

// ── Push ──
public record AssinarRequest(string Endpoint, string ChavePublica, string ChaveAuth);
public record CancelarRequest(string Endpoint);
