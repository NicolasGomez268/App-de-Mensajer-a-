namespace Grupos.API.DTOs;

public class GrupoResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<MiembroGrupoResponseDto> Miembros { get; set; } = new();
}

public class MiembroGrupoResponseDto
{
    public Guid Id { get; set; }
    public Guid GrupoId { get; set; }
    public Guid UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = "Usuario";
    public string EmailUsuario { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
}
