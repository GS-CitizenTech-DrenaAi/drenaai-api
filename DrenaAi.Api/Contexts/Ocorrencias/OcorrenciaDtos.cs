using System.ComponentModel.DataAnnotations;

namespace DrenaAi.Api.Contexts.Ocorrencias;

/// <summary>
/// Value Object de transferência (VO) que representa o endereço completo e geolocalizado de uma ocorrência.
/// </summary>
/// <param name="Logradouro">Rua e número do local da ocorrência (ex.: Rua Formosa, 95).</param>
/// <param name="Bairro">Bairro ou região da ocorrência (ex.: Vale do Anhangabaú).</param>
/// <param name="Latitude">Latitude geográfica (-90 a 90).</param>
/// <param name="Longitude">Longitude geográfica (-180 a 180).</param>
public record EnderecoVo(
    [Required] string Logradouro,
    [Required] string Bairro,
    [Range(-90, 90)] double Latitude,
    [Range(-180, 180)] double Longitude
);

/// <summary>
/// Value Object com a decomposição das três parcelas ponderadas que formam o Índice de Prioridade de Coleta (IPC).
/// </summary>
/// <param name="S">Parcela ponderada da Severidade (0.4 * S).</param>
/// <param name="C">Parcela ponderada da Chuva normalizada (0.3 * C).</param>
/// <param name="V">Parcela ponderada da Vulnerabilidade da via (0.3 * V).</param>
public record ParcelasIpcVo(
    double S,
    double C,
    double V
);

/// <summary>
/// Resposta retornada após o upload de uma foto de evidência da ocorrência.
/// </summary>
/// <param name="FotoUrl">URL HTTP pública gerada pelo servidor para acesso à imagem armazenada.</param>
public record UploadFotoResponse(
    string FotoUrl
);

/// <summary>
/// Payload de requisição para criação de uma nova ocorrência enviada pelo aplicativo móvel.
/// </summary>
/// <param name="Tipo">Categoria do problema reportado pelo morador.</param>
/// <param name="Descricao">Relato descritivo opcional que passará pelo filtro de sanitização de PII.</param>
/// <param name="FotoUrl">URL ou identificador da foto capturada.</param>
/// <param name="Latitude">Latitude geográfica obtida via GPS (-90 a 90).</param>
/// <param name="Longitude">Longitude geográfica obtida via GPS (-180 a 180).</param>
/// <param name="Bairro">Bairro onde a ocorrência foi identificada.</param>
/// <param name="Endereco">Value Object opcional contendo o endereço completo (Logradouro, Bairro, Latitude e Longitude).</param>
public record CriarOcorrenciaRequest(
    [Required] string Tipo,
    [MaxLength(500)] string? Descricao,
    string? FotoUrl,
    [Range(-90, 90)] double Latitude = 0.0,
    [Range(-180, 180)] double Longitude = 0.0,
    string Bairro = "Vale do Anhangabaú",
    EnderecoVo? Endereco = null
);

/// <summary>
/// Resposta retornada após o registro de uma nova ocorrência pelo aplicativo móvel.
/// </summary>
/// <param name="Id">Identificador único da ocorrência criada.</param>
/// <param name="Severidade">Severidade avaliada pela visão computacional (0.0 a 1.0).</param>
/// <param name="Ipc">Índice de Prioridade de Coleta calculado (0.0 a 1.0).</param>
/// <param name="Status">Status inicial da ocorrência na fila.</param>
/// <param name="Descricao">Descrição já sanitizada contra exposição de PII.</param>
/// <param name="FotoUrl">URL da foto armazenada para a ocorrência.</param>
/// <param name="Endereco">Value Object contendo o endereço e coordenadas salvas.</param>
/// <param name="Confianca">Grau de confiança da inferência da IA (0.0 a 1.0).</param>
/// <param name="Justificativa">Explicação técnica gerada pela IA para a severidade.</param>
/// <param name="ParcelasIpc">Decomposição das parcelas S, C e V do IPC.</param>
/// <param name="PosicaoFila">Posição atual da ocorrência na fila de coleta do bairro.</param>
/// <param name="CriadaEm">Data e hora (UTC) de criação do registro.</param>
public record CriarOcorrenciaResponse(
    Guid Id,
    double Severidade,
    double Ipc,
    string Status,
    string Descricao,
    string? FotoUrl = null,
    EnderecoVo? Endereco = null,
    double? Confianca = null,
    string? Justificativa = null,
    ParcelasIpcVo? ParcelasIpc = null,
    int? PosicaoFila = null,
    DateTime? CriadaEm = null
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
/// <param name="Endereco">Value Object contendo o endereço completo e coordenadas.</param>
/// <param name="FotoUrl">URL da foto associada à ocorrência.</param>
/// <param name="PosicaoFila">Posição atual na fila de coleta.</param>
public record OcorrenciaResumoResponse(
    Guid Id,
    string Origem,
    string Tipo,
    string Descricao,
    string Bairro,
    double Severidade,
    double Ipc,
    string Status,
    DateTime CriadaEm,
    EnderecoVo? Endereco = null,
    string? FotoUrl = null,
    int? PosicaoFila = null
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
/// Representação detalhada de uma ocorrência incluindo sua linha do tempo de status e Value Object de endereço.
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
/// <param name="Endereco">Value Object com logradouro, bairro, latitude e longitude.</param>
/// <param name="Confianca">Confiança da avaliação por visão computacional (0.0 a 1.0).</param>
/// <param name="Justificativa">Justificativa técnica da avaliação de severidade.</param>
/// <param name="ParcelasIpc">Parcelas ponderadas S, C e V do cálculo do IPC.</param>
/// <param name="PosicaoFila">Posição atual da ocorrência na fila de prioridade do bairro.</param>
/// <param name="Confirmacoes">Quantidade de confirmações comunitárias recebidas.</param>
/// <param name="UsuarioId">Identificador opcional do morador que abriu a ocorrência.</param>
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
    IReadOnlyList<HistoricoStatusItemResponse> HistoricoStatus,
    EnderecoVo? Endereco = null,
    double? Confianca = null,
    string? Justificativa = null,
    ParcelasIpcVo? ParcelasIpc = null,
    int? PosicaoFila = null,
    int Confirmacoes = 0,
    Guid? UsuarioId = null
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

/// <summary>
/// Dados de entrada do webhook ou rota administrativa de atualização de status de uma ocorrência.
/// </summary>
/// <param name="NovoStatus">Novo status a ser atribuído (VALIDADA, NA_FILA, EM_ATENDIMENTO ou CONCLUIDA).</param>
public record AtualizarStatusOcorrenciaRequest(
    [Required(ErrorMessage = "O novo status da ocorrência é obrigatório.")]
    StatusOcorrencia NovoStatus
);
