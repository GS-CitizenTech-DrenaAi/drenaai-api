namespace DrenaAi.Api.Contexts.Usuarios;

/// <summary>
/// Contrato do serviço de autenticação e cadastro de moradores.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Realiza o cadastro de um novo morador validando unicidade de e-mail e armazenando a senha como hash.
    /// </summary>
    /// <param name="request">Dados mínimos de cadastro do morador.</param>
    /// <returns>Dados do usuário criado, incluindo identificador único e data de criação.</returns>
    Task<CadastroResponse> CadastrarAsync(CadastroRequest request);

    /// <summary>
    /// Autentica um morador por e-mail e senha e emite um token JWT válido.
    /// </summary>
    /// <param name="request">Credenciais de acesso do morador.</param>
    /// <returns>Token JWT gerado e dados básicos do usuário autenticado.</returns>
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
