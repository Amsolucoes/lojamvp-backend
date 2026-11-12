using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Shared.Api;

public static class ResultadoExtensions
{
    // Traduz o resultado de um caso de uso para a resposta HTTP. As respostas de erro mantêm o formato { erro: "..." }.
    public static IActionResult ParaResposta<T>(this Resultado<T> resultado, ControllerBase controller) => resultado.Tipo switch
    {
        TipoResultado.Ok => controller.Ok(resultado.Valor),
        TipoResultado.Invalido => controller.BadRequest(new { erro = resultado.Mensagem }),
        TipoResultado.NaoEncontrado => resultado.Mensagem is null
            ? controller.NotFound()
            : controller.NotFound(new { erro = resultado.Mensagem }),
        TipoResultado.Conflito => controller.Conflict(new { erro = resultado.Mensagem }),
        _ => controller.StatusCode(500, new { erro = resultado.Mensagem }),
    };
}
