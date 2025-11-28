namespace Grupos.API.DTOs;

public class CrearGrupoDto
{
    public string Nombre { get; set; } = string.Empty;
    public List<Guid> MiembrosIds { get; set; } = new();
}
