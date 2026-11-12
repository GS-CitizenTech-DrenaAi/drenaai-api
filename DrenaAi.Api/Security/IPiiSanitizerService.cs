namespace DrenaAi.Api.Security;

/// <summary>
/// Contrato do serviço de sanitização de dados pessoais identificáveis (PII) e informações sensíveis em textos livres.
/// </summary>
public interface IPiiSanitizerService
{
    /// <summary>
    /// Remove ou mascara dados pessoais (CPF, telefone, e-mail, senhas e termos sensíveis de saúde) de um texto.
    /// </summary>
    /// <param name="texto">Texto original enviado pelo usuário.</param>
    /// <returns>Texto sanitizado pronto para persistência e exibição segura em logs.</returns>
    string Sanitizar(string? texto);
}
