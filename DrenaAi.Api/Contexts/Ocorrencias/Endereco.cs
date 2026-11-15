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

    /// <summary>
    /// Verifica se outro endereço representa o mesmo local físico (mesmo logradouro/bairro ou dentro do raio informado).
    /// </summary>
    /// <param name="outro">Outro endereço para comparação.</param>
    /// <param name="raioMetros">Raio máximo em metros para considerar proximidade geográfica.</param>
    /// <returns>Verdadeiro caso ambos representem o mesmo local.</returns>
    public bool MesmoLocal(Endereco? outro, double raioMetros = 50.0)
    {
        if (outro is null)
            return false;

        var mesmoBairro = string.Equals(Bairro.Trim(), outro.Bairro.Trim(), StringComparison.OrdinalIgnoreCase);
        var logradouroEspecifico =
            !string.Equals(Logradouro.Trim(), "Local marcado no mapa", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(outro.Logradouro.Trim(), "Local marcado no mapa", StringComparison.OrdinalIgnoreCase);

        if (mesmoBairro && logradouroEspecifico &&
            string.Equals(Logradouro.Trim(), outro.Logradouro.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return DistanciaMetros(outro) <= raioMetros;
    }

    /// <summary>
    /// Calcula a distância aproximada em metros entre dois pontos geográficos usando a fórmula de Haversine.
    /// </summary>
    /// <param name="outro">Endereço de destino para o cálculo.</param>
    /// <returns>Distância em metros.</returns>
    public double DistanciaMetros(Endereco outro)
    {
        const double raioTerraMetros = 6371000.0;
        var dLat = (outro.Latitude - Latitude) * Math.PI / 180.0;
        var dLon = (outro.Longitude - Longitude) * Math.PI / 180.0;
        var lat1 = Latitude * Math.PI / 180.0;
        var lat2 = outro.Latitude * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return 2 * raioTerraMetros * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
    }
}
