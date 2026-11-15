using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Devolve o hash bcrypt de um texto (rota pública, usada para preparar senhas à mão).
public class GerarHashQueryHandler : IQueryHandler<GerarHashQuery>
{
    public Task<Resultado<object>> HandleAsync(GerarHashQuery query) =>
        Task.FromResult(Resultado.Ok(new HashDto(BCrypt.Net.BCrypt.HashPassword(query.Senha))));
}
