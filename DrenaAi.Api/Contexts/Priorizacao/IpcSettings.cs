namespace DrenaAi.Api.Contexts.Priorizacao;

/// <summary>
/// Parâmetros configuráveis de pesos e limites para o cálculo do Índice de Prioridade de Coleta (IPC).
/// </summary>
public class IpcSettings
{
    /// <summary>
    /// Peso atribuído à variável de severidade do resíduo (padrão: 0.4).
    /// </summary>
    public double PesoSeveridade { get; set; } = 0.4;

    /// <summary>
    /// Peso atribuído à variável de precipitação prevista normalizada (padrão: 0.3).
    /// </summary>
    public double PesoChuva { get; set; } = 0.3;

    /// <summary>
    /// Peso atribuído à vulnerabilidade histórica e topográfica da via (padrão: 0.3).
    /// </summary>
    public double PesoVulnerabilidade { get; set; } = 0.3;

    /// <summary>
    /// Teto de precipitação em milímetros utilizado para normalizar a chuva prevista (padrão: 50.0 mm).
    /// </summary>
    public double TetoChuvaMm { get; set; } = 50.0;
}
