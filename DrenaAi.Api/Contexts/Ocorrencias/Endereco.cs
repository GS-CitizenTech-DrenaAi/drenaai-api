using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DrenaAi.Api.Contexts.Ocorrencias;

/// <summary>
/// Value Object (VO) que encapsula o endereço postal e as coordenadas geográficas de uma ocorrência.
/// </summary>
[Owned]
public class Endereco
{
    /// <summary>
    /// Logradouro e número do local da ocorrência (ex.: Rua Formosa, 95).
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("ENDERECO")]
    public string Logradouro { get; private set; } = string.Empty;

    /// <summary>
    /// Nome do bairro ou região da ocorrência (ex.: Vale do Anhangabaú).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("BAIRRO")]
    public string Bairro { get; private set; } = string.Empty;

    /// <summary>
    /// Coordenada de latitude geográfica (-90 a 90).
    /// </summary>
    [Column("LATITUDE")]
    public double Latitude { get; private set; }

    /// <summary>
    /// Coordenada de longitude geográfica (-180 a 180).
    /// </summary>
    [Column("LONGITUDE")]
    public double Longitude { get; private set; }

    /// <summary>
    /// Construtor protegido sem parâmetros exigido pelo Entity Framework Core.
    /// </summary>
    protected Endereco() { }

    /// <summary>
    /// Inicializa uma nova instância imutável do Value Object <see cref="Endereco"/>.
    /// </summary>
    /// <param name="logradouro">Rua e número do local.</param>
    /// <param name="bairro">Nome do bairro ou região.</param>
    /// <param name="latitude">Latitude geográfica (-90 a 90).</param>
    /// <param name="longitude">Longitude geográfica (-180 a 180).</param>
    public Endereco(string? logradouro, string bairro, double latitude, double longitude)
    {
        Logradouro = string.IsNullOrWhiteSpace(logradouro) ? "Local marcado no mapa" : logradouro.Trim();
        Bairro = string.IsNullOrWhiteSpace(bairro) ? "Vale do Anhangabaú" : bairro.Trim();
        Latitude = Math.Clamp(latitude, -90.0, 90.0);
        Longitude = Math.Clamp(longitude, -180.0, 180.0);
    }
}
