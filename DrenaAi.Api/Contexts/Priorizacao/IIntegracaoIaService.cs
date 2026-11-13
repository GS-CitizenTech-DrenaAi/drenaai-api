namespace DrenaAi.Api.Contexts.Priorizacao;

/// <summary>
/// Contrato de integração com os serviços de Visão Computacional (severidade) e ML Climático/Topográfico (risco).
/// </summary>
public interface IIntegracaoIaService
{
    /// <summary>
    /// Avalia a severidade de uma ocorrência a partir da referência de uma foto capturada.
    /// </summary>
    /// <param name="foto">URL, caminho ou identificador da foto enviada para análise.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Resultado estruturado com severidade, confiança e justificativa técnica.</returns>
    Task<AvaliarSeveridadeResponse> AvaliarSeveridadeAsync(string? foto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a previsão de chuva e o índice de vulnerabilidade da via para o bairro informado.
    /// </summary>
    /// <param name="bairro">Nome do bairro onde a ocorrência foi registrada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Dados pluviométricos e vulnerabilidade da via para cálculo do IPC.</returns>
    Task<PrevisaoRiscoResponse> ObterPrevisaoERiscoAsync(string bairro, CancellationToken cancellationToken = default);
}
