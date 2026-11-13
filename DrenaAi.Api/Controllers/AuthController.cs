using DrenaAi.Api.Contexts.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrenaAi.Api.Controllers;

/// <summary>
/// Controlador responsável pelo cadastro de moradores e emissão de tokens JWT de autenticação.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AuthController"/>.
    /// </summary>
    /// <param name="authService">Serviço de autenticação e cadastro de usuários.</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Realiza o cadastro de um novo morador com coleta mínima de dados.
    /// </summary>
    /// <param name="request">Dados de nome, e-mail e senha do morador.</param>
    /// <returns>Identificador único e dados básicos do morador cadastrado.</returns>
    /// <response code="201">Morador cadastrado com sucesso.</response>
    /// <response code="400">Dados inválidos ou e-mail já cadastrado.</response>
    [HttpPost("cadastro")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CadastroResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CadastroResponse>> Cadastrar([FromBody] CadastroRequest request)
    {
        var response = await _authService.CadastrarAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Autentica um morador cadastrado e emite um token JWT Bearer.
    /// </summary>
    /// <param name="request">Credenciais de e-mail e senha.</param>
    /// <returns>Token JWT e dados básicos do usuário autenticado.</returns>
    /// <response code="200">Autenticação realizada com sucesso.</response>
    /// <response code="401">Credenciais inválidas.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }
}
