using Microsoft.EntityFrameworkCore;
using Usuarios.API.Models;

namespace Usuarios.API.Data;

/// <summary>
/// DbContext para la base de datos de usuarios
/// </summary>
public class UsuariosDbContext : DbContext
{
    public UsuariosDbContext(DbContextOptions<UsuariosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Perfil> Perfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar esquema usuarios
        modelBuilder.HasDefaultSchema("usuarios");

        // Configuración de la tabla Perfiles
        modelBuilder.Entity<Perfil>(entity =>
        {
            entity.ToTable("perfiles");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.AuthId)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.AuthId)
                .IsUnique();

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500);

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("offline");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("NOW()");
        });
    }
}
