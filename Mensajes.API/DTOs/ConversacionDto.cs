namespace Mensajes.API.DTOs;

public class ConversacionDto
{
    public Guid Id { get; set; }
    public string OtroUsuarioId { get; set; } = string.Empty;
    public string OtroUsuarioNombre { get; set; } = string.Empty;
    public string? OtroUsuarioAvatar { get; set; }
    public string? OtroUsuarioEstado { get; set; }
    public DateTime UltimaActividad { get; set; }
    public MensajeDto? UltimoMensaje { get; set; }
    public int MensajesNoLeidos { get; set; }
}
