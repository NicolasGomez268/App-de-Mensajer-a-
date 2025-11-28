using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mensajes.API.Models;

[Table("conversaciones", Schema = "mensajeria")]
public class Conversacion
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Usuario1Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Usuario2Id { get; set; } = string.Empty;

    public DateTime UltimaActividad { get; set; } = DateTime.UtcNow;

    public Guid? UltimoMensajeId { get; set; }
}
