namespace DrenaAi.Api.Exceptions;

/// <summary>
/// Exceção lançada quando uma regra de negócio ou validação de domínio é violada.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="DomainException"/> com uma mensagem descritiva.
    /// </summary>
    /// <param name="message">Mensagem explicando a violação da regra de negócio.</param>
    public DomainException(string message) : base(message)
    {
    }
}
