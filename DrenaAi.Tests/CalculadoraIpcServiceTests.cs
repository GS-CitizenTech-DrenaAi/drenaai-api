using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DrenaAi.Tests;

/// <summary>
/// Testes unitários para o serviço de cálculo do Índice de Prioridade de Coleta <see cref="CalculadoraIpcService"/>.
/// </summary>
public class CalculadoraIpcServiceTests
{
    private static IOptions<IpcSettings> CriarOpcoesPadrao()
    {
        return Options.Create(new IpcSettings
        {
            PesoSeveridade = 0.4,
            PesoChuva = 0.3,
            PesoVulnerabilidade = 0.3,
            TetoChuvaMm = 50.0
        });
    }

    /// <summary>
    /// Deve calcular exatamente 0.26 para a Ocorrência B e 0.78 para a Ocorrência A.
    /// </summary>
    [Fact]
    public void CalcularIpc_DeveRetornarValoresExatos_ParaCenarioDeReferenciaAeB()
    {
        // Arrange
        var calculadora = new CalculadoraIpcService(CriarOpcoesPadrao());

        // Act
        var ipcB = calculadora.CalcularIpc(severidade: 0.50, chuvaMm12h: 0.0, vulnerabilidadeVia: 0.20);
        var ipcA = calculadora.CalcularIpc(severidade: 0.75, chuvaMm12h: 35.0, vulnerabilidadeVia: 0.90);

        // Assert
        ipcB.Should().Be(0.26);
        ipcA.Should().Be(0.78);
        ipcA.Should().BeGreaterThan(ipcB);
    }

    /// <summary>
    /// Deve saturar a chuva acima de 50mm no teto máximo (1.0) e respeitar o limite mínimo (0.0).
    /// </summary>
    [Theory]
    [InlineData(1.0, 120.0, 1.0, 1.0)]
    [InlineData(0.0, 0.0, 0.0, 0.0)]
    [InlineData(1.5, 200.0, 1.5, 1.0)]
    [InlineData(-0.5, -10.0, -0.5, 0.0)]
    public void CalcularIpc_DeveRespeitarLimitesESaturacaoDeChuva(
        double severidade,
        double chuvaMm12h,
        double vulnerabilidade,
        double esperado)
    {
        // Arrange
        var calculadora = new CalculadoraIpcService(CriarOpcoesPadrao());

        // Act
        var ipc = calculadora.CalcularIpc(severidade, chuvaMm12h, vulnerabilidade);

        // Assert
        ipc.Should().Be(esperado);
    }

    /// <summary>
    /// Deve lançar DomainException quando a soma dos pesos configurados for diferente de 1.0.
    /// </summary>
    [Fact]
    public void Construtor_DeveLancarDomainException_QuandoSomaDosPesosForDiferenteDeUm()
    {
        // Arrange
        var opcoesInvalidas = Options.Create(new IpcSettings
        {
            PesoSeveridade = 0.5,
            PesoChuva = 0.5,
            PesoVulnerabilidade = 0.2,
            TetoChuvaMm = 50.0
        });

        // Act
        var act = () => new CalculadoraIpcService(opcoesInvalidas);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("A soma dos pesos do IPC deve ser exatamente 1.0.");
    }
}
