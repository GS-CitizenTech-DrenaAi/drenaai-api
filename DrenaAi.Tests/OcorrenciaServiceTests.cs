using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Data;
using DrenaAi.Api.Exceptions;
using DrenaAi.Api.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DrenaAi.Tests;

/// <summary>
/// Testes unitários e funcionais para <see cref="OcorrenciaService"/>.
/// </summary>
public class OcorrenciaServiceTests
{
    private static AppDbContext CriarDbContextEmMemoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static OcorrenciaService CriarService(AppDbContext context)
    {
        var calculadora = new CalculadoraIpcService(Options.Create(new IpcSettings
        {
            PesoSeveridade = 0.4,
            PesoChuva = 0.3,
            PesoVulnerabilidade = 0.3,
            TetoChuvaMm = 50.0
        }));

        return new OcorrenciaService(
            context,
            new PiiSanitizerService(),
            new IntegracaoIaMockService(),
            calculadora,
            NullLogger<OcorrenciaService>.Instance);
    }

    /// <summary>
    /// Deve filtrar e retornar exclusivamente as ocorrências do bairro solicitado.
    /// </summary>
    [Fact]
    public async Task ListarAsync_DeveRetornarApenasOcorrenciasDoBairroInformado()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var service = CriarService(context);
        var usuarioId = Guid.NewGuid();

        await service.CriarAsync(
            new CriarOcorrenciaRequest("LIXO_ACUMULADO", "Sacos de lixo na calçada", "foto_1.jpg", -23.556, -46.599, "Mooca"),
            usuarioId);
        await service.CriarAsync(
            new CriarOcorrenciaRequest("BUEIRO_ENTUPIDO", "Bueiro entupido na esquina", "foto_2.jpg", -23.557, -46.598, "Mooca"),
            usuarioId);
        await service.CriarAsync(
            new CriarOcorrenciaRequest("LIXO_ACUMULADO", "Lixo leve na via", "cenario_b.jpg", -23.561, -46.701, "Pinheiros"),
            usuarioId);

        // Act
        var resultado = await service.ListarAsync("Mooca", null);

        // Assert
        resultado.Should().HaveCount(2);
        resultado.Should().OnlyContain(o => o.Bairro == "Mooca");
    }

    /// <summary>
    /// Deve retornar os detalhes da ocorrência e seu histórico cronológico de status.
    /// </summary>
    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarDetalhesComHistoricoStatus_QuandoExistir()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var service = CriarService(context);
        var criada = await service.CriarAsync(
            new CriarOcorrenciaRequest("BUEIRO_ENTUPIDO", "Bueiro cheio na via", "foto_bueiro.jpg", -23.589, -46.634, "Vila Mariana"),
            Guid.NewGuid());

        // Act
        var detalhe = await service.ObterPorIdAsync(criada.Id);

        // Assert
        detalhe.Id.Should().Be(criada.Id);
        detalhe.Bairro.Should().Be("Vila Mariana");
        detalhe.HistoricoStatus.Should().NotBeEmpty();
        detalhe.HistoricoStatus[0].Status.Should().Be(StatusOcorrencia.NA_FILA.ToString());
        detalhe.HistoricoStatus[0].DataHora.Should().NotBe(default);
    }

    /// <summary>
    /// Deve lançar NotFoundException quando o ID da ocorrência não existir.
    /// </summary>
    [Fact]
    public async Task ObterPorIdAsync_DeveLancarNotFoundException_QuandoIdNaoExistir()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var service = CriarService(context);
        var idInexistente = Guid.Parse("00000000-0000-0000-0000-000000000099");

        // Act
        var act = async () => await service.ObterPorIdAsync(idInexistente);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Deve posicionar a Ocorrência A (IPC 0.78) acima da Ocorrência B (IPC 0.26) na fila,
    /// mesmo tendo sido criada depois.
    /// </summary>
    [Fact]
    public async Task ObterFilaAsync_DeveOrdenarOcorrenciaAAcimaDaOcorrenciaB_NoCenarioDeReferencia()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var service = CriarService(context);
        var usuarioId = Guid.NewGuid();

        // Ocorrência B chega primeiro (Pinheiros: 0mm, V=0.20, S=0.50 -> IPC = 0.26)
        var ocorrenciaB = await service.CriarAsync(
            new CriarOcorrenciaRequest("LIXO_ACUMULADO", "Lixo leve na via alta", "cenario_b.jpg", -23.561, -46.701, "Pinheiros"),
            usuarioId);

        // Ocorrência A chega depois (Vila Mariana: 35mm, V=0.90, S=0.75 -> IPC = 0.78)
        var ocorrenciaA = await service.CriarAsync(
            new CriarOcorrenciaRequest("BUEIRO_ENTUPIDO", "Bueiro cheio em via que alaga", "bueiro_obstruido_critico.jpg", -23.589, -46.634, "Vila Mariana"),
            usuarioId);

        // Act
        var fila = await service.ObterFilaAsync();

        // Assert
        fila.Should().HaveCount(2);
        fila[0].Id.Should().Be(ocorrenciaA.Id);
        fila[0].Ipc.Should().Be(0.78);
        fila[1].Id.Should().Be(ocorrenciaB.Id);
        fila[1].Ipc.Should().Be(0.26);
    }

    /// <summary>
    /// Deve desempatar ocorrências com mesmo IPC pela mais antiga (CriadaEm crescente).
    /// </summary>
    [Fact]
    public async Task ObterFilaAsync_DeveDesempatarPorAntiguidade_QuandoIpcForIdentico()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var service = CriarService(context);

        var primeira = new Ocorrencia(
            OrigemOcorrencia.APP,
            "LIXO_ACUMULADO",
            "Primeira ocorrência registrada",
            "foto1.jpg",
            -23.55,
            -46.63,
            "Mooca",
            severidade: 0.60,
            ipc: 0.60,
            usuarioId: Guid.NewGuid());

        context.Ocorrencias.Add(primeira);
        await context.SaveChangesAsync();

        await Task.Delay(20);

        var segunda = new Ocorrencia(
            OrigemOcorrencia.APP,
            "LIXO_ACUMULADO",
            "Segunda ocorrência registrada",
            "foto2.jpg",
            -23.55,
            -46.63,
            "Mooca",
            severidade: 0.60,
            ipc: 0.60,
            usuarioId: Guid.NewGuid());

        context.Ocorrencias.Add(segunda);
        await context.SaveChangesAsync();

        // Act
        var fila = await service.ObterFilaAsync();

        // Assert
        fila.Should().HaveCount(2);
        fila[0].Id.Should().Be(primeira.Id);
        fila[1].Id.Should().Be(segunda.Id);
        fila[0].CriadaEm.Should().BeBefore(fila[1].CriadaEm);
    }
}
