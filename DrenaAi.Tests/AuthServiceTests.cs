using System.IdentityModel.Tokens.Jwt;
using DrenaAi.Api.Contexts.Usuarios;
using DrenaAi.Api.Data;
using DrenaAi.Api.Exceptions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DrenaAi.Tests;

/// <summary>
/// Testes unitários e funcionais para o serviço de autenticação <see cref="AuthService"/>.
/// </summary>
public class AuthServiceTests
{
    private static AppDbContext CriarDbContextEmMemoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static IOptions<JwtSettings> CriarJwtOptions()
    {
        return Options.Create(new JwtSettings
        {
            SecretKey = "DrenaAi_Chave_Secreta_Testes_Unitarios_FIAP_GS_2026!",
            Issuer = "DrenaAi.Api",
            Audience = "DrenaAi.Mobile",
            ExpiracaoHoras = 8
        });
    }

    /// <summary>
    /// Deve cadastrar um novo morador com dados válidos e armazenar a senha como hash.
    /// </summary>
    [Fact]
    public async Task CadastrarAsync_DeveCriarUsuarioComSenhaHasheada_QuandoDadosForemValidos()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var authService = new AuthService(context, CriarJwtOptions());
        var request = new CadastroRequest("Carlos Silva", "morador.teste@drenaai.com", "Senha@123");

        // Act
        var response = await authService.CadastrarAsync(request);

        // Assert
        response.Id.Should().NotBeEmpty();
        response.Nome.Should().Be("Carlos Silva");
        response.Email.Should().Be("morador.teste@drenaai.com");
        response.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var usuarioBanco = await context.Usuarios.FirstOrDefaultAsync(u => u.Id == response.Id);
        usuarioBanco.Should().NotBeNull();
        usuarioBanco!.SenhaHash.Should().NotBeNullOrWhiteSpace();
        usuarioBanco.SenhaHash.Should().NotBe("Senha@123");
    }

    /// <summary>
    /// Deve rejeitar cadastro quando o e-mail já estiver cadastrado.
    /// </summary>
    [Fact]
    public async Task CadastrarAsync_DeveLancarDomainException_QuandoEmailJaEstiverCadastrado()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var authService = new AuthService(context, CriarJwtOptions());
        await authService.CadastrarAsync(new CadastroRequest("Carlos Silva", "morador.teste@drenaai.com", "Senha@123"));

        var requestDuplicado = new CadastroRequest("Outro Nome", "morador.teste@drenaai.com", "Outra@123");

        // Act
        var act = async () => await authService.CadastrarAsync(requestDuplicado);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("O e-mail informado já está cadastrado.");
    }

    /// <summary>
    /// Deve autenticar morador com credenciais válidas e retornar token JWT válido.
    /// </summary>
    [Fact]
    public async Task LoginAsync_DeveRetornarTokenJwtValido_QuandoCredenciaisForemCorretas()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var authService = new AuthService(context, CriarJwtOptions());
        await authService.CadastrarAsync(new CadastroRequest("Carlos Silva", "morador.teste@drenaai.com", "Senha@123"));

        var loginRequest = new LoginRequest("morador.teste@drenaai.com", "Senha@123");

        // Act
        var response = await authService.LoginAsync(loginRequest);

        // Assert
        response.Token.Should().NotBeNullOrWhiteSpace();
        response.Nome.Should().Be("Carlos Silva");
        response.Email.Should().Be("morador.teste@drenaai.com");

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(response.Token).Should().BeTrue();
        var jwt = handler.ReadJwtToken(response.Token);
        jwt.Issuer.Should().Be("DrenaAi.Api");
        jwt.Audiences.Should().Contain("DrenaAi.Mobile");
    }

    /// <summary>
    /// Deve lançar UnauthorizedException com mensagem genérica quando a senha estiver incorreta.
    /// </summary>
    [Fact]
    public async Task LoginAsync_DeveLancarUnauthorizedException_QuandoSenhaForIncorreta()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var authService = new AuthService(context, CriarJwtOptions());
        await authService.CadastrarAsync(new CadastroRequest("Carlos Silva", "morador.teste@drenaai.com", "Senha@123"));

        var loginRequest = new LoginRequest("morador.teste@drenaai.com", "SenhaErrada999");

        // Act
        var act = async () => await authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Credenciais inválidas.");
    }
}
