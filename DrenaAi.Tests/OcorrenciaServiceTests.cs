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
            -23.556,
            -46.599,
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

    /// <summary>
    /// Deve persistir e retornar o Value Object EnderecoVo e as parcelas do IPC ao criar ocorrência no Vale do Anhangabaú.
    /// </summary>
    [Fact]
    public async Task CriarAsync_DevePersistirValueObjectEnderecoECalcularParcelasIpc_NoValeDoAnhangabau()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var service = CriarService(context);
        var usuarioId = Guid.NewGuid();
        var enderecoVo = new EnderecoVo(
            Logradouro: "Rua Formosa, 95",
            Bairro: "Vale do Anhangabaú",
            Latitude: -23.5461,
            Longitude: -46.6388);

        var request = new CriarOcorrenciaRequest(
            Tipo: "ENTULHO",
            Descricao: null,
            FotoUrl: "http://localhost:5239/uploads/oc-teste.jpg",
            Latitude: enderecoVo.Latitude,
            Longitude: enderecoVo.Longitude,
            Bairro: enderecoVo.Bairro,
            Endereco: enderecoVo);

        // Act
        var criada = await service.CriarAsync(request, usuarioId);
        var detalhe = await service.ObterPorIdAsync(criada.Id);

        // Assert
        criada.Endereco.Should().NotBeNull();
        criada.Endereco!.Logradouro.Should().Be("Rua Formosa, 95");
        criada.Endereco.Bairro.Should().Be("Vale do Anhangabaú");
        criada.ParcelasIpc.Should().NotBeNull();
        criada.PosicaoFila.Should().Be(1);

        detalhe.Endereco.Should().NotBeNull();
        detalhe.Endereco!.Logradouro.Should().Be("Rua Formosa, 95");
        detalhe.Latitude.Should().Be(-23.5461);
        detalhe.Longitude.Should().Be(-46.6388);
    }

    /// <summary>
    /// Deve atualizar o status da ocorrência, gravar o novo registro no histórico e removê-la da fila ao concluir.
    /// </summary>
    [Fact]
    public async Task AtualizarStatusAsync_DeveAtualizarStatusERegistrarHistoricoERemoverDaFilaQuandoConcluida()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var service = CriarService(context);
        var usuarioId = Guid.NewGuid();

        var primeira = await service.CriarAsync(
            new CriarOcorrenciaRequest("BUEIRO_ENTUPIDO", "Bueiro entupido", "foto_1.jpg", -23.5461, -46.6388, "Vale do Anhangabaú"),
            usuarioId);
        var segunda = await service.CriarAsync(
            new CriarOcorrenciaRequest("LIXO_ORGANICO", "Lixo na calçada", "cenario_b.jpg", -23.5493, -46.6391, "Vale do Anhangabaú"),
            usuarioId);

        // Act - Avança para EM_ATENDIMENTO
        var emAtendimento = await service.AtualizarStatusAsync(primeira.Id, StatusOcorrencia.EM_ATENDIMENTO);

        // Assert - Permanece na fila e registra novo item no histórico
        emAtendimento.Status.Should().Be(StatusOcorrencia.EM_ATENDIMENTO.ToString());
        emAtendimento.PosicaoFila.Should().Be(1);
        emAtendimento.HistoricoStatus.Should().HaveCount(2);
        emAtendimento.HistoricoStatus.Select(h => h.Status).Should().ContainInOrder("NA_FILA", "EM_ATENDIMENTO");

        // Act - Conclui a primeira ocorrência
        var concluida = await service.AtualizarStatusAsync(primeira.Id, StatusOcorrencia.CONCLUIDA);
        var detalheSegunda = await service.ObterPorIdAsync(segunda.Id);

        // Assert - Sai da fila ativa e promove a segunda colocada para 1º lugar
        concluida.Status.Should().Be(StatusOcorrencia.CONCLUIDA.ToString());
        concluida.PosicaoFila.Should().BeNull();
        concluida.HistoricoStatus.Should().HaveCount(3);
        concluida.HistoricoStatus.Select(h => h.Status).Should().ContainInOrder("NA_FILA", "EM_ATENDIMENTO", "CONCLUIDA");
        detalheSegunda.PosicaoFila.Should().Be(1);
    }

    /// <summary>
    /// Deve consolidar duas denúncias de moradores no mesmo endereço em uma única ocorrência ativa
    /// incrementando o contador de confirmações comunitárias.
    /// </summary>
    [Fact]
    public async Task CriarAsync_DeveConsolidarOcorrenciaNoMesmoLocalEIncrementarConfirmacoes()
    {
        // Arrange
        await using var context = CriarDbContextEmMemoria();
        var service = CriarService(context);
        var endereco = new EnderecoVo("Praça da Bandeira, 40", "Vale do Anhangabaú", -23.5493, -46.6391);

        var primeira = await service.CriarAsync(
            new CriarOcorrenciaRequest("BUEIRO_ENTUPIDO", "Bueiro entupido", "foto_1.jpg", Endereco: endereco),
            Guid.NewGuid());

        // Act
        var segunda = await service.CriarAsync(
            new CriarOcorrenciaRequest("BUEIRO_ENTUPIDO", "Ainda está entupido", "foto_2.jpg", Endereco: endereco),
            Guid.NewGuid());

        var fila = await service.ObterFilaAsync("Vale do Anhangabaú");
        var detalhe = await service.ObterPorIdAsync(primeira.Id);

        // Assert
        segunda.Id.Should().Be(primeira.Id);
        segunda.Confirmacoes.Should().Be(1);
        fila.Should().ContainSingle();
        fila[0].Confirmacoes.Should().Be(1);
        detalhe.Confirmacoes.Should().Be(1);
    }
}
