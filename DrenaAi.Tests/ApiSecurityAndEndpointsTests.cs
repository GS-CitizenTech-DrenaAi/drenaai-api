using System.Net;
using System.Net.Http.Json;
using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Contexts.Telemetrias;
using DrenaAi.Api.Data;
using DrenaAi.Api.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DrenaAi.Tests;

/// <summary>
/// Testes de integração HTTP validando autenticação JWT, ApiKey ESP32,
/// validação de previsão e geração do Swagger/OpenAPI.
/// </summary>
public class ApiSecurityAndEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Inicializa a suíte configurando o host de testes com banco em memória.
    /// </summary>
    /// <param name="factory">Fábrica padrão do servidor de testes ASP.NET Core.</param>
    public ApiSecurityAndEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var dbName = $"DrenaAiTestDb_{Guid.NewGuid()}";
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                                d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>))
                    .ToList();

                foreach (var descriptor in descriptors)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });
    }

    /// <summary>
    /// Deve retornar 401 Unauthorized ao tentar registrar ocorrência sem token JWT.
    /// </summary>
    [Fact]
    public async Task PostOcorrencias_DeveRetornar401Unauthorized_QuandoTokenJwtEstiverAusente()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new CriarOcorrenciaRequest(
            "BUEIRO_ENTUPIDO",
            "Bueiro entupido na rua",
            "foto.jpg",
            -23.589,
            -46.634,
            "Vila Mariana");

        // Act
        var response = await client.PostAsJsonAsync("/api/ocorrencias", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Deve retornar 401 Unauthorized quando o cabeçalho X-Device-Api-Key for inválido ou ausente.
    /// </summary>
    [Fact]
    public async Task PostTelemetria_DeveRetornar401Unauthorized_QuandoApiKeyForInvalidaOuAusente()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new TelemetriaRequest("ESP32-SP-01", "BUEIRO-VM-01", 8.5, 650.0, DateTime.UtcNow);

        // Act 1: Sem cabeçalho
        var semHeader = await client.PostAsJsonAsync("/api/telemetria", payload);

        // Act 2: Chave inválida
        var requestComChaveFalsa = new HttpRequestMessage(HttpMethod.Post, "/api/telemetria")
        {
            Content = JsonContent.Create(payload)
        };
        requestComChaveFalsa.Headers.Add(ApiKeyAuthFilter.ApiKeyHeaderName, "chave-falsa-999");
        var chaveInvalida = await client.SendAsync(requestComChaveFalsa);

        // Assert
        semHeader.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        chaveInvalida.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Deve retornar 202 Accepted quando X-Device-Api-Key válida for informada.
    /// </summary>
    [Fact]
    public async Task PostTelemetria_DeveRetornar202Accepted_QuandoApiKeyForValida()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new TelemetriaRequest("ESP32-SP-01", "BUEIRO-VM-01", 8.5, 650.0, DateTime.UtcNow);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/telemetria")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add(ApiKeyAuthFilter.ApiKeyHeaderName, "ESP32-DRENAAI-BUEIRO-KEY-2026");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    /// <summary>
    /// Deve retornar 400 Bad Request ao consultar previsão sem informar o bairro.
    /// </summary>
    [Fact]
    public async Task GetPrevisao_DeveRetornar400BadRequest_QuandoBairroEstiverVazio()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/previsao?bairro=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Deve expor o documento Swagger JSON contendo todas as 10 rotas e os esquemas Bearer e DeviceApiKey.
    /// </summary>
    [Fact]
    public async Task SwaggerJson_DeveConterTodasAs10RotasEEsquemasDeSeguranca()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain("/api/auth/cadastro");
        json.Should().Contain("/api/auth/login");
        json.Should().Contain("/api/ocorrencias");
        json.Should().Contain("/api/ocorrencias/{id}");
        json.Should().Contain("/api/telemetria");
        json.Should().Contain("/api/previsao");
        json.Should().Contain("/api/severidade");
        json.Should().Contain("/api/fila");
        json.Should().Contain("/api/fila/priorizar");
        json.Should().Contain("Bearer");
        json.Should().Contain("DeviceApiKey");
    }
}
