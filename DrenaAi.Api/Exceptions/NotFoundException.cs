namespace DrenaAi.Api.Exceptions;

/// <summary>
/// Exceção lançada quando um recurso solicitado não é encontrado na base de dados.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="NotFoundException"/> com uma mensagem descritiva.
    /// </summary>
    /// <param name="message">Mensagem informando o recurso não encontrado.</param>
    public NotFoundException(string message) : base(message)
    {
    }
}
