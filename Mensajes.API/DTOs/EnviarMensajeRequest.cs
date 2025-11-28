using System.ComponentModel.DataAnnotations;

namespace Mensajes.API.DTOs;

public class EnviarMensajeRequest
{
    [Required]
    [MaxLength(100)]
    public string DestinatarioId { get; set; } = string.Empty;

    public Guid? GrupoId { get; set; }

    [Required]
    [MaxLength(5000)]
    public string Contenido { get; set; } = string.Empty;

    [MaxLength(20)]
    public string TipoMensaje { get; set; } = "texto";

    [MaxLength(500)]
    public string? ArchivoUrl { get; set; }
}
