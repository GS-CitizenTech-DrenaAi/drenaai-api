using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Contexts.Priorizacao;
using DrenaAi.Api.Data;
using DrenaAi.Api.Exceptions;

namespace DrenaAi.Api.Contexts.Telemetrias;

/// <summary>
/// Serviço responsável pela conversão de leituras físicas de sensores ESP32 em ocorrências críticas na fila de coleta.
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
    /// <exception cref="DomainException">Lançada quando os dados de telemetria possuem valores negativos ou identificadores ausentes.</exception>
    public async Task<TelemetriaResponse> ReceberTelemetriaAsync(
        TelemetriaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SensorId) || string.IsNullOrWhiteSpace(request.BueiroId))
            throw new DomainException("Os identificadores 'sensorId' e 'bueiroId' são obrigatórios.");

        if (request.DistanciaCm < 0 || request.GasPpm < 0)
            throw new DomainException("As leituras de distância e gás não podem ser negativas.");

        var severidadeDistancia = Math.Clamp((30.0 - request.DistanciaCm) / 30.0, 0.0, 1.0);
        var severidadeGas = Math.Clamp(request.GasPpm / 800.0, 0.0, 1.0);
        var severidade = Math.Round(Math.Max(severidadeDistancia, severidadeGas), 4);

        var ehCritico = request.DistanciaCm <= LimiarDistanciaCriticaCm || request.GasPpm >= LimiarGasCriticoPpm;
        if (!ehCritico)
        {
            return new TelemetriaResponse(
                OcorrenciaGerada: false,
                OcorrenciaId: null,
                Severidade: severidade,
                Ipc: null,
                Mensagem: "Telemetria recebida dentro dos parâmetros normais.");
        }

        var localizacao = ResolverLocalizacaoBueiro(request.BueiroId);
        var previsaoRisco = await _integracaoIa.ObterPrevisaoERiscoAsync(localizacao.Bairro, cancellationToken);
        var ipc = _calculadoraIpc.CalcularIpc(severidade, previsaoRisco.ChuvaMm12h, previsaoRisco.VulnerabilidadeVia);

        var descricao = $"Alerta automático IoT ({request.SensorId.Trim()}) no bueiro {request.BueiroId.Trim()}: " +
                        $"distância {request.DistanciaCm}cm, gás {request.GasPpm}ppm.";

        var ocorrencia = new Ocorrencia(
            OrigemOcorrencia.SENSOR,
            "OBSTRUCAO_BUEIRO",
            descricao,
            fotoUrl: null,
            localizacao,
            severidade,
            ipc,
            usuarioId: null);

        _context.Ocorrencias.Add(ocorrencia);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Telemetria crítica do sensor {SensorId} gerou ocorrência {OcorrenciaId} no bairro {Bairro} com IPC {Ipc}.",
            request.SensorId,
            ocorrencia.Id,
            localizacao.Bairro,
            ipc);

        return new TelemetriaResponse(
            OcorrenciaGerada: true,
            OcorrenciaId: ocorrencia.Id,
            Severidade: severidade,
            Ipc: ipc,
            Mensagem: "Telemetria crítica processada e ocorrência aberta na fila.");
    }

    private static Endereco ResolverLocalizacaoBueiro(string bueiroId)
    {
        var idUpper = bueiroId.Trim().ToUpperInvariant();

        if (idUpper.Contains("VM"))
            return new Endereco("Rua Domingos de Morais, 2564", "Vila Mariana", -23.589, -46.634);

        if (idUpper.Contains("PI"))
            return new Endereco("Rua dos Pinheiros, 498", "Pinheiros", -23.561, -46.701);

        if (idUpper.Contains("MO"))
            return new Endereco("Rua da Mooca, 1800", "Mooca", -23.556, -46.599);

        return new Endereco("Praça da Bandeira, 40", "Vale do Anhangabaú", -23.5493, -46.6391);
    }
}
