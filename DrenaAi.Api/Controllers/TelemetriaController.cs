using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Contexts.Telemetrias;
using DrenaAi.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DrenaAi.Api.Controllers;

/// <summary>
/// Controlador para ingestão de leituras de telemetria enviadas pelos dispositivos IoT ESP32.
/// </summary>
[ApiController]
[Route("api/telemetria")]
public class TelemetriaController : ControllerBase
{
    private readonly ITelemetriaService _telemetriaService;
    private readonly IWebHostEnvironment _environment;
    private readonly IaServicesSettings _iaSettings;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TelemetriaController"/>.
    /// </summary>
    /// <param name="telemetriaService">Serviço de processamento de telemetria IoT.</param>
    /// <param name="environment">Informações do ambiente de execução atual.</param>
    /// <param name="iaOptions">Configurações de modo mock da aplicação.</param>
    public TelemetriaController(
        ITelemetriaService telemetriaService,
        IWebHostEnvironment environment,
        IOptions<IaServicesSettings> iaOptions)
    {
        _telemetriaService = telemetriaService;
        _environment = environment;
        _iaSettings = iaOptions.Value;
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
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
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

    /// <summary>
    /// Gera e processa uma leitura de telemetria mockada em ambiente de desenvolvimento/testes,
    /// dispensando a integração física com o dispositivo ESP32.
    /// </summary>
    /// <param name="bueiroId">Identificador opcional do bueiro para simulação (padrão: BUEIRO-VM-01).</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resultado do processamento da telemetria simulada.</returns>
    /// <response code="202">Telemetria mockada gerada e processada com sucesso.</response>
    /// <response code="404">Endpoint disponível apenas em ambiente de desenvolvimento ou modo mock.</response>
    [HttpPost("simular")]
    [ProducesResponseType(typeof(TelemetriaResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TelemetriaResponse>> Simular(
        [FromQuery] string? bueiroId = "BUEIRO-VM-01",
        CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment() && !_iaSettings.UseMocks)
            return NotFound(new { erro = "Simulação de telemetria disponível apenas em ambiente de testes/desenvolvimento." });

        var leituraMockada = new TelemetriaRequest(
            SensorId: "ESP32-MOCK-01",
            BueiroId: string.IsNullOrWhiteSpace(bueiroId) ? "BUEIRO-VM-01" : bueiroId.Trim(),
            DistanciaCm: 8.5,
            GasPpm: 650.0,
            Timestamp: DateTime.UtcNow);

        var response = await _telemetriaService.ReceberTelemetriaAsync(leituraMockada, cancellationToken);
        return Accepted(response);
    }
}
