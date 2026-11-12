namespace DrenaAi.Api.Contexts.Usuarios;

/// <summary>
/// Configurações para geração e validação de tokens JWT da aplicação.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Chave secreta simétrica utilizada para assinar os tokens JWT (mínimo 32 caracteres).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Emissor (Issuer) do token JWT.
    /// </summary>
    public string Issuer { get; set; } = "DrenaAi.Api";

    /// <summary>
    /// Público-alvo (Audience) autorizado a consumir o token JWT.
    /// </summary>
    public string Audience { get; set; } = "DrenaAi.Mobile";

    /// <summary>
    /// Tempo de validade do token JWT em horas.
    /// </summary>
    public int ExpiracaoHoras { get; set; } = 8;
}
