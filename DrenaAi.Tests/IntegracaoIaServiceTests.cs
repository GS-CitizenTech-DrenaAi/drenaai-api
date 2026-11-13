using System.Net;
using System.Net.Http.Json;
using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Controllers;
using DrenaAi.Api.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace DrenaAi.Tests;

/// <summary>
/// Testes unitários e de contrato para <see cref="IntegracaoIaMockService"/>, <see cref="IntegracaoIaHttpService"/> e <see cref="SeveridadeController"/> (US09).
/// </summary>
public class IntegracaoIaServiceTests
{
    /// <summary>
    /// CT09.1 [Positivo]: Deve avaliar foto válida e retornar severidade, confiança e justificativa esperadas.
    /// </summary>
    [Fact]
    public async Task AvaliarSeveridadeAsync_DeveRetornarSeveridadeEstruturada_QuandoFotoForValida()
    {
        // Arrange
        IIntegracaoIaService service = new IntegracaoIaMockService();
        var controller = new SeveridadeController(service);

        // Act
        var actionResult = await controller.Avaliar(
            new AvaliarSeveridadeRequest("bueiro_obstruido_critico.jpg"),
            CancellationToken.None);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AvaliarSeveridadeResponse>().Subject;
        response.Severidade.Should().Be(0.75);
        response.Confianca.Should().Be(0.92);
        response.Justificativa.Should().Be("Acúmulo severo de resíduos obstruindo a grade de drenagem.");
    }

    /// <summary>
    /// CT09.2 [Negativo]: Deve retornar 400 Bad Request no controller e lançar DomainException no serviço quando não houver imagem.
    /// </summary>
    [Fact]
    public async Task AvaliarSeveridade_DeveRejeitarRequisicaoSemImagem()
    {
        // Arrange
        IIntegracaoIaService service = new IntegracaoIaMockService();
        var controller = new SeveridadeController(service);

        // Act
        var actService = async () => await service.AvaliarSeveridadeAsync(null, CancellationToken.None);
        var actionResult = await controller.Avaliar(new AvaliarSeveridadeRequest(null), CancellationToken.None);

        // Assert
        await actService.Should().ThrowAsync<DomainException>();
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Deve retornar os parâmetros dos Cenários de Referência A (Vila Mariana) e B (Pinheiros) no serviço mock.
    /// </summary>
    [Fact]
    public async Task ObterPrevisaoERiscoAsync_DeveRetornarDadosDosCenariosDeReferenciaAeB()
    {
        // Arrange
        IIntegracaoIaService service = new IntegracaoIaMockService();

        // Act
        var cenarioA = await service.ObterPrevisaoERiscoAsync("Vila Mariana");
        var cenarioB = await service.ObterPrevisaoERiscoAsync("Pinheiros");

        // Assert
        cenarioA.ChuvaMm12h.Should().Be(35.0);
        cenarioA.VulnerabilidadeVia.Should().Be(0.90);

        cenarioB.ChuvaMm12h.Should().Be(0.0);
        cenarioB.VulnerabilidadeVia.Should().Be(0.20);
    }

    /// <summary>
    /// Deve realizar chamada HTTP configurável para VisionBaseUrl através de <see cref="IntegracaoIaHttpService"/>.
    /// </summary>
    [Fact]
    public async Task IntegracaoIaHttpService_DeveChamarEndpointHttpConfigurado()
    {
        // Arrange
        var respostaEsperada = new AvaliarSeveridadeResponse(0.82, 0.95, "Obstrução crítica detectada via HTTP.");
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "http://vision-ia.local:8001/api/severidade"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(respostaEsperada)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var options = Options.Create(new IaServicesSettings
        {
            VisionBaseUrl = "http://vision-ia.local:8001",
            MlFloodRiskBaseUrl = "http://ml-risco.local:8002",
            UseMocks = false
        });

        IIntegracaoIaService service = new IntegracaoIaHttpService(httpClient, options);

        // Act
        var resultado = await service.AvaliarSeveridadeAsync("foto_teste.jpg");

        // Assert
        resultado.Should().Be(respostaEsperada);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}
