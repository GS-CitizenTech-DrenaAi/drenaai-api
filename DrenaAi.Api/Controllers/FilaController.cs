using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Contexts.Priorizacao;
using Microsoft.AspNetCore.Mvc;

namespace DrenaAi.Api.Controllers;

/// <summary>
/// Controlador responsável pela consulta da fila de coleta priorizada por IPC, webhook de reordenação e previsão local.
/// </summary>
[ApiController]
[Route("api/fila")]
public class FilaController : ControllerBase
{
    private readonly IOcorrenciaService _ocorrenciaService;
    private readonly IIntegracaoIaService _integracaoIaService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="FilaController"/>.
    /// </summary>
    /// <param name="ocorrenciaService">Serviço de gerenciamento de ocorrências e fila.</param>
    /// <param name="integracaoIaService">Serviço de integração para consulta de previsão pluviométrica.</param>
    public FilaController(
        IOcorrenciaService ocorrenciaService,
        IIntegracaoIaService integracaoIaService)
    {
        _ocorrenciaService = ocorrenciaService;
        _integracaoIaService = integracaoIaService;
    }

    /// <summary>
    /// Consulta a fila de coleta ordenada por IPC decrescente e data de criação crescente.
    /// </summary>
    /// <param name="bairro">Filtro opcional pelo nome do bairro.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Lista ordenada de ocorrências pendentes na fila.</returns>
    /// <response code="200">Fila retornada com sucesso.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OcorrenciaResumoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OcorrenciaResumoResponse>>> ObterFila(
        [FromQuery] string? bairro,
        CancellationToken cancellationToken)
    {
        var fila = await _ocorrenciaService.ObterFilaAsync(bairro, cancellationToken);
        return Ok(fila);
    }

    /// <summary>
    /// Aciona o webhook de priorização recalculando o IPC de uma ocorrência e reordenando a fila.
    /// </summary>
    /// <param name="request">Payload contendo o identificador da ocorrência a ser priorizada.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Novo IPC calculado, confirmação de notificação e fila atualizada.</returns>
    /// <response code="200">Fila reordenada com sucesso.</response>
    /// <response code="404">Ocorrência não encontrada.</response>
    [HttpPost("priorizar")]
    [ProducesResponseType(typeof(PriorizarFilaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriorizarFilaResponse>> Priorizar(
        [FromBody] PriorizarFilaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _ocorrenciaService.PriorizarFilaAsync(request.OcorrenciaId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Consulta a previsão de chuva e o índice de risco de alagamento para o bairro informado.
    /// </summary>
    /// <param name="bairro">Nome do bairro obrigatório para consulta.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Previsão de chuva (12h e 24h), risco de alagamento e vulnerabilidade da via.</returns>
    /// <response code="200">Previsão retornada com sucesso.</response>
    /// <response code="400">Parâmetro bairro ausente ou vazio.</response>
    [HttpGet("/api/previsao")]
    [ProducesResponseType(typeof(PrevisaoRiscoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PrevisaoRiscoResponse>> ObterPrevisao(
        [FromQuery] string? bairro,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bairro))
            return BadRequest(new { erro = "O parâmetro 'bairro' é obrigatório." });

        var previsao = await _integracaoIaService.ObterPrevisaoERiscoAsync(bairro, cancellationToken);
        return Ok(previsao);
    }
}
