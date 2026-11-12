namespace DrenaAi.Api.Contexts.Ocorrencias;

/// <summary>
/// Define a origem do registro da ocorrência no sistema DrenaAi.
/// </summary>
public enum OrigemOcorrencia
{
    /// <summary>
    /// Ocorrência reportada por um morador através do aplicativo móvel.
    /// </summary>
    APP = 1,

    /// <summary>
    /// Ocorrência gerada automaticamente por telemetria de sensor IoT ESP32 instalado em bueiro.
    /// </summary>
    SENSOR = 2
}

/// <summary>
/// Define os estados possíveis do ciclo de vida de atendimento de uma ocorrência na fila de coleta.
/// </summary>
public enum StatusOcorrencia
{
    /// <summary>
    /// Ocorrência aguardando atendimento na fila priorizada pelo IPC.
    /// </summary>
    NA_FILA = 1,

    /// <summary>
    /// Ocorrência em processo de atendimento ou coleta pela equipe de zeladoria.
    /// </summary>
    EM_ATENDIMENTO = 2,

    /// <summary>
    /// Ocorrência atendida e finalizada.
    /// </summary>
    CONCLUIDA = 3
}
