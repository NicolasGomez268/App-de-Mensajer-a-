using Microsoft.EntityFrameworkCore;
using Grupos.API.Models;

namespace Grupos.API.Data;

public class GruposDbContext : DbContext
{
    public GruposDbContext(DbContextOptions<GruposDbContext> options) : base(options) { }

    public DbSet<Grupo> Grupos { get; set; }
    public DbSet<MiembroGrupo> MiembrosGrupo { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ⚠️ CRÍTICO: Usar el schema "grupos"
        modelBuilder.HasDefaultSchema("grupos");

        modelBuilder.Entity<MiembroGrupo>()
            .HasIndex(m => new { m.GrupoId, m.UsuarioId })
            .IsUnique();
    }
}
