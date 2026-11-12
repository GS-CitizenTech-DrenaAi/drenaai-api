using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrenaAi.Api.Contexts.Usuarios;

/// <summary>
/// Representa um morador cadastrado no sistema DrenaAi para registro e acompanhamento de ocorrências.
/// </summary>
[Table("TB_DRENA_USUARIO")]
public class Usuario
{
    /// <summary>
    /// Identificador único do usuário.
    /// </summary>
    [Key]
    [Column("ID_USUARIO")]
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Nome completo do morador.
    /// </summary>
    [Required]
    [MaxLength(120)]
    [Column("NOME")]
    public string Nome { get; private set; } = string.Empty;

    /// <summary>
    /// Endereço de e-mail único utilizado para autenticação.
    /// </summary>
    [Required]
    [MaxLength(180)]
    [Column("EMAIL")]
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Hash criptográfico da senha do usuário (nunca armazenado em texto puro).
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("SENHA_HASH")]
    public string SenhaHash { get; private set; } = string.Empty;

    /// <summary>
    /// Data e hora (UTC) de criação do cadastro do usuário.
    /// </summary>
    [Required]
    [Column("CRIADO_EM")]
    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Construtor protegido sem parâmetros exigido pelo Entity Framework Core.
    /// </summary>
    protected Usuario() { }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="Usuario"/> com os dados mínimos obrigatórios.
    /// </summary>
    /// <param name="nome">Nome do morador.</param>
    /// <param name="email">E-mail normalizado do morador.</param>
    /// <param name="senhaHash">Hash seguro da senha do morador.</param>
    public Usuario(string nome, string email, string senhaHash)
    {
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        CriadoEm = DateTime.UtcNow;
    }
}
