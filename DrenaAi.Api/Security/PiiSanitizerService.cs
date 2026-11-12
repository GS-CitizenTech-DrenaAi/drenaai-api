using System.Text.RegularExpressions;

namespace DrenaAi.Api.Security;

/// <summary>
/// Implementação de sanitização de PII (Taint Analysis) baseada em expressões regulares compiladas.
/// </summary>
public partial class PiiSanitizerService : IPiiSanitizerService
{
    /// <inheritdoc />
    public string Sanitizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var resultado = texto;

        resultado = CpfRegex().Replace(resultado, "[CPF REMOVIDO]");
        resultado = EmailRegex().Replace(resultado, "[EMAIL REMOVIDO]");
        resultado = TelefoneRegex().Replace(resultado, "[TELEFONE REMOVIDO]");
        resultado = SenhaRegex().Replace(resultado, "[SENHA REMOVIDA]");
        resultado = SaudeRegex().Replace(resultado, "[DADO DE SAUDE REMOVIDO]");

        return resultado;
    }

    [GeneratedRegex(@"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b", RegexOptions.Compiled)]
    private static partial Regex CpfRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?:\+?55\s?)?(?:\(\d{2}\)\s?|\b\d{2}\s)(?:9\d{4}|\d{4})-?\d{4}\b|\b(?:9\d{4}|\d{4})-\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex TelefoneRegex();

    [GeneratedRegex(@"\b(?:senha|password|pwd|pass)\s*(?:[:=]|[eé])\s*\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SenhaRegex();

    [GeneratedRegex(@"\b(?:prontu[aá]rio|diagn[oó]stico|cid\s*-?\s*\d+|exame\s+m[eé]dico|receita\s+m[eé]dica)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SaudeRegex();
}
