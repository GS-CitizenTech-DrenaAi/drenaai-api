using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Data;
using DrenaAi.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DrenaAi.Api.Contexts.Telemetrias;

/// <summary>
/// Serviço responsável pelo cadastro de bueiros monitorados, consulta do painel de sensores e ingestão de leituras ESP32.
/// </summary>
public class TelemetriaService : ITelemetriaService
{
    private const double LimiarDistanciaCriticaCm = 20.0;
    private const double LimiarGasCriticoPpm = 400.0;

    private readonly AppDbContext _context;
    private readonly IIntegracaoIaService _integracaoIa;
    private readonly ICalculadoraIpcService _calculadoraIpc;
    private readonly ILogger<TelemetriaService> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TelemetriaService"/>.
    /// </summary>
    /// <param name="context">Contexto de banco de dados da aplicação.</param>
    /// <param name="integracaoIa">Serviço de integração para consulta de previsão de chuva e vulnerabilidade da via.</param>
    /// <param name="calculadoraIpc">Calculadora do Índice de Prioridade de Coleta.</param>
    /// <param name="logger">Logger estruturado da aplicação.</param>
    public TelemetriaService(
        AppDbContext context,
        IIntegracaoIaService integracaoIa,
        ICalculadoraIpcService calculadoraIpc,
        ILogger<TelemetriaService> logger)
    {
        _context = context;
        _integracaoIa = integracaoIa;
        _calculadoraIpc = calculadoraIpc;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="DomainException">Lançada quando os dados de localização são inválidos ou as leituras são negativas.</exception>
    public async Task<SensorBueiroResponse> CadastrarSensorAsync(
        CadastrarSensorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Logradouro) || string.IsNullOrWhiteSpace(request.Bairro))
            throw new DomainException("Os campos 'logradouro' e 'bairro' são obrigatórios.");

        var distanciaInicial = request.DistanciaCm ?? 30.0;
        var gasInicial = request.GasPpm ?? 0.0;

        if (distanciaInicial < 0 || gasInicial < 0)
            throw new DomainException("As leituras iniciais de distância e gás não podem ser negativas.");

        var timestampCadastro = DateTime.UtcNow;
        var localizacao = new Endereco(request.Logradouro, request.Bairro, request.Latitude, request.Longitude);
        var sensor = new SensorBueiro(
            localizacao,
            request.CodigoSensor,
            distanciaInicial,
            gasInicial,
            timestampCadastro);

        var (severidade, ehCritico) = AvaliarLeitura(distanciaInicial, gasInicial);
        if (ehCritico)
        {
            var ocorrencia = await ObterOuCriarOcorrenciaSensorAsync(
                sensor,
                distanciaInicial,
                gasInicial,
                severidade,
                timestampCadastro,
                cancellationToken);
            sensor.RegistrarLeitura(sensor.CodigoSensor, distanciaInicial, gasInicial, timestampCadastro, ocorrencia.Id);
        }

