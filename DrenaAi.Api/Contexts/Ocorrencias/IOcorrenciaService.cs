namespace DrenaAi.Api.Contexts.Ocorrencias;

/// <summary>
/// Contrato do serviço responsável pelo gerenciamento de ocorrências e ordenação da fila de coleta.
/// </summary>
public interface IOcorrenciaService
{
    /// <summary>
    /// Registra uma nova ocorrência enviada pelo aplicativo, sanitizando PII e calculando severidade e IPC.
    /// </summary>
    /// <param name="request">Dados da ocorrência reportada pelo morador.</param>
    /// <param name="usuarioId">Identificador do morador autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Dados da ocorrência criada com severidade, IPC e descrição sanitizada.</returns>
    Task<CriarOcorrenciaResponse> CriarAsync(
        CriarOcorrenciaRequest request,
        Guid usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista ocorrências filtradas opcionalmente por bairro e status.
    /// </summary>
    /// <param name="bairro">Filtro opcional pelo nome do bairro.</param>
    /// <param name="status">Filtro opcional pelo status da ocorrência.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Lista de ocorrências ordenadas por IPC decrescente e data de criação crescente.</returns>
    Task<IReadOnlyList<OcorrenciaResumoResponse>> ListarAsync(
        string? bairro,
        StatusOcorrencia? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os detalhes completos de uma ocorrência específica incluindo seu histórico cronológico de status.
    /// </summary>
    /// <param name="id">Identificador único da ocorrência.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Detalhes da ocorrência e linha do tempo de status.</returns>
    Task<OcorrenciaDetalheResponse> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta a fila de coleta contendo ocorrências não concluídas ordenadas por IPC decrescente e antiguidade.
    /// </summary>
    /// <param name="bairro">Filtro opcional pelo nome do bairro.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Fila de ocorrências ordenada por prioridade.</returns>
    Task<IReadOnlyList<OcorrenciaResumoResponse>> ObterFilaAsync(
        string? bairro = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprocessa o IPC de uma ocorrência específica, atualiza a fila e registra notificação de priorização.
    /// </summary>
    /// <param name="ocorrenciaId">Identificador da ocorrência a ser priorizada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Resultado da priorização com o novo IPC e a fila reordenada.</returns>
    Task<PriorizarFilaResponse> PriorizarFilaAsync(
        Guid ocorrenciaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza o status de uma ocorrência existente e registra a transição no histórico cronológico.
    /// </summary>
    /// <param name="id">Identificador único da ocorrência.</param>
    /// <param name="novoStatus">Novo status a ser aplicado à ocorrência.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Detalhes atualizados da ocorrência com o novo histórico e posição na fila.</returns>
    Task<OcorrenciaDetalheResponse> AtualizarStatusAsync(
        Guid id,
        StatusOcorrencia novoStatus,
        CancellationToken cancellationToken = default);
}
