using System.Net;

namespace LojaApi.src.Middleware;

// Captura qualquer exceção não tratada que escape dos controllers, loga o
// stack trace completo (visível no Railway) e devolve uma resposta JSON
// limpa e genérica pro cliente — sem vazar detalhes técnicos, mas também
// sem deixar o front sem nenhuma indicação de que algo deu errado.
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado em {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                // A resposta já começou a ser enviada — não dá mais pra reescrevê-la.
                throw;
            }

            // Não usa Response.Clear() de propósito: isso apagaria os headers de
            // CORS que o middleware de CORS já aplicou antes de chegarmos aqui.
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                erro = "Ocorreu um erro ao processar sua solicitação. Tente novamente em instantes.",
            });
        }
    }
}