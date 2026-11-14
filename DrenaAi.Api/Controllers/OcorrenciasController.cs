using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrenaAi.Api.Controllers;

/// <summary>
/// Controlador protegido por JWT para registro, upload de foto e consulta de ocorrências pelo aplicativo móvel.
/// </summary>
[ApiController]
[Route("api/ocorrencias")]
[Authorize]
public class OcorrenciasController : ControllerBase
{
    private static readonly HashSet<string> ExtensoesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly IOcorrenciaService _ocorrenciaService;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OcorrenciasController"/>.
    /// </summary>
    /// <param name="ocorrenciaService">Serviço de gerenciamento de ocorrências.</param>
    /// <param name="environment">Ambiente de hospedagem web para armazenamento de fotos em wwwroot/uploads.</param>
    public OcorrenciasController(
        IOcorrenciaService ocorrenciaService,
        IWebHostEnvironment environment)
    {
        _ocorrenciaService = ocorrenciaService;
        _environment = environment;
    }

    /// <summary>
    /// Realiza o upload físico da foto tirada pelo morador na denúncia e retorna a URL pública de armazenamento.
    /// </summary>
    /// <param name="arquivo">Arquivo de imagem capturado pela câmera do aplicativo.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>URL HTTP pública da imagem salva no servidor.</returns>
    /// <response code="200">Imagem armazenada com sucesso.</response>
    /// <response code="400">Arquivo ausente, vazio ou com formato não suportado.</response>
    /// <response code="401">Token JWT ausente ou inválido.</response>
    [HttpPost("foto")]
    [ProducesResponseType(typeof(UploadFotoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UploadFotoResponse>> UploadFoto(
        IFormFile? arquivo,
        CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
            throw new DomainException("A foto da denúncia é obrigatória.");

        var extensao = Path.GetExtension(arquivo.FileName);
        if (string.IsNullOrWhiteSpace(extensao) || !ExtensoesPermitidas.Contains(extensao))
            extensao = ".jpg";

        var pastaUploads = ObterPastaUploads();
        var nomeArquivo = $"oc-{Guid.NewGuid():N}{extensao.ToLowerInvariant()}";
        var caminhoCompleto = Path.Combine(pastaUploads, nomeArquivo);

        await using (var stream = System.IO.File.Create(caminhoCompleto))
        {
            await arquivo.CopyToAsync(stream, cancellationToken);
        }

        var fotoUrl = $"{Request.Scheme}://{Request.Host}/uploads/{nomeArquivo}";
        return Ok(new UploadFotoResponse(fotoUrl));
    }

    /// <summary>
    /// Registra uma nova ocorrência reportada por um morador autenticado.
    /// Caso a foto venha em formato Base64 Data URI, salva o arquivo em wwwroot/uploads e persiste a URL pública curta no Oracle.
    /// </summary>
    /// <param name="request">Dados da denúncia incluindo tipo, descrição, foto e Value Object de endereço.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Ocorrência criada com severidade, IPC, endereço e descrição sanitizada.</returns>
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
        var fotoUrlNormalizada = await MaterializarFotoUrlSeBase64Async(request.FotoUrl, cancellationToken);
        var requestNormalizado = request with { FotoUrl = fotoUrlNormalizada };

        var response = await _ocorrenciaService.CriarAsync(requestNormalizado, usuarioId, cancellationToken);
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
    /// <returns>Detalhes da ocorrência com histórico cronológico de status e Value Object de endereço.</returns>
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

    private async Task<string?> MaterializarFotoUrlSeBase64Async(string? fotoInput, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fotoInput))
            return null;

        var texto = fotoInput.Trim();
        if (texto.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var indiceVirgula = texto.IndexOf(',');
            if (indiceVirgula > 0 && indiceVirgula < texto.Length - 1)
            {
                var base64Puro = texto[(indiceVirgula + 1)..];
                var bytes = Convert.FromBase64String(base64Puro);
                var pastaUploads = ObterPastaUploads();
                var nomeArquivo = $"oc-{Guid.NewGuid():N}.jpg";
                var caminhoCompleto = Path.Combine(pastaUploads, nomeArquivo);
                await System.IO.File.WriteAllBytesAsync(caminhoCompleto, bytes, cancellationToken);
                return $"{Request.Scheme}://{Request.Host}/uploads/{nomeArquivo}";
            }
        }

        // Caso seja um caminho local residual do dispositivo (file:///...) maior ou igual ao limite,
        // ou referência local, converte em identificador curto seguro para o banco Oracle (NVARCHAR2(300)).
        if (texto.Length > 280)
            return texto[..280];

        return texto;
    }

    private string ObterPastaUploads()
    {
        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        var pastaUploads = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(pastaUploads);
        return pastaUploads;
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
