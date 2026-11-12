namespace LojaApi.Shared.Application;

public enum TipoResultado { Ok, Invalido, NaoEncontrado, Conflito, Falha }

// Resultado de um caso de uso. A camada de aplicação devolve isto (sem saber nada de HTTP)
// e o controller traduz para a resposta certa (200, 400, 404, 409, 500) em ResultadoExtensions.
public sealed class Resultado<T>
{
    public TipoResultado Tipo { get; }
    public T? Valor { get; }
    public string? Mensagem { get; }

    private Resultado(TipoResultado tipo, T? valor, string? mensagem)
    {
        Tipo = tipo; Valor = valor; Mensagem = mensagem;
    }

    public bool Sucesso => Tipo == TipoResultado.Ok;

    public static Resultado<T> Ok(T valor) => new(TipoResultado.Ok, valor, null);
    public static Resultado<T> Invalido(string mensagem) => new(TipoResultado.Invalido, default, mensagem);
    public static Resultado<T> NaoEncontrado(string? mensagem = null) => new(TipoResultado.NaoEncontrado, default, mensagem);
    public static Resultado<T> Conflito(string mensagem) => new(TipoResultado.Conflito, default, mensagem);
    public static Resultado<T> Falha(string mensagem) => new(TipoResultado.Falha, default, mensagem);
}

// Atalhos para quando o tipo do valor é "qualquer objeto" (mensagens, respostas montadas na hora)
public static class Resultado
{
    public static Resultado<object> Ok(object valor) => Resultado<object>.Ok(valor);
    public static Resultado<object> Invalido(string mensagem) => Resultado<object>.Invalido(mensagem);
    public static Resultado<object> NaoEncontrado(string? mensagem = null) => Resultado<object>.NaoEncontrado(mensagem);
    public static Resultado<object> Conflito(string mensagem) => Resultado<object>.Conflito(mensagem);
    public static Resultado<object> Falha(string mensagem) => Resultado<object>.Falha(mensagem);
}
