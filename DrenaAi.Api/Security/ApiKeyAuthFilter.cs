using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace DrenaAi.Api.Security;

/// <summary>
/// Filtro de autenticação que valida o cabeçalho <c>X-Device-Api-Key</c> enviado pelos dispositivos IoT ESP32.
/// </summary>
public class ApiKeyAuthFilter : IAsyncActionFilter
{
    /// <summary>
    /// Nome do cabeçalho HTTP que transporta a chave de autenticação do dispositivo.
    /// </summary>
    public const string ApiKeyHeaderName = "X-Device-Api-Key";

    private readonly DeviceAuthSettings _settings;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ApiKeyAuthFilter"/>.
    /// </summary>
    /// <param name="options">Opções de autenticação de dispositivo injetadas via <see cref="IOptions{DeviceAuthSettings}"/>.</param>
    public ApiKeyAuthFilter(IOptions<DeviceAuthSettings> options)
    {
        _settings = options.Value;
    }

    /// <inheritdoc />
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey.StartsWith('<'))
        {
            context.Result = new UnauthorizedObjectResult(new { erro = "Chave de autenticação de dispositivo não configurada." });
            return Task.CompletedTask;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var chaveRecebida) ||
            string.IsNullOrWhiteSpace(chaveRecebida))
        {
            context.Result = new UnauthorizedObjectResult(new { erro = "Cabeçalho X-Device-Api-Key ausente." });
            return Task.CompletedTask;
        }

        var bytesEsperados = Encoding.UTF8.GetBytes(_settings.ApiKey);
        var bytesRecebidos = Encoding.UTF8.GetBytes(chaveRecebida.ToString());

        if (bytesEsperados.Length != bytesRecebidos.Length ||
            !CryptographicOperations.FixedTimeEquals(bytesEsperados, bytesRecebidos))
        {
            context.Result = new UnauthorizedObjectResult(new { erro = "X-Device-Api-Key inválida." });
            return Task.CompletedTask;
        }

        return next();
    }
}
