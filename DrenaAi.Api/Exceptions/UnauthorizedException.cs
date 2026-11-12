namespace DrenaAi.Api.Exceptions;

/// <summary>
/// Exceção lançada quando a autenticação falha devido a credenciais inválidas ou ausência de autorização.
/// </summary>
public class UnauthorizedException : Exception
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="UnauthorizedException"/> com uma mensagem descritiva.
    /// </summary>
    /// <param name="message">Mensagem de erro de autenticação.</param>
    public UnauthorizedException(string message) : base(message)
    {
    }
}
