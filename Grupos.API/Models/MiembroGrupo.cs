namespace Grupos.API.Models;

public class MiembroGrupo
{
    public Guid Id { get; set; }
    public Guid GrupoId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
}