        _context.SensoresBueiros.Add(sensor);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sensor/bueiro {BueiroId} ({CodigoSensor}) cadastrado em {Logradouro} - {Bairro}.",
            sensor.Id,
            sensor.CodigoSensor,
            sensor.Localizacao.Logradouro,
            sensor.Localizacao.Bairro);

        return MapearRespostaSensor(sensor);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensorBueiroResponse>> ListarSensoresAsync(
        string? bairro = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SensoresBueiros.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(bairro))
        {
            var bairroFiltro = bairro.Trim().ToLowerInvariant();
            query = query.Where(s => s.Localizacao.Bairro.ToLower() == bairroFiltro);
        }

        var sensores = await query
            .OrderByDescending(s => s.Obstrucao)
            .ThenByDescending(s => s.UltimaLeitura)
            .ToListAsync(cancellationToken);

        return sensores.Select(MapearRespostaSensor).ToList();
    }

    /// <inheritdoc />
    /// <exception cref="NotFoundException">Lançada quando o bueiro não é encontrado.</exception>
    public async Task<SensorBueiroResponse> ObterSensorPorIdAsync(
        Guid bueiroId,
        CancellationToken cancellationToken = default)
    {
        var sensor = await _context.SensoresBueiros
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == bueiroId, cancellationToken);

        if (sensor is null)
            throw new NotFoundException($"Bueiro com ID '{bueiroId}' não encontrado.");

        return MapearRespostaSensor(sensor);
    }

    /// <inheritdoc />
    /// <exception cref="DomainException">Lançada quando os dados de telemetria possuem valores inválidos.</exception>
    /// <exception cref="NotFoundException">Lançada quando o bueiro informado ainda não foi cadastrado.</exception>
    public async Task<TelemetriaResponse> ReceberTelemetriaAsync(
        TelemetriaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.BueiroId == Guid.Empty)
            throw new DomainException("O identificador 'bueiroId' é obrigatório.");

        if (request.DistanciaCm < 0 || request.GasPpm < 0)
            throw new DomainException("As leituras de distância e gás não podem ser negativas.");

        var sensor = await _context.SensoresBueiros
            .FirstOrDefaultAsync(s => s.Id == request.BueiroId, cancellationToken);

        if (sensor is null)
            throw new NotFoundException($"Bueiro '{request.BueiroId}' não cadastrado. Realize o cadastro prévio em POST /api/telemetria/sensores.");

        var timestampLeitura = request.Timestamp ?? DateTime.UtcNow;
        var (severidade, ehCritico) = AvaliarLeitura(request.DistanciaCm, request.GasPpm);

        if (!ehCritico)
        {
            sensor.RegistrarLeitura(request.SensorId, request.DistanciaCm, request.GasPpm, timestampLeitura, ocorrenciaAtivaId: null);
            await _context.SaveChangesAsync(cancellationToken);

            return new TelemetriaResponse(
                OcorrenciaGerada: false,
                OcorrenciaId: null,
                Severidade: severidade,
                Ipc: null,
                Mensagem: "Telemetria recebida dentro dos parâmetros normais.");
        }

        var ocorrencia = await ObterOuCriarOcorrenciaSensorAsync(
            sensor,
            request.DistanciaCm,
            request.GasPpm,
            severidade,
            timestampLeitura,
            cancellationToken);
        sensor.RegistrarLeitura(request.SensorId, request.DistanciaCm, request.GasPpm, timestampLeitura, ocorrencia.Id);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Telemetria crítica do sensor {CodigoSensor} (Bueiro {BueiroId}) gerou ocorrência {OcorrenciaId} no bairro {Bairro} com IPC {Ipc}.",
            sensor.CodigoSensor,
            sensor.Id,
            ocorrencia.Id,
            sensor.Localizacao.Bairro,
            ocorrencia.Ipc);

        return new TelemetriaResponse(
            OcorrenciaGerada: true,
            OcorrenciaId: ocorrencia.Id,
            Severidade: severidade,
            Ipc: ocorrencia.Ipc,
            Mensagem: "Telemetria crítica processada e ocorrência aberta na fila.");
    }

    private static (double severidade, bool ehCritico) AvaliarLeitura(double distanciaCm, double gasPpm)
    {
        var severidadeDistancia = Math.Clamp((30.0 - distanciaCm) / 30.0, 0.0, 1.0);
        var severidadeGas = Math.Clamp(gasPpm / 800.0, 0.0, 1.0);
        var severidade = Math.Round(Math.Max(severidadeDistancia, severidadeGas), 4);
        var ehCritico = distanciaCm <= LimiarDistanciaCriticaCm || gasPpm >= LimiarGasCriticoPpm;
        return (severidade, ehCritico);
    }

    private async Task<Ocorrencia> ObterOuCriarOcorrenciaSensorAsync(
        SensorBueiro sensor,
        double distanciaCm,
        double gasPpm,
        double severidade,
        DateTime timestampLeitura,
        CancellationToken cancellationToken)
    {
        var previsaoRisco = await _integracaoIa.ObterPrevisaoERiscoAsync(sensor.Localizacao.Bairro, cancellationToken);
        var ipc = _calculadoraIpc.CalcularIpc(severidade, previsaoRisco.ChuvaMm12h, previsaoRisco.VulnerabilidadeVia);

        var descricao = $"Sensor IoT ({sensor.CodigoSensor}) em {sensor.Localizacao.Logradouro}: " +
                        $"bueiro cheio, precisa de coleta (distância {distanciaCm}cm, gás {gasPpm}ppm).";

        var candidatasAtivas = await _context.Ocorrencias
            .Include(o => o.HistoricoStatus)
            .Where(o => o.Status != StatusOcorrencia.CONCLUIDA)
            .OrderByDescending(o => o.CriadaEm)
            .ToListAsync(cancellationToken);

        var doMesmoLocal = candidatasAtivas
            .Where(o => (sensor.OcorrenciaId.HasValue && o.Id == sensor.OcorrenciaId.Value) ||
                        o.Localizacao.MesmoLocal(sensor.Localizacao))
            .ToList();

        var ocorrenciaAtiva = doMesmoLocal
            .FirstOrDefault(o => sensor.OcorrenciaId.HasValue && o.Id == sensor.OcorrenciaId.Value)
            ?? doMesmoLocal.FirstOrDefault();

        if (ocorrenciaAtiva is not null)
        {
            ocorrenciaAtiva.AtualizarAlertaSensor(descricao, severidade, ipc, timestampLeitura);

            var duplicadasSensor = doMesmoLocal
                .Where(o => o.Id != ocorrenciaAtiva.Id && o.Origem == OrigemOcorrencia.SENSOR)
                .ToList();

            if (duplicadasSensor.Count > 0)
            {
                _context.Ocorrencias.RemoveRange(duplicadasSensor);
            }

            return ocorrenciaAtiva;
        }

        var ocorrencia = new Ocorrencia(
            OrigemOcorrencia.SENSOR,
            "OBSTRUCAO_BUEIRO",
            descricao,
            fotoUrl: null,
            new Endereco(sensor.Localizacao.Logradouro, sensor.Localizacao.Bairro, sensor.Localizacao.Latitude, sensor.Localizacao.Longitude),
            severidade,
            ipc,
            usuarioId: null);

        ocorrencia.AtualizarAlertaSensor(descricao, severidade, ipc, timestampLeitura);
        _context.Ocorrencias.Add(ocorrencia);
        return ocorrencia;
    }

    private static SensorBueiroResponse MapearRespostaSensor(SensorBueiro sensor) =>
        new(
            Id: sensor.Id,
            CodigoSensor: sensor.CodigoSensor,
            Endereco: sensor.Localizacao.Logradouro,
            Bairro: sensor.Localizacao.Bairro,
            Latitude: sensor.Localizacao.Latitude,
            Longitude: sensor.Localizacao.Longitude,
            Obstrucao: sensor.Obstrucao,
            DistanciaCm: sensor.DistanciaCm,
            GasPpm: sensor.GasPpm,
            GasDetectado: sensor.GasDetectado,
            UltimaLeitura: NormalizarUtc(sensor.UltimaLeitura),
            OcorrenciaId: sensor.OcorrenciaId);

    private static DateTime NormalizarUtc(DateTime data) =>
        data.Kind == DateTimeKind.Utc
            ? data
            : DateTime.SpecifyKind(data, DateTimeKind.Utc);
}
