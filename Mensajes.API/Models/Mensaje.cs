using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mensajes.API.Models;

[Table("mensajes", Schema = "mensajeria")]
public class Mensaje
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string RemitenteId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DestinatarioId { get; set; } = string.Empty;

    public Guid? GrupoId { get; set; }

    [Required]
    [MaxLength(5000)]
    public string Contenido { get; set; } = string.Empty;

    [MaxLength(20)]
    public string TipoMensaje { get; set; } = "texto"; // texto, imagen, archivo

    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

    public bool Leido { get; set; } = false;

    public DateTime? FechaLectura { get; set; }

    [MaxLength(500)]
    public string? ArchivoUrl { get; set; }
}
