namespace LojaApi.Shared.Application;

// Um handler executa UM caso de uso. Command muda dados; Query só lê. Sem biblioteca de mediador: o controller recebe
// o handler que precisa pela injeção de dependência e chama HandleAsync.
public interface ICommandHandler<in TCommand>
{
    Task<Resultado<object>> HandleAsync(TCommand command);
}

public interface IQueryHandler<in TQuery>
{
    Task<Resultado<object>> HandleAsync(TQuery query);
}
