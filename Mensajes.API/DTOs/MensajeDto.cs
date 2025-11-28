namespace Mensajes.API.DTOs;

public class MensajeDto
{
    public Guid Id { get; set; }
    public string RemitenteId { get; set; } = string.Empty;
    public string RemitenteNombre { get; set; } = string.Empty;
    public string? RemitenteAvatar { get; set; }
    public string DestinatarioId { get; set; } = string.Empty;
    public Guid? GrupoId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public string TipoMensaje { get; set; } = "texto";
    public DateTime FechaEnvio { get; set; }
    public bool Leido { get; set; }
    public DateTime? FechaLectura { get; set; }
    public string? ArchivoUrl { get; set; }
}
