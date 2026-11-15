using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DrenaAi.Api.Contexts.Ocorrencias;

namespace DrenaAi.Api.Contexts.Telemetrias;

/// <summary>
/// Representa um bueiro monitorado por sensor IoT ESP32 com localização fixa e estado atual de obstrução.
/// </summary>
[Table("TB_DRENA_SENSOR_BUEIRO")]
public class SensorBueiro
{
    private const double AlturaReferenciaBueiroCm = 30.0;
    private const double LimiarGasDetectadoPpm = 400.0;

    /// <summary>
    /// Identificador único do bueiro/sensor gerado automaticamente pelo servidor.
    /// </summary>
    [Key]
    [Column("ID_SENSOR_BUEIRO")]
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Identificador ou código opcional do dispositivo físico ESP32 associado ao bueiro.
    /// </summary>
    [Required]
    [MaxLength(60)]
    [Column("CODIGO_SENSOR")]
    public string CodigoSensor { get; private set; } = string.Empty;

    /// <summary>
    /// Value Object contendo endereço postal (logradouro, bairro) e coordenadas geográficas do bueiro.
    /// </summary>
    [Required]
    public Endereco Localizacao { get; private set; } = null!;

    /// <summary>
    /// Última distância medida pelo sensor ultrassônico em centímetros.
    /// </summary>
    [Column("DISTANCIA_CM")]
    public double DistanciaCm { get; private set; }

    /// <summary>
    /// Última concentração de gás medida pelo sensor em partes por milhão (ppm).
    /// </summary>
    [Column("GAS_PPM")]
    public double GasPpm { get; private set; }

    /// <summary>
    /// Percentual atual de obstrução do bueiro (0 a 100%).
    /// </summary>
    [Column("OBSTRUCAO_PERCENTUAL")]
    public int Obstrucao { get; private set; }

    /// <summary>
    /// Indica se há detecção crítica de gás de matéria orgânica na última leitura.
    /// </summary>
    [Column("GAS_DETECTADO")]
    public bool GasDetectado { get; private set; }

    /// <summary>
    /// Data e hora (UTC) da última leitura registrada pelo backend.
    /// </summary>
    [Column("DT_ULTIMA_LEITURA")]
    public DateTime UltimaLeitura { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Identificador da última ocorrência crítica aberta para este bueiro, quando existir.
    /// </summary>
    [Column("ID_OCORRENCIA_ATIVA")]
    public Guid? OcorrenciaId { get; private set; }

    /// <summary>
    /// Construtor protegido sem parâmetros exigido pelo Entity Framework Core.
    /// </summary>
    protected SensorBueiro() { }

    /// <summary>
    /// Inicializa um novo bueiro monitorado com localização geográfica e leitura inicial.
    /// </summary>
    /// <param name="localizacao">Value Object com logradouro, bairro, latitude e longitude do bueiro.</param>
    /// <param name="codigoSensor">Código opcional do dispositivo ESP32 instalado no local.</param>
    /// <param name="distanciaCm">Leitura inicial de distância ultrassônica em centímetros.</param>
    /// <param name="gasPpm">Leitura inicial de gás em ppm.</param>
    /// <param name="timestamp">Data/hora opcional da leitura inicial (padrão: UTC atual).</param>
    public SensorBueiro(
        Endereco localizacao,
        string? codigoSensor = null,
        double distanciaCm = AlturaReferenciaBueiroCm,
        double gasPpm = 0.0,
        DateTime? timestamp = null)
    {
        Localizacao = localizacao ?? throw new ArgumentNullException(nameof(localizacao));
        CodigoSensor = string.IsNullOrWhiteSpace(codigoSensor)
            ? $"ESP32-{Id.ToString("N")[..6].ToUpperInvariant()}"
            : codigoSensor.Trim();

        RegistrarLeitura(CodigoSensor, distanciaCm, gasPpm, timestamp ?? DateTime.UtcNow, null);
    }

    /// <summary>
    /// Atualiza o estado atual do bueiro com uma nova leitura recebida do sensor ESP32.
    /// </summary>
    /// <param name="codigoSensor">Código do dispositivo que enviou a leitura (mantém o atual se nulo/vazio).</param>
    /// <param name="distanciaCm">Distância medida pelo sensor ultrassônico em centímetros.</param>
    /// <param name="gasPpm">Concentração de gás medida em ppm.</param>
    /// <param name="timestamp">Data/hora da leitura (UTC).</param>
    /// <param name="ocorrenciaAtivaId">Identificador da ocorrência crítica aberta, ou nulo se leitura normal.</param>
    public void RegistrarLeitura(
        string? codigoSensor,
        double distanciaCm,
        double gasPpm,
        DateTime timestamp,
        Guid? ocorrenciaAtivaId)
    {
        if (!string.IsNullOrWhiteSpace(codigoSensor))
            CodigoSensor = codigoSensor.Trim();

        DistanciaCm = Math.Max(0.0, distanciaCm);
        GasPpm = Math.Max(0.0, gasPpm);
        Obstrucao = CalcularObstrucaoPercentual(DistanciaCm);
        GasDetectado = GasPpm >= LimiarGasDetectadoPpm;
        UltimaLeitura = timestamp;
        OcorrenciaId = ocorrenciaAtivaId;
    }

    /// <summary>
    /// Converte a distância ultrassônica (cm) em percentual inteiro de obstrução (0 a 100%).
    /// </summary>
    /// <param name="distanciaCm">Distância medida em centímetros.</param>
    /// <returns>Percentual de obstrução entre 0 e 100.</returns>
    public static int CalcularObstrucaoPercentual(double distanciaCm)
    {
        var fracao = Math.Clamp((AlturaReferenciaBueiroCm - distanciaCm) / AlturaReferenciaBueiroCm, 0.0, 1.0);
        return (int)Math.Round(fracao * 100.0);
    }
}
