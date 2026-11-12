using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrenaAi.Api.Contexts.Ocorrencias;

/// <summary>
/// Representa um registro cronológico de alteração de status de uma ocorrência.
/// </summary>
[Table("TB_DRENA_HISTORICO_STATUS")]
public class HistoricoStatus
{
    /// <summary>
    /// Identificador único do registro de histórico.
    /// </summary>
    [Key]
    [Column("ID_HISTORICO")]
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Identificador da ocorrência associada a este registro de histórico.
    /// </summary>
    [Required]
    [Column("ID_OCORRENCIA")]
    public Guid OcorrenciaId { get; private set; }

    /// <summary>
    /// Status atribuído à ocorrência neste ponto da linha do tempo.
    /// </summary>
    [Required]
    [Column("STATUS")]
    public StatusOcorrencia Status { get; private set; }

    /// <summary>
    /// Data e hora (UTC) em que a transição de status ocorreu.
    /// </summary>
    [Required]
    [Column("DATA_HORA")]
    public DateTime DataHora { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Construtor protegido sem parâmetros exigido pelo Entity Framework Core.
    /// </summary>
    protected HistoricoStatus() { }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="HistoricoStatus"/> para uma ocorrência.
    /// </summary>
    /// <param name="ocorrenciaId">Identificador da ocorrência relacionada.</param>
    /// <param name="status">Status registrado na linha do tempo.</param>
    public HistoricoStatus(Guid ocorrenciaId, StatusOcorrencia status)
    {
        OcorrenciaId = ocorrenciaId;
        Status = status;
        DataHora = DateTime.UtcNow;
    }
}
