using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DrenaAi.Api.Controllers;

/// <summary>
/// Controlador responsável pelo contrato de avaliação de severidade de ocorrências via visão computacional.
/// </summary>
[ApiController]
[Route("api/severidade")]
public class SeveridadeController : ControllerBase
{
    private readonly IIntegracaoIaService _integracaoIaService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SeveridadeController"/>.
    /// </summary>
    /// <param name="integracaoIaService">Serviço de integração com os modelos de IA.</param>
    public SeveridadeController(IIntegracaoIaService integracaoIaService)
    {
        _integracaoIaService = integracaoIaService;
    }

    /// <summary>
    /// Avalia a severidade de obstrução a partir da referência de uma imagem enviada.
    /// </summary>
    /// <param name="request">Payload contendo a referência ou URL da foto.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Severidade normalizada, nível de confiança e justificativa técnica.</returns>
    /// <response code="200">Avaliação realizada com sucesso.</response>
    /// <response code="400">Nenhuma foto ou referência válida foi informada.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AvaliarSeveridadeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvaliarSeveridadeResponse>> Avaliar(
        [FromBody] AvaliarSeveridadeRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Foto))
            return BadRequest(new { erro = "A referência da foto é obrigatória." });

        try
        {
            var resultado = await _integracaoIaService.AvaliarSeveridadeAsync(request.Foto, cancellationToken);
            return Ok(resultado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
