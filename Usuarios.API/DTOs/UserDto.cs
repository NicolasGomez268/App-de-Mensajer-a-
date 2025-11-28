namespace Usuarios.API.DTOs;

/// <summary>
/// DTO para respuesta de usuario
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string AuthId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Estado { get; set; } = "offline";
    public DateTime FechaCreacion { get; set; }
    public DateTime? UltimaConexion { get; set; }
}
