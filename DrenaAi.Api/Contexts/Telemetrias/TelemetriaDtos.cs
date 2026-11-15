using System.ComponentModel.DataAnnotations;

namespace DrenaAi.Api.Contexts.Telemetrias;

/// <summary>
/// Payload para cadastro de um novo bueiro monitorado por sensor IoT no banco de dados.
/// O identificador único (Guid) e o código padrão do sensor são gerados automaticamente pelo backend.
/// </summary>
/// <param name="Logradouro">Rua e número onde o bueiro está instalado.</param>
/// <param name="Bairro">Bairro de localização do bueiro.</param>
/// <param name="Latitude">Latitude geográfica (-90 a 90).</param>
/// <param name="Longitude">Longitude geográfica (-180 a 180).</param>
/// <param name="CodigoSensor">Código opcional do dispositivo físico ESP32 (gerado automaticamente se omitido).</param>
/// <param name="DistanciaCm">Distância inicial opcional em centímetros (padrão: 30 cm = 0% de obstrução).</param>
/// <param name="GasPpm">Concentração inicial opcional de gás em ppm (padrão: 0 ppm).</param>
public record CadastrarSensorRequest(
    [Required] string Logradouro,
    [Required] string Bairro,
    [Range(-90.0, 90.0, ErrorMessage = "A latitude deve estar entre -90 e 90.")] double Latitude,
    [Range(-180.0, 180.0, ErrorMessage = "A longitude deve estar entre -180 e 180.")] double Longitude,
    string? CodigoSensor = null,
    [Range(0, double.MaxValue, ErrorMessage = "A distância deve ser maior ou igual a zero.")] double? DistanciaCm = null,
    [Range(0, double.MaxValue, ErrorMessage = "O nível de gás deve ser maior ou igual a zero.")] double? GasPpm = null
);

/// <summary>
/// Representação de um bueiro monitorado e sua última leitura para exibição no painel de sensores.
/// </summary>
/// <param name="Id">Identificador único do bueiro gerado pelo backend.</param>
/// <param name="CodigoSensor">Código do dispositivo ESP32 instalado no bueiro.</param>
/// <param name="Endereco">Logradouro onde o bueiro está localizado.</param>
/// <param name="Bairro">Bairro onde o bueiro está localizado.</param>
/// <param name="Latitude">Latitude geográfica do bueiro.</param>
/// <param name="Longitude">Longitude geográfica do bueiro.</param>
/// <param name="Obstrucao">Percentual atual de obstrução do bueiro (0 a 100).</param>
/// <param name="DistanciaCm">Última distância medida pelo sensor ultrassônico em centímetros.</param>
/// <param name="GasPpm">Última concentração de gás medida em partes por milhão (ppm).</param>
/// <param name="GasDetectado">Indica se foi detectado nível crítico de gás de matéria orgânica.</param>
/// <param name="UltimaLeitura">Data e hora (UTC) da última leitura registrada.</param>
/// <param name="OcorrenciaId">Identificador da ocorrência ativa vinculada ao bueiro, quando houver.</param>
public record SensorBueiroResponse(
    Guid Id,
    string CodigoSensor,
    string Endereco,
    string Bairro,
    double Latitude,
    double Longitude,
    int Obstrucao,
    double DistanciaCm,
    double GasPpm,
    bool GasDetectado,
    DateTime UltimaLeitura,
    Guid? OcorrenciaId
);

/// <summary>
/// Payload de telemetria enviado periodicamente pelo dispositivo IoT ESP32 instalado em bueiro.
/// O horário da leitura é registrado automaticamente pelo backend caso não seja informado.
/// </summary>
/// <param name="BueiroId">Identificador único (Guid) do bueiro cadastrado no sistema.</param>
/// <param name="DistanciaCm">Distância medida pelo sensor ultrassônico até o nível de resíduos/água em centímetros.</param>
/// <param name="GasPpm">Concentração de gás detectada pelo sensor em partes por milhão (ppm).</param>
/// <param name="SensorId">Código opcional do dispositivo ESP32.</param>
/// <param name="Timestamp">Data e hora (UTC) opcional; quando omitida, o servidor utiliza o horário atual.</param>
public record TelemetriaRequest(
    [Required] Guid BueiroId,
    [Range(0, double.MaxValue, ErrorMessage = "A distância deve ser maior ou igual a zero.")] double DistanciaCm,
    [Range(0, double.MaxValue, ErrorMessage = "O nível de gás deve ser maior ou igual a zero.")] double GasPpm,
    string? SensorId = null,
    DateTime? Timestamp = null
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
