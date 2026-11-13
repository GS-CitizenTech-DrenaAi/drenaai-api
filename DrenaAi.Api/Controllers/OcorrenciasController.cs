using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrenaAi.Api.Controllers;

/// <summary>
/// Controlador protegido por JWT para registro e consulta de ocorrências pelo aplicativo móvel.
/// </summary>
[ApiController]
[Route("api/ocorrencias")]
[Authorize]
public class OcorrenciasController : ControllerBase
{
    private readonly IOcorrenciaService _ocorrenciaService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OcorrenciasController"/>.
    /// </summary>
    /// <param name="ocorrenciaService">Serviço de gerenciamento de ocorrências.</param>
    public OcorrenciasController(IOcorrenciaService ocorrenciaService)
    {
        _ocorrenciaService = ocorrenciaService;
    }

    /// <summary>
    /// Registra uma nova ocorrência reportada por um morador autenticado.
    /// </summary>
    /// <param name="request">Dados da denúncia incluindo tipo, descrição, foto e coordenadas GPS.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Ocorrência criada com severidade, IPC e descrição sanitizada.</returns>
    /// <response code="201">Ocorrência registrada com sucesso.</response>
    /// <response code="400">Payload inválido ou erro de validação.</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CriarOcorrenciaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CriarOcorrenciaResponse>> Criar(
        [FromBody] CriarOcorrenciaRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioIdAutenticado();
        var response = await _ocorrenciaService.CriarAsync(request, usuarioId, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
    }

    /// <summary>
    /// Lista ocorrências filtradas opcionalmente por bairro e status.
    /// </summary>
    /// <param name="bairro">Filtro opcional pelo nome do bairro.</param>
    /// <param name="status">Filtro opcional pelo status atual da ocorrência.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Lista de ocorrências ordenada por prioridade.</returns>
    /// <response code="200">Lista retornada com sucesso.</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OcorrenciaResumoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<OcorrenciaResumoResponse>>> Listar(
        [FromQuery] string? bairro,
        [FromQuery] StatusOcorrencia? status,
        CancellationToken cancellationToken)
    {
        var lista = await _ocorrenciaService.ListarAsync(bairro, status, cancellationToken);
        return Ok(lista);
    }

    /// <summary>
    /// Consulta os detalhes completos de uma ocorrência e sua linha do tempo de status.
    /// </summary>
    /// <param name="id">Identificador único da ocorrência.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Detalhes da ocorrência com histórico cronológico de status.</returns>
    /// <response code="200">Ocorrência encontrada.</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    /// <response code="404">Ocorrência não encontrada.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OcorrenciaDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OcorrenciaDetalheResponse>> ObterPorId(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var detalhe = await _ocorrenciaService.ObterPorIdAsync(id, cancellationToken);
        return Ok(detalhe);
    }

    private Guid ObterUsuarioIdAutenticado()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sub, out var usuarioId))
            throw new UnauthorizedException("Token JWT inválido ou sem identificador de usuário.");

        return usuarioId;
    }
}
