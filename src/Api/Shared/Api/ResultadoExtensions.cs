using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Shared.Api;

public static class ResultadoExtensions
{
    // Traduz o resultado de um caso de uso para a resposta HTTP (200, 400, 401, 403, 404, 409, 500, 503). As respostas de erro mantêm o formato { erro: "..." }.
    public static IActionResult ParaResposta<T>(this Resultado<T> resultado, ControllerBase controller) => resultado.Tipo switch
    {
        TipoResultado.Ok => controller.Ok(resultado.Valor),
        TipoResultado.Invalido => controller.BadRequest(resultado.Corpo ?? new { erro = resultado.Mensagem }),
        TipoResultado.NaoEncontrado => resultado.Mensagem is null
            ? controller.NotFound()
            : controller.NotFound(new { erro = resultado.Mensagem }),
        TipoResultado.Conflito => controller.Conflict(new { erro = resultado.Mensagem }),
        TipoResultado.NaoAutorizado => resultado.Mensagem is null
            ? controller.Unauthorized()
            : controller.Unauthorized(new { erro = resultado.Mensagem }),
        TipoResultado.Proibido => controller.StatusCode(403, new { erro = resultado.Mensagem }),
        TipoResultado.Indisponivel => controller.StatusCode(503, new { erro = resultado.Mensagem }),
        _ => controller.StatusCode(500, new { erro = resultado.Mensagem }),
    };
}
