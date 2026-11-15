namespace DrenaAi.Api.Contexts.Telemetrias;

/// <summary>
/// Contrato do serviço responsável pelo cadastro, consulta e ingestão de telemetria dos sensores IoT ESP32.
/// </summary>
public interface ITelemetriaService
{
    /// <summary>
    /// Cadastra um novo bueiro monitorado por sensor IoT com localização geográfica e leitura inicial.
    /// </summary>
    /// <param name="request">Dados de endereço, coordenadas e leitura inicial opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Dados completos do sensor cadastrado com identificador gerado pelo servidor.</returns>
    Task<SensorBueiroResponse> CadastrarSensorAsync(
        CadastrarSensorRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos os bueiros monitorados por sensores IoT, com filtro opcional por bairro.
    /// </summary>
    /// <param name="bairro">Nome opcional do bairro para filtragem.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Lista de sensores ordenada por maior percentual de obstrução.</returns>
    Task<IReadOnlyList<SensorBueiroResponse>> ListarSensoresAsync(
        string? bairro = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o estado atual de um bueiro monitorado pelo seu identificador único.
    /// </summary>
    /// <param name="bueiroId">Identificador único do bueiro.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Dados atuais de localização, obstrução e última leitura do sensor.</returns>
    Task<SensorBueiroResponse> ObterSensorPorIdAsync(
        Guid bueiroId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processa uma leitura de telemetria de bueiro cadastrado, atualiza seu estado atual e abre ocorrência crítica quando necessário.
    /// </summary>
    /// <param name="request">Dados de leitura do sensor ultrassônico e de gás.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Resultado do processamento indicando se uma ocorrência foi aberta.</returns>
    Task<TelemetriaResponse> ReceberTelemetriaAsync(
        TelemetriaRequest request,
        CancellationToken cancellationToken = default);
}
