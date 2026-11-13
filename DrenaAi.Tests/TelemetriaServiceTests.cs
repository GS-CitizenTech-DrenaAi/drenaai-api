using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Contexts.Telemetrias;
using DrenaAi.Api.Data;
using DrenaAi.Api.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DrenaAi.Tests;

/// <summary>
/// Testes unitários e funcionais para <see cref="TelemetriaService"/>.
/// </summary>
public class TelemetriaServiceTests
{
    private static AppDbContext CriarDbContextEmMemoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Deve gerar ocorrência automática na fila com origem SENSOR quando a telemetria for crítica.
    /// </summary>
    [Fact]
    public async Task ReceberTelemetriaAsync_DeveGerarOcorrenciaSensorNaFila_QuandoLeituraForCritica()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var calculadora = new CalculadoraIpcService(Options.Create(new IpcSettings
        {
            PesoSeveridade = 0.4,
            PesoChuva = 0.3,
            PesoVulnerabilidade = 0.3,
            TetoChuvaMm = 50.0
        }));
        var integracaoIa = new IntegracaoIaMockService();

        var telemetriaService = new TelemetriaService(
            context,
            integracaoIa,
            calculadora,
            NullLogger<TelemetriaService>.Instance);

        var ocorrenciaService = new OcorrenciaService(
            context,
            new PiiSanitizerService(),
            integracaoIa,
            calculadora,
            NullLogger<OcorrenciaService>.Instance);

        var request = new TelemetriaRequest(
            SensorId: "ESP32-SP-01",
            BueiroId: "BUEIRO-VM-01",
            DistanciaCm: 8.5,
            GasPpm: 650.0,
            Timestamp: DateTime.UtcNow);

        // Act
        var resposta = await telemetriaService.ReceberTelemetriaAsync(request);
        var fila = await ocorrenciaService.ObterFilaAsync();

        // Assert
        resposta.OcorrenciaGerada.Should().BeTrue();
        resposta.OcorrenciaId.Should().NotBeNull();
        resposta.Ipc.Should().BeGreaterThan(0);

        fila.Should().ContainSingle();
        fila[0].Id.Should().Be(resposta.OcorrenciaId!.Value);
        fila[0].Origem.Should().Be("SENSOR");
        fila[0].Tipo.Should().Be("OBSTRUCAO_BUEIRO");
        fila[0].Ipc.Should().BeGreaterThan(0);

        var entidade = await context.Ocorrencias.FirstAsync(o => o.Id == resposta.OcorrenciaId!.Value);
        entidade.FotoUrl.Should().BeNull();
        entidade.UsuarioId.Should().BeNull();
    }
}
