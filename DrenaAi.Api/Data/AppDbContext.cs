using DrenaAi.Api.Contexts.Ocorrencias;
using DrenaAi.Api.Contexts.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace DrenaAi.Api.Data;

/// <summary>
/// Contexto principal do Entity Framework Core para persistência das entidades do DrenaAi no Oracle Database.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="AppDbContext"/> com as opções configuradas.
    /// </summary>
    /// <param name="options">Opções de configuração do contexto de banco de dados.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Conjunto de moradores cadastrados no sistema.
    /// </summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>
    /// Conjunto de ocorrências de obstrução e descarte irregular registradas.
    /// </summary>
    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();

    /// <summary>
    /// Conjunto de registros históricos de transição de status das ocorrências.
    /// </summary>
    public DbSet<HistoricoStatus> HistoricosStatus => Set<HistoricoStatus>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Ocorrencia>(entity =>
        {
            entity.Property(o => o.Origem)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasMany(o => o.HistoricoStatus)
                .WithOne()
                .HasForeignKey(h => h.OcorrenciaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HistoricoStatus>(entity =>
        {
            entity.Property(h => h.Status)
                .HasConversion<string>()
                .HasMaxLength(30);
        });
    }
}
