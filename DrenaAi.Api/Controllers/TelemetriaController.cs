using DrenaAi.Api.Contexts.Telemetrias;
using DrenaAi.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace DrenaAi.Api.Controllers;

/// <summary>
/// Controlador para ingestão de leituras de telemetria enviadas pelos dispositivos IoT ESP32.
/// </summary>
[ApiController]
[Route("api/telemetria")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class TelemetriaController : ControllerBase
{
    private readonly ITelemetriaService _telemetriaService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TelemetriaController"/>.
    /// </summary>
    /// <param name="telemetriaService">Serviço de processamento de telemetria IoT.</param>
    public TelemetriaController(ITelemetriaService telemetriaService)
    {
        _telemetriaService = telemetriaService;
    }

    /// <summary>
    /// Recebe uma leitura de sensor de bueiro (ultrassônico e gás) e abre ocorrência automática quando crítico.
    /// </summary>
    /// <param name="request">Payload de telemetria enviado pelo ESP32.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resultado do processamento com indicação de abertura de ocorrência.</returns>
    /// <response code="202">Telemetria aceita e processada.</response>
    /// <response code="400">Leituras inválidas.</response>
    /// <response code="401">Cabeçalho X-Device-Api-Key ausente ou inválido.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TelemetriaResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TelemetriaResponse>> Receber(
        [FromBody] TelemetriaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _telemetriaService.ReceberTelemetriaAsync(request, cancellationToken);
        return Accepted(response);
    }
}
