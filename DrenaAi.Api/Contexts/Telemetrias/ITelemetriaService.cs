namespace DrenaAi.Api.Contexts.Telemetrias;

/// <summary>
/// Contrato do serviço responsável pela ingestão e análise de telemetria dos sensores IoT ESP32.
/// </summary>
public interface ITelemetriaService
{
    /// <summary>
    /// Processa uma leitura de telemetria de bueiro e abre automaticamente uma ocorrência crítica quando necessário.
    /// </summary>
    /// <param name="request">Dados de leitura do sensor ultrassônico e de gás.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Resultado do processamento indicando se uma ocorrência foi aberta.</returns>
    Task<TelemetriaResponse> ReceberTelemetriaAsync(
        TelemetriaRequest request,
        CancellationToken cancellationToken = default);
}
