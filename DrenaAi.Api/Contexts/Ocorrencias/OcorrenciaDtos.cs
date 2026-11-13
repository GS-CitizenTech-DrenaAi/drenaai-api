using System.ComponentModel.DataAnnotations;
using DrenaAi.Api.Contexts.Ocorrencias;

namespace DrenaAi.Api.Contexts.Ocorrencias;

/// <summary>
/// Payload de requisição para criação de uma nova ocorrência enviada pelo aplicativo móvel.
/// </summary>
/// <param name="Tipo">Categoria do problema reportado pelo morador.</param>
/// <param name="Descricao">Relato descritivo que passará pelo filtro de sanitização de PII.</param>
/// <param name="FotoUrl">URL ou identificador da foto capturada.</param>
/// <param name="Latitude">Latitude geográfica obtida via GPS (-90 a 90).</param>
/// <param name="Longitude">Longitude geográfica obtida via GPS (-180 a 180).</param>
/// <param name="Bairro">Bairro onde a ocorrência foi identificada.</param>
public record CriarOcorrenciaRequest(
    [Required] string Tipo,
    [Required][MaxLength(500)] string Descricao,
    string? FotoUrl,
    [Range(-90, 90)] double Latitude,
    [Range(-180, 180)] double Longitude,
    [Required] string Bairro
);

/// <summary>
/// Resposta retornada após o registro de uma nova ocorrência pelo aplicativo móvel.
/// </summary>
/// <param name="Id">Identificador único da ocorrência criada.</param>
/// <param name="Severidade">Severidade avaliada pela visão computacional (0.0 a 1.0).</param>
/// <param name="Ipc">Índice de Prioridade de Coleta calculado (0.0 a 1.0).</param>
/// <param name="Status">Status inicial da ocorrência na fila.</param>
/// <param name="Descricao">Descrição já sanitizada contra exposição de PII.</param>
public record CriarOcorrenciaResponse(
    Guid Id,
    double Severidade,
    double Ipc,
    string Status,
    string Descricao
);

/// <summary>
/// Representação resumida de uma ocorrência para listagens e visualização na fila de coleta.
/// </summary>
/// <param name="Id">Identificador único da ocorrência.</param>
/// <param name="Origem">Origem do registro (APP ou SENSOR).</param>
/// <param name="Tipo">Categoria da ocorrência.</param>
/// <param name="Descricao">Descrição sanitizada.</param>
/// <param name="Bairro">Bairro onde a ocorrência está localizada.</param>
/// <param name="Severidade">Severidade normalizada (0.0 a 1.0).</param>
/// <param name="Ipc">Índice de Prioridade de Coleta (0.0 a 1.0).</param>
/// <param name="Status">Status atual do atendimento.</param>
/// <param name="CriadaEm">Data e hora (UTC) do registro.</param>
public record OcorrenciaResumoResponse(
    Guid Id,
    string Origem,
    string Tipo,
    string Descricao,
    string Bairro,
    double Severidade,
    double Ipc,
    string Status,
    DateTime CriadaEm
);

/// <summary>
/// Representa um item da linha do tempo de mudanças de status de uma ocorrência.
/// </summary>
/// <param name="Status">Status registrado na transição.</param>
/// <param name="DataHora">Data e hora (UTC) da transição.</param>
public record HistoricoStatusItemResponse(
    string Status,
    DateTime DataHora
);

/// <summary>
/// Representação detalhada de uma ocorrência incluindo sua linha do tempo de status.
/// </summary>
/// <param name="Id">Identificador único da ocorrência.</param>
/// <param name="Origem">Origem do registro (APP ou SENSOR).</param>
/// <param name="Tipo">Categoria da ocorrência.</param>
/// <param name="Descricao">Descrição sanitizada.</param>
/// <param name="FotoUrl">URL da foto enviada (nulo para origem SENSOR).</param>
/// <param name="Latitude">Latitude geográfica.</param>
/// <param name="Longitude">Longitude geográfica.</param>
/// <param name="Bairro">Bairro da ocorrência.</param>
/// <param name="Severidade">Severidade normalizada (0.0 a 1.0).</param>
/// <param name="Ipc">Índice de Prioridade de Coleta (0.0 a 1.0).</param>
/// <param name="Status">Status atual do atendimento.</param>
/// <param name="CriadaEm">Data e hora (UTC) de criação.</param>
/// <param name="HistoricoStatus">Lista cronológica de alterações de status.</param>
public record OcorrenciaDetalheResponse(
    Guid Id,
    string Origem,
    string Tipo,
    string Descricao,
    string? FotoUrl,
    double Latitude,
    double Longitude,
    string Bairro,
    double Severidade,
    double Ipc,
    string Status,
    DateTime CriadaEm,
    IReadOnlyList<HistoricoStatusItemResponse> HistoricoStatus
);

/// <summary>
/// Payload de requisição para acionamento do webhook de priorização e recálculo de IPC.
/// </summary>
/// <param name="OcorrenciaId">Identificador da ocorrência a ser reprocessada.</param>
public record PriorizarFilaRequest(
    [Required] Guid OcorrenciaId
);

/// <summary>
/// Resposta do webhook de priorização contendo o novo IPC, confirmação de notificação e fila reordenada.
/// </summary>
/// <param name="OcorrenciaId">Identificador da ocorrência reprocessada.</param>
/// <param name="NovoIpc">Novo valor do Índice de Prioridade de Coleta calculado.</param>
/// <param name="NotificacaoEnviada">Indica se o evento de notificação foi registrado com sucesso.</param>
/// <param name="Mensagem">Mensagem descritiva do resultado da priorização.</param>
/// <param name="Fila">Lista atualizada da fila ordenada por prioridade.</param>
public record PriorizarFilaResponse(
    Guid OcorrenciaId,
    double NovoIpc,
    bool NotificacaoEnviada,
    string Mensagem,
    IReadOnlyList<OcorrenciaResumoResponse> Fila
);
