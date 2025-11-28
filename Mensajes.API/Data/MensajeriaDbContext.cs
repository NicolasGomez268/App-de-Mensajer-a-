using Microsoft.EntityFrameworkCore;
using Mensajes.API.Models;

namespace Mensajes.API.Data;

public class MensajeriaDbContext : DbContext
{
    public MensajeriaDbContext(DbContextOptions<MensajeriaDbContext> options) : base(options)
    {
    }

    public DbSet<Mensaje> Mensajes { get; set; }
    public DbSet<Conversacion> Conversaciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar índices para Mensaje
        modelBuilder.Entity<Mensaje>()
            .HasIndex(m => m.RemitenteId);

        modelBuilder.Entity<Mensaje>()
            .HasIndex(m => m.DestinatarioId);

        modelBuilder.Entity<Mensaje>()
            .HasIndex(m => m.GrupoId);

        modelBuilder.Entity<Mensaje>()
            .HasIndex(m => m.FechaEnvio);

        // Configurar índices para Conversacion
        modelBuilder.Entity<Conversacion>()
            .HasIndex(c => new { c.Usuario1Id, c.Usuario2Id })
            .IsUnique();

        modelBuilder.Entity<Conversacion>()
            .HasIndex(c => c.UltimaActividad);
    }
}
