namespace Usuarios.API.Models;

/// <summary>
/// Modelo de perfil de usuario
/// </summary>
public class Perfil
{
    /// <summary>
    /// ID único del perfil
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID de autenticación de Supabase Auth
    /// </summary>
    public string AuthId { get; set; } = string.Empty;

    /// <summary>
    /// Email del usuario
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del usuario
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// URL del avatar del usuario
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Estado del usuario (online, offline, ausente)
    /// </summary>
    public string Estado { get; set; } = "offline";

    /// <summary>
    /// Fecha de creación del perfil
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Última conexión del usuario
    /// </summary>
    public DateTime? UltimaConexion { get; set; }
}
