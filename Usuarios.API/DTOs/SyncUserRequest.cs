namespace Usuarios.API.DTOs;

/// <summary>
/// Request para sincronizar usuario desde Supabase Auth
/// </summary>
public class SyncUserRequest
{
    public string AuthId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
