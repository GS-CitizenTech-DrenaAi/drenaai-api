namespace DrenaAi.Api.Contexts.Priorizacao;

/// <summary>
/// Contrato do serviço responsável pelo cálculo matemático do Índice de Prioridade de Coleta (IPC).
/// </summary>
public interface ICalculadoraIpcService
{
    /// <summary>
    /// Calcula o Índice de Prioridade de Coleta (IPC) combinando severidade, chuva prevista e vulnerabilidade da via.
    /// </summary>
    /// <param name="severidade">Severidade da ocorrência avaliada entre 0.0 e 1.0.</param>
    /// <param name="chuvaMm12h">Volume de chuva previsto em milímetros para as próximas 12 horas.</param>
    /// <param name="vulnerabilidadeVia">Índice de risco de alagamento da via entre 0.0 e 1.0.</param>
    /// <returns>Valor numérico do IPC arredondado em 4 casas decimais no intervalo de 0.0 a 1.0.</returns>
    double CalcularIpc(double severidade, double chuvaMm12h, double vulnerabilidadeVia);
}
