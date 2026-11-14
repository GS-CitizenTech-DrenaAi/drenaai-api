using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrenaAi.Api.Contexts.Ocorrencias;

/// <summary>
/// Representa uma ocorrência de descarte irregular de resíduos ou obstrução de bueiro registrada no sistema.
/// </summary>
[Table("TB_DRENA_OCORRENCIA")]
public class Ocorrencia
{
    /// <summary>
    /// Identificador único da ocorrência.
    /// </summary>
    [Key]
    [Column("ID_OCORRENCIA")]
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Origem do registro da ocorrência (APP do morador ou SENSOR IoT ESP32).
    /// </summary>
    [Required]
    [Column("ORIGEM")]
    public OrigemOcorrencia Origem { get; private set; }

    /// <summary>
    /// Categoria ou tipo da ocorrência (ex.: BUEIRO_ENTUPIDO, ENTULHO, SACOS_LIXO).
    /// </summary>
    [Required]
    [MaxLength(80)]
    [Column("TIPO")]
    public string Tipo { get; private set; } = string.Empty;

    /// <summary>
    /// Descrição do relato já processada e sanitizada pelo filtro de dados pessoais (PII).
    /// </summary>
    [MaxLength(500)]
    [Column("DESCRICAO_SANITIZADA")]
    public string Descricao { get; private set; } = string.Empty;

    /// <summary>
    /// URL ou caminho da foto enviada pelo morador (nulo quando a origem é SENSOR).
    /// </summary>
    [MaxLength(300)]
    [Column("FOTO_URL")]
    public string? FotoUrl { get; private set; }

    /// <summary>
    /// Value Object contendo o endereço postal (logradouro, bairro) e as coordenadas geográficas da ocorrência.
    /// </summary>
    [Required]
    public Endereco Localizacao { get; private set; } = null!;

    /// <summary>
    /// Atalho de leitura para o logradouro do Value Object <see cref="Localizacao"/>.
    /// </summary>
    [NotMapped]
    public string Logradouro => Localizacao?.Logradouro ?? string.Empty;

    /// <summary>
    /// Atalho de leitura para a latitude geográfica do Value Object <see cref="Localizacao"/>.
    /// </summary>
    [NotMapped]
    public double Latitude => Localizacao?.Latitude ?? 0.0;

    /// <summary>
    /// Atalho de leitura para a longitude geográfica do Value Object <see cref="Localizacao"/>.
    /// </summary>
    [NotMapped]
    public double Longitude => Localizacao?.Longitude ?? 0.0;

    /// <summary>
    /// Atalho de leitura para o nome do bairro do Value Object <see cref="Localizacao"/>.
    /// </summary>
    [NotMapped]
    public string Bairro => Localizacao?.Bairro ?? string.Empty;

    /// <summary>
    /// Grau de severidade da ocorrência normalizado na escala de 0.0 a 1.0.
    /// </summary>
    [Column("SEVERIDADE")]
    public double Severidade { get; private set; }

    /// <summary>
    /// Status atual do atendimento da ocorrência na fila de coleta.
    /// </summary>
    [Required]
    [Column("STATUS")]
    public StatusOcorrencia Status { get; private set; }

    /// <summary>
    /// Índice de Prioridade de Coleta (IPC) calculado na escala de 0.0 a 1.0.
    /// </summary>
    [Column("IPC")]
    public double Ipc { get; private set; }

