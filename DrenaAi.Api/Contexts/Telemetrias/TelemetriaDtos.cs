using System.ComponentModel.DataAnnotations;

namespace DrenaAi.Api.Contexts.Telemetrias;

/// <summary>
/// Payload de telemetria enviado periodicamente pelo dispositivo IoT ESP32 instalado em bueiro.
/// </summary>
/// <param name="SensorId">Identificador único do dispositivo sensor ESP32.</param>
/// <param name="BueiroId">Identificador do bueiro monitorado.</param>
/// <param name="DistanciaCm">Distância medida pelo sensor ultrassônico até o nível de resíduos/água em centímetros.</param>
/// <param name="GasPpm">Concentração de gás detectada pelo sensor em partes por milhão (ppm).</param>
/// <param name="Timestamp">Data e hora (UTC) da leitura realizada pelo dispositivo.</param>
public record TelemetriaRequest(
    [Required] string SensorId,
    [Required] string BueiroId,
    [Range(0, double.MaxValue, ErrorMessage = "A distância deve ser maior ou igual a zero.")] double DistanciaCm,
    [Range(0, double.MaxValue, ErrorMessage = "O nível de gás deve ser maior ou igual a zero.")] double GasPpm,
    DateTime Timestamp
);

/// <summary>
/// Resposta retornada após o processamento de uma leitura de telemetria IoT.
/// </summary>
/// <param name="OcorrenciaGerada">Indica se a leitura atingiu nível crítico e gerou uma ocorrência automática.</param>
/// <param name="OcorrenciaId">Identificador da ocorrência criada, quando aplicável.</param>
/// <param name="Severidade">Severidade física calculada a partir dos sensores (0.0 a 1.0).</param>
/// <param name="Ipc">Índice de Prioridade de Coleta calculado, quando uma ocorrência é gerada.</param>
/// <param name="Mensagem">Descrição do resultado do processamento da telemetria.</param>
public record TelemetriaResponse(
    bool OcorrenciaGerada,
    Guid? OcorrenciaId,
    double Severidade,
    double? Ipc,
    string Mensagem
);
