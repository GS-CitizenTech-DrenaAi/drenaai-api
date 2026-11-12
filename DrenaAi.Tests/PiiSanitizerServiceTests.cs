using DrenaAi.Api.Security;
using FluentAssertions;

namespace DrenaAi.Tests;

/// <summary>
/// Testes unitários para o serviço de sanitização de PII <see cref="PiiSanitizerService"/> (US03-CA02).
/// </summary>
public class PiiSanitizerServiceTests
{
    private readonly PiiSanitizerService _sanitizer = new();

    /// <summary>
    /// CT03.1 [Positivo]: Deve mascarar CPF e telefone na descrição da ocorrência antes de persistir ou logar.
    /// </summary>
    [Fact]
    public void Sanitizar_DeveMascararCpfETelefone_NaDescricaoDaOcorrencia()
    {
        // Arrange
        const string descricaoOriginal = "Bueiro cheio na minha porta, meu CPF é 123.456.789-00 e cel (11) 98888-7777";

        // Act
        var sanitizada = _sanitizer.Sanitizar(descricaoOriginal);

        // Assert
        sanitizada.Should().Contain("[CPF REMOVIDO]");
        sanitizada.Should().Contain("[TELEFONE REMOVIDO]");
        sanitizada.Should().NotContain("123.456.789-00");
        sanitizada.Should().NotContain("98888-7777");
    }

    /// <summary>
    /// Deve mascarar e-mail, senha e termos sensíveis de saúde presentes no texto livre.
    /// </summary>
    [Fact]
    public void Sanitizar_DeveMascararEmailSenhaETermosDeSaude()
    {
        // Arrange
        const string texto = "Contato: joao.silva@teste.com, senha: MinhaSenha123, prontuário médico anexado.";

        // Act
        var sanitizada = _sanitizer.Sanitizar(texto);

        // Assert
        sanitizada.Should().Contain("[EMAIL REMOVIDO]");
        sanitizada.Should().Contain("[SENHA REMOVIDA]");
        sanitizada.Should().Contain("[DADO DE SAUDE REMOVIDO]");
        sanitizada.Should().NotContain("joao.silva@teste.com");
        sanitizada.Should().NotContain("MinhaSenha123");
    }
}
