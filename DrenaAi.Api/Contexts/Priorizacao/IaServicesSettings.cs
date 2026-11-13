namespace DrenaAi.Api.Contexts.Priorizacao;

/// <summary>
/// Configurações de integração com os microsserviços externos de Visão Computacional e Machine Learning.
/// </summary>
public class IaServicesSettings
{
    /// <summary>
    /// URL base do serviço de visão computacional responsável por avaliar a severidade de imagens.
    /// </summary>
    public string VisionBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL base do serviço de Machine Learning responsável por previsão de chuva e vulnerabilidade da via.
    /// </summary>
    public string MlFloodRiskBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Indica se o sistema deve utilizar o modo simulado determinístico (mock) em vez de chamadas HTTP reais.
    /// </summary>
    public bool UseMocks { get; set; } = true;
}
