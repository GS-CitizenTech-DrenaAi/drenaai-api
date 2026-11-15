using DrenaAi.Api.Contexts.Telemetrias;
using DrenaAi.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace DrenaAi.Api.Controllers;

/// <summary>
/// Controlador para cadastro de bueiros monitorados, consulta do painel de sensores e ingestão de telemetria ESP32.
/// </summary>
[ApiController]
[Route("api/telemetria")]
public class TelemetriaController : ControllerBase
{
    private readonly ITelemetriaService _telemetriaService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TelemetriaController"/>.
    /// </summary>
    /// <param name="telemetriaService">Serviço de gerenciamento e ingestão de telemetria IoT.</param>
    public TelemetriaController(ITelemetriaService telemetriaService)
    {
        _telemetriaService = telemetriaService;
    }

    /// <summary>
    /// Cadastra um novo bueiro monitorado por sensor IoT no banco de dados com localização real e leitura inicial opcional.
    /// </summary>
    /// <param name="request">Dados de endereço, coordenadas e leitura inicial opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Dados do bueiro cadastrado incluindo o identificador único gerado pelo servidor.</returns>
    /// <response code="201">Sensor cadastrado com sucesso.</response>
    /// <response code="400">Dados de localização ou leitura inválidos.</response>
    /// <response code="401">Cabeçalho X-Device-Api-Key ausente ou inválido.</response>
    [HttpPost("sensores")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    [ProducesResponseType(typeof(SensorBueiroResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SensorBueiroResponse>> CadastrarSensor(
        [FromBody] CadastrarSensorRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _telemetriaService.CadastrarSensorAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterSensorPorId), new { bueiroId = response.Id }, response);
    }

    /// <summary>
    /// Lista todos os bueiros monitorados por sensores IoT com localização, percentual de obstrução e última leitura.
    /// </summary>
    /// <param name="bairro">Filtro opcional pelo nome do bairro.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Lista de bueiros ordenada por maior obstrução.</returns>
    /// <response code="200">Lista de sensores retornada com sucesso.</response>
    [HttpGet("sensores")]
    [ProducesResponseType(typeof(IReadOnlyList<SensorBueiroResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SensorBueiroResponse>>> ListarSensores(
        [FromQuery] string? bairro,
        CancellationToken cancellationToken)
    {
        var sensores = await _telemetriaService.ListarSensoresAsync(bairro, cancellationToken);
        return Ok(sensores);
    }

    /// <summary>
    /// Consulta os detalhes e a última leitura de um bueiro monitorado pelo seu identificador único.
    /// </summary>
    /// <param name="bueiroId">Identificador único do bueiro.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Estado atual do sensor e sua localização.</returns>
    /// <response code="200">Sensor encontrado.</response>
    /// <response code="404">Sensor não encontrado.</response>
    [HttpGet("sensores/{bueiroId:guid}")]
    [ProducesResponseType(typeof(SensorBueiroResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SensorBueiroResponse>> ObterSensorPorId(
        [FromRoute] Guid bueiroId,
        CancellationToken cancellationToken)
    {
        var sensor = await _telemetriaService.ObterSensorPorIdAsync(bueiroId, cancellationToken);
        return Ok(sensor);
    }

    /// <summary>
    /// Recebe uma leitura recorrente de sensor de bueiro cadastrado (ultrassônico e gás), atualiza o painel e abre ocorrência automática quando crítico.
    /// </summary>
    /// <param name="request">Payload de telemetria enviado pelo ESP32.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resultado do processamento com indicação de abertura de ocorrência.</returns>
    /// <response code="202">Telemetria aceita e processada.</response>
    /// <response code="400">Leituras inválidas.</response>
    /// <response code="401">Cabeçalho X-Device-Api-Key ausente ou inválido.</response>
    /// <response code="404">Bueiro informado não cadastrado no sistema.</response>
    [HttpPost]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    [ProducesResponseType(typeof(TelemetriaResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TelemetriaResponse>> Receber(
        [FromBody] TelemetriaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _telemetriaService.ReceberTelemetriaAsync(request, cancellationToken);
        return Accepted(response);
    }
}
