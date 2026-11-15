using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Contexts.Telemetrias;
using DrenaAi.Api.Data;
using DrenaAi.Api.Exceptions;
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

    private static (TelemetriaService telemetriaService, OcorrenciaService ocorrenciaService) CriarServicos(AppDbContext context)
    {
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

        return (telemetriaService, ocorrenciaService);
    }

    /// <summary>
    /// Deve cadastrar o sensor, gerar ocorrência automática na fila com origem SENSOR quando a telemetria for crítica
    /// e atualizar o estado do sensor no painel.
    /// </summary>
    [Fact]
    public async Task ReceberTelemetriaAsync_DeveGerarOcorrenciaSensorNaFila_QuandoLeituraForCritica()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var (telemetriaService, ocorrenciaService) = CriarServicos(context);

        var sensorCadastrado = await telemetriaService.CadastrarSensorAsync(new CadastrarSensorRequest(
            Logradouro: "Praça da Bandeira, 40",
            Bairro: "Vale do Anhangabaú",
            Latitude: -23.5493,
            Longitude: -46.6391));

        var request = new TelemetriaRequest(
            BueiroId: sensorCadastrado.Id,
            DistanciaCm: 8.5,
            GasPpm: 650.0);

        // Act
        var resposta = await telemetriaService.ReceberTelemetriaAsync(request);
        var fila = await ocorrenciaService.ObterFilaAsync();
        var sensorAtualizado = await telemetriaService.ObterSensorPorIdAsync(sensorCadastrado.Id);

        // Assert
        resposta.OcorrenciaGerada.Should().BeTrue();
        resposta.OcorrenciaId.Should().NotBeNull();
        resposta.Ipc.Should().BeGreaterThan(0);

        fila.Should().ContainSingle();
        fila[0].Id.Should().Be(resposta.OcorrenciaId!.Value);
        fila[0].Origem.Should().Be("SENSOR");
        fila[0].Tipo.Should().Be("OBSTRUCAO_BUEIRO");
        fila[0].Ipc.Should().BeGreaterThan(0);

        sensorAtualizado.Obstrucao.Should().Be(72);
        sensorAtualizado.GasDetectado.Should().BeTrue();
        sensorAtualizado.OcorrenciaId.Should().Be(resposta.OcorrenciaId);

        var entidade = await context.Ocorrencias.FirstAsync(o => o.Id == resposta.OcorrenciaId!.Value);
        entidade.FotoUrl.Should().BeNull();
        entidade.UsuarioId.Should().BeNull();
    }

    /// <summary>
    /// Deve atualizar o percentual de obstrução e manter o sensor visível na listagem mesmo quando a leitura for normal.
    /// </summary>
    [Fact]
    public async Task ReceberTelemetriaAsync_DeveAtualizarObstrucaoSemAbrirOcorrencia_QuandoLeituraForNormal()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var (telemetriaService, ocorrenciaService) = CriarServicos(context);

        var sensor = await telemetriaService.CadastrarSensorAsync(new CadastrarSensorRequest(
            Logradouro: "Rua Formosa, 95",
            Bairro: "Vale do Anhangabaú",
            Latitude: -23.5461,
            Longitude: -46.6388,
            CodigoSensor: "ESP32-FORMOSA"));

        // Act: 21 cm em bueiro de 30 cm = 30% de obstrução (abaixo do limiar crítico de 20 cm)
        var resposta = await telemetriaService.ReceberTelemetriaAsync(new TelemetriaRequest(
            BueiroId: sensor.Id,
            DistanciaCm: 21.0,
            GasPpm: 50.0));

        var sensores = await telemetriaService.ListarSensoresAsync();
        var fila = await ocorrenciaService.ObterFilaAsync();

        // Assert
        resposta.OcorrenciaGerada.Should().BeFalse();
        resposta.OcorrenciaId.Should().BeNull();
        fila.Should().BeEmpty();

        sensores.Should().ContainSingle();
        sensores[0].Id.Should().Be(sensor.Id);
        sensores[0].CodigoSensor.Should().Be("ESP32-FORMOSA");
        sensores[0].Obstrucao.Should().Be(30);
        sensores[0].GasDetectado.Should().BeFalse();
        sensores[0].OcorrenciaId.Should().BeNull();
    }

    /// <summary>
    /// Deve lançar NotFoundException ao receber leitura de um bueiro não cadastrado previamente.
    /// </summary>
    [Fact]
    public async Task ReceberTelemetriaAsync_DeveLancarNotFoundException_QuandoBueiroNaoExistir()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var (telemetriaService, _) = CriarServicos(context);
        var request = new TelemetriaRequest(
            BueiroId: Guid.NewGuid(),
            DistanciaCm: 10.0,
            GasPpm: 500.0);

        // Act
        var act = async () => await telemetriaService.ReceberTelemetriaAsync(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
