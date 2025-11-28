namespace Grupos.API.Models;

public class Grupo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    
    public ICollection<MiembroGrupo> Miembros { get; set; } = new List<MiembroGrupo>();
}
