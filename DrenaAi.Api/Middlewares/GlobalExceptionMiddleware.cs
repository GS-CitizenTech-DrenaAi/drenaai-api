using System.Net;
using System.Text.Json;
using DrenaAi.Api.Exceptions;

namespace DrenaAi.Api.Middlewares;

/// <summary>
/// Middleware global responsável por interceptar exceções não tratadas e convertê-las em respostas JSON padronizadas.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GlobalExceptionMiddleware"/>.
    /// </summary>
    /// <param name="next">Próximo delegado do pipeline HTTP.</param>
    /// <param name="logger">Logger estruturado para registro seguro de exceções.</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Executa o pipeline HTTP capturando exceções de domínio, autorização, recurso não encontrado e falhas inesperadas.
    /// </summary>
    /// <param name="context">Contexto HTTP da requisição atual.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await TratarExcecaoAsync(context, ex);
        }
    }

    private async Task TratarExcecaoAsync(HttpContext context, Exception exception)
    {
        var (statusCode, mensagem) = exception switch
        {
            DomainException => (HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedException => (HttpStatusCode.Unauthorized, exception.Message),
            NotFoundException => (HttpStatusCode.NotFound, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno inesperado no servidor.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Erro inesperado ao processar requisição {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Exceção controlada ({StatusCode}) em {Method} {Path}: {Message}",
                (int)statusCode,
                context.Request.Method,
                context.Request.Path,
                mensagem);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            statusCode = (int)statusCode,
            erro = mensagem,
            timestamp = DateTime.UtcNow
        });

        await context.Response.WriteAsync(payload);
    }
}
