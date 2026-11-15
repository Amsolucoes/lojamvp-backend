namespace LojaApi.Shared.Application;

public enum TipoResultado { Ok, Invalido, NaoEncontrado, Conflito, Falha, NaoAutorizado, Proibido, Indisponivel }

// Resultado de um caso de uso. A camada de aplicação devolve isto (sem saber nada de HTTP)
// e o controller traduz para a resposta certa (200, 400, 401, 403, 404, 409, 500, 503) em ResultadoExtensions.
public sealed class Resultado<T>
{
    public TipoResultado Tipo { get; }
    public T? Valor { get; }
    public string? Mensagem { get; }
    // Corpo de erro próprio (quando a resposta precisa de mais campos além de "erro"); se nulo, o corpo é { erro: Mensagem }
    public object? Corpo { get; }

    private Resultado(TipoResultado tipo, T? valor, string? mensagem, object? corpo = null)
    {
        Tipo = tipo; Valor = valor; Mensagem = mensagem; Corpo = corpo;
    }

    public bool Sucesso => Tipo == TipoResultado.Ok;

    public static Resultado<T> Ok(T valor) => new(TipoResultado.Ok, valor, null);
    public static Resultado<T> Invalido(string mensagem) => new(TipoResultado.Invalido, default, mensagem);
    public static Resultado<T> NaoEncontrado(string? mensagem = null) => new(TipoResultado.NaoEncontrado, default, mensagem);
    public static Resultado<T> Conflito(string mensagem) => new(TipoResultado.Conflito, default, mensagem);
    public static Resultado<T> Falha(string mensagem) => new(TipoResultado.Falha, default, mensagem);
    public static Resultado<T> InvalidoComCorpo(string mensagem, object corpo) => new(TipoResultado.Invalido, default, mensagem, corpo);
    public static Resultado<T> NaoAutorizado(string? mensagem = null) => new(TipoResultado.NaoAutorizado, default, mensagem);
    public static Resultado<T> Proibido(string mensagem) => new(TipoResultado.Proibido, default, mensagem);
    public static Resultado<T> Indisponivel(string mensagem) => new(TipoResultado.Indisponivel, default, mensagem);
}

// Atalhos para quando o tipo do valor é "qualquer objeto" (mensagens, respostas montadas na hora)
public static class Resultado
{
    public static Resultado<object> Ok(object valor) => Resultado<object>.Ok(valor);
    public static Resultado<object> Invalido(string mensagem) => Resultado<object>.Invalido(mensagem);
    public static Resultado<object> NaoEncontrado(string? mensagem = null) => Resultado<object>.NaoEncontrado(mensagem);
    public static Resultado<object> Conflito(string mensagem) => Resultado<object>.Conflito(mensagem);
    public static Resultado<object> Falha(string mensagem) => Resultado<object>.Falha(mensagem);
    public static Resultado<object> InvalidoComCorpo(string mensagem, object corpo) => Resultado<object>.InvalidoComCorpo(mensagem, corpo);
    public static Resultado<object> NaoAutorizado(string? mensagem = null) => Resultado<object>.NaoAutorizado(mensagem);
    public static Resultado<object> Proibido(string mensagem) => Resultado<object>.Proibido(mensagem);
    public static Resultado<object> Indisponivel(string mensagem) => Resultado<object>.Indisponivel(mensagem);
}