    /// <summary>
    /// Data e hora (UTC) em que a ocorrência foi registrada.
    /// </summary>
    [Column("CRIADA_EM")]
    public DateTime CriadaEm { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Identificador do usuário que registrou a denúncia (nulo quando a origem é SENSOR).
    /// </summary>
    [Column("ID_USUARIO")]
    public Guid? UsuarioId { get; private set; }

    /// <summary>
    /// Coleção com o histórico cronológico de mudanças de status da ocorrência.
    /// </summary>
    public ICollection<HistoricoStatus> HistoricoStatus { get; private set; } = new List<HistoricoStatus>();

    /// <summary>
    /// Construtor protegido sem parâmetros exigido pelo Entity Framework Core.
    /// </summary>
    protected Ocorrencia() { }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="Ocorrencia"/> recebendo o Value Object <see cref="Endereco"/>.
    /// </summary>
    /// <param name="origem">Origem da ocorrência (APP ou SENSOR).</param>
    /// <param name="tipo">Tipo da ocorrência reportada.</param>
    /// <param name="descricaoSanitizada">Texto descritivo livre de dados pessoais (PII).</param>
    /// <param name="fotoUrl">URL da foto da denúncia, caso exista.</param>
    /// <param name="localizacao">Value Object contendo logradouro, bairro, latitude e longitude.</param>
    /// <param name="severidade">Severidade avaliada entre 0.0 e 1.0.</param>
    /// <param name="ipc">Índice de Prioridade de Coleta inicial entre 0.0 e 1.0.</param>
    /// <param name="usuarioId">Identificador opcional do morador autor da denúncia.</param>
    public Ocorrencia(
        OrigemOcorrencia origem,
        string tipo,
        string descricaoSanitizada,
        string? fotoUrl,
        Endereco localizacao,
        double severidade,
        double ipc,
        Guid? usuarioId = null)
    {
        Origem = origem;
        Tipo = tipo;
        Descricao = descricaoSanitizada;
        FotoUrl = fotoUrl;
        Localizacao = localizacao;
        Severidade = Math.Clamp(severidade, 0.0, 1.0);
        Ipc = Math.Clamp(ipc, 0.0, 1.0);
        UsuarioId = usuarioId;

        AtualizarStatus(StatusOcorrencia.NA_FILA);
    }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="Ocorrencia"/> aplicando limites seguros de severidade e IPC.
    /// </summary>
    /// <param name="origem">Origem da ocorrência (APP ou SENSOR).</param>
    /// <param name="tipo">Tipo da ocorrência reportada.</param>
    /// <param name="descricaoSanitizada">Texto descritivo livre de dados pessoais (PII).</param>
    /// <param name="fotoUrl">URL da foto da denúncia, caso exista.</param>
    /// <param name="latitude">Latitude geográfica.</param>
    /// <param name="longitude">Longitude geográfica.</param>
    /// <param name="bairro">Nome do bairro da ocorrência.</param>
    /// <param name="severidade">Severidade avaliada entre 0.0 e 1.0.</param>
    /// <param name="ipc">Índice de Prioridade de Coleta inicial entre 0.0 e 1.0.</param>
    /// <param name="usuarioId">Identificador opcional do morador autor da denúncia.</param>
    /// <param name="logradouro">Logradouro opcional da ocorrência.</param>
    public Ocorrencia(
        OrigemOcorrencia origem,
        string tipo,
        string descricaoSanitizada,
        string? fotoUrl,
        double latitude,
        double longitude,
        string bairro,
        double severidade,
        double ipc,
        Guid? usuarioId = null,
        string? logradouro = null)
        : this(
            origem,
            tipo,
            descricaoSanitizada,
            fotoUrl,
            new Endereco(logradouro, bairro, latitude, longitude),
            severidade,
            ipc,
            usuarioId)
    {
    }

    /// <summary>
    /// Atualiza o status da ocorrência e adiciona automaticamente um registro na linha do tempo.
    /// </summary>
    /// <param name="novoStatus">Novo status a ser atribuído à ocorrência.</param>
    /// <returns>Registro de histórico criado para esta transição de status.</returns>
    public HistoricoStatus AtualizarStatus(StatusOcorrencia novoStatus)
    {
        Status = novoStatus;
        var registroHistorico = new HistoricoStatus(Id, novoStatus);
        HistoricoStatus.Add(registroHistorico);
        return registroHistorico;
    }

    /// <summary>
    /// Atualiza o valor do Índice de Prioridade de Coleta (IPC) mantendo o intervalo entre 0.0 e 1.0.
    /// </summary>
    /// <param name="novoIpc">Novo valor calculado para o IPC.</param>
    public void RecalcularIpc(double novoIpc)
    {
        Ipc = Math.Clamp(novoIpc, 0.0, 1.0);
    }
}
