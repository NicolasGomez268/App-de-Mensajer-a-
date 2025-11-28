namespace Grupos.API.DTOs;

public class UserInfoDto
{
    public Guid Id { get; set; }
    public string AuthId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Estado { get; set; } = "offline";
}
