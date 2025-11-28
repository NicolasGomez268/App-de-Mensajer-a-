using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Mensajes.API.Data;
using Mensajes.API.DTOs;
using Mensajes.API.Models;
using Mensajes.API.Hubs;

namespace Mensajes.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly MensajeriaDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<MessagesController> _logger;
    private readonly HttpClient _httpClient;

    public MessagesController(
        MensajeriaDbContext context,
        IHubContext<ChatHub> hubContext,
        ILogger<MessagesController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
    }

    /// <summary>
    /// Enviar un mensaje a otro usuario
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MensajeDto>> SendMessage([FromBody] EnviarMensajeRequest request)
    {
        var remitenteId = GetCurrentUserId();
        if (string.IsNullOrEmpty(remitenteId))
        {
            return Unauthorized("No se pudo obtener el ID del usuario");
        }

        // Resolver el AuthId del destinatario
        // El frontend envía el ID interno, pero necesitamos el AuthId para consistencia en la BD y SignalR
        var destinatarioId = request.DestinatarioId;
        if (!request.GrupoId.HasValue)
        {
            var destinatarioInfo = await GetUserInfo(request.DestinatarioId);
            if (destinatarioInfo != null && !string.IsNullOrEmpty(destinatarioInfo.AuthId))
            {
                destinatarioId = destinatarioInfo.AuthId;
            }
        }

        // Crear el mensaje
        var mensaje = new Mensaje
        {
            Id = Guid.NewGuid(),
            RemitenteId = remitenteId,
            DestinatarioId = destinatarioId, // Usar AuthId
            GrupoId = request.GrupoId,
            Contenido = request.Contenido,
            TipoMensaje = request.TipoMensaje,
            ArchivoUrl = request.ArchivoUrl,
            FechaEnvio = DateTime.UtcNow,
            Leido = false
        };

        _context.Mensajes.Add(mensaje);

        // Crear o actualizar conversación (solo si es mensaje directo)
        if (!request.GrupoId.HasValue)
        {
            var conversacion = await _context.Conversaciones
                .FirstOrDefaultAsync(c =>
                    (c.Usuario1Id == remitenteId && c.Usuario2Id == destinatarioId) ||
                    (c.Usuario1Id == destinatarioId && c.Usuario2Id == remitenteId));

            if (conversacion == null)
            {
                conversacion = new Conversacion
                {
                    Id = Guid.NewGuid(),
                    Usuario1Id = remitenteId,
                    Usuario2Id = destinatarioId, // Usar AuthId
                    UltimaActividad = DateTime.UtcNow,
                    UltimoMensajeId = mensaje.Id
                };
                _context.Conversaciones.Add(conversacion);
            }
            else
            {
                conversacion.UltimaActividad = DateTime.UtcNow;
                conversacion.UltimoMensajeId = mensaje.Id;
            }
        }

        await _context.SaveChangesAsync();

        // Enviar notificación via SignalR
        _logger.LogInformation($"[DEBUG] Intentando enviar notificación SignalR. GrupoId: {request.GrupoId}, DestinatarioId: {destinatarioId}");
        
        if (request.GrupoId.HasValue)
        {
             await _hubContext.Clients.Group(request.GrupoId.Value.ToString()).SendAsync("ReceiveMessage", new
            {
                id = mensaje.Id,
                remitenteId = mensaje.RemitenteId,
                destinatarioId = mensaje.GrupoId, // Para el cliente, el "destinatario" es el grupo
                grupoId = mensaje.GrupoId,
                contenido = mensaje.Contenido,
                tipoMensaje = mensaje.TipoMensaje,
                fechaEnvio = mensaje.FechaEnvio,
                leido = mensaje.Leido
            });
            _logger.LogInformation($"[DEBUG] Notificación SignalR enviada al grupo {request.GrupoId}");
        }
        else
        {
            // Ya tenemos el AuthId en destinatarioId
            await _hubContext.Clients.User(destinatarioId).SendAsync("ReceiveMessage", new
            {
                id = mensaje.Id,
                remitenteId = mensaje.RemitenteId,
                destinatarioId = mensaje.DestinatarioId,
                contenido = mensaje.Contenido,
                tipoMensaje = mensaje.TipoMensaje,
                fechaEnvio = mensaje.FechaEnvio,
                leido = mensaje.Leido
            });
            _logger.LogInformation($"[DEBUG] Notificación SignalR enviada al usuario {destinatarioId}");

        }

        _logger.LogInformation($"Mensaje enviado de {remitenteId} a {destinatarioId}");

        return Ok(new MensajeDto
        {
            Id = mensaje.Id,
            RemitenteId = mensaje.RemitenteId,
            DestinatarioId = mensaje.DestinatarioId,
            GrupoId = mensaje.GrupoId,
            Contenido = mensaje.Contenido,
            TipoMensaje = mensaje.TipoMensaje,
            FechaEnvio = mensaje.FechaEnvio,
            Leido = mensaje.Leido,
            ArchivoUrl = mensaje.ArchivoUrl
        });
    }

    /// <summary>
    /// Obtener todas las conversaciones del usuario actual
    /// </summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversacionDto>>> GetConversations()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var conversaciones = await _context.Conversaciones
            .Where(c => c.Usuario1Id == userId || c.Usuario2Id == userId)
            .OrderByDescending(c => c.UltimaActividad)
            .ToListAsync();

        var conversacionesDto = new List<ConversacionDto>();

        foreach (var conv in conversaciones)
        {
            var otroUsuarioId = conv.Usuario1Id == userId ? conv.Usuario2Id : conv.Usuario1Id;

            // Obtener info del otro usuario desde Usuarios.API
            var usuarioInfo = await GetUserInfo(otroUsuarioId);

            // Obtener último mensaje
            MensajeDto? ultimoMensaje = null;
            if (conv.UltimoMensajeId.HasValue)
            {
                var msg = await _context.Mensajes.FindAsync(conv.UltimoMensajeId.Value);
                if (msg != null)
                {
                    ultimoMensaje = new MensajeDto
                    {
                        Id = msg.Id,
                        RemitenteId = msg.RemitenteId,
                        DestinatarioId = msg.DestinatarioId,
                        Contenido = msg.Contenido,
                        TipoMensaje = msg.TipoMensaje,
                        FechaEnvio = msg.FechaEnvio,
                        Leido = msg.Leido
                    };
                }
            }

            // Contar mensajes no leídos
            var noLeidos = await _context.Mensajes
                .CountAsync(m => m.DestinatarioId == userId && m.RemitenteId == otroUsuarioId && !m.Leido);

            conversacionesDto.Add(new ConversacionDto
            {
                Id = conv.Id,
                OtroUsuarioId = otroUsuarioId,
                OtroUsuarioNombre = usuarioInfo?.Nombre ?? "Usuario",
                OtroUsuarioAvatar = usuarioInfo?.AvatarUrl,
                OtroUsuarioEstado = usuarioInfo?.Estado ?? "offline",
                UltimaActividad = conv.UltimaActividad,
                UltimoMensaje = ultimoMensaje,
                MensajesNoLeidos = noLeidos
            });
        }

        return Ok(conversacionesDto);
    }

    /// <summary>
    /// Obtener mensajes de una conversación con otro usuario
    /// </summary>
    [HttpGet("conversation/{otroUsuarioId}")]
    public async Task<ActionResult<List<MensajeDto>>> GetConversationMessages(string otroUsuarioId, [FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var mensajes = await _context.Mensajes
            .Where(m =>
                (m.RemitenteId == userId && m.DestinatarioId == otroUsuarioId) ||
                (m.RemitenteId == otroUsuarioId && m.DestinatarioId == userId))
            .OrderByDescending(m => m.FechaEnvio)
            .Take(limit)
            .ToListAsync();

        var mensajesDto = mensajes.Select(m => new MensajeDto
        {
            Id = m.Id,
            RemitenteId = m.RemitenteId,
            DestinatarioId = m.DestinatarioId,
            GrupoId = m.GrupoId,
            Contenido = m.Contenido,
            TipoMensaje = m.TipoMensaje,
            FechaEnvio = m.FechaEnvio,
            Leido = m.Leido,
            FechaLectura = m.FechaLectura,
            ArchivoUrl = m.ArchivoUrl
        }).Reverse().ToList();

        return Ok(mensajesDto);
    }

    /// <summary>
    /// Obtener mensajes de un grupo
    /// </summary>
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<List<MensajeDto>>> GetGroupMessages(string groupId, [FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // TODO: Verificar si el usuario pertenece al grupo (llamando a Grupos.API o confiando en el cliente por ahora)
        // Por simplicidad y rapidez, confiamos en que si tiene el ID es porque puede verlo, 
        // pero idealmente deberíamos validar membresía.

        if (!Guid.TryParse(groupId, out var groupGuid))
        {
            return BadRequest("ID de grupo inválido");
        }

        var mensajes = await _context.Mensajes
            .Where(m => m.GrupoId == groupGuid)
            .OrderByDescending(m => m.FechaEnvio)
            .Take(limit)
            .ToListAsync();

        // Obtener información de los remitentes para mostrar nombres
        var remitentesIds = mensajes.Select(m => m.RemitenteId).Distinct().ToList();
        var remitentesInfo = new Dictionary<string, UserInfoDto>();

        foreach (var id in remitentesIds)
        {
            var info = await GetUserInfo(id);
            if (info != null)
            {
                remitentesInfo[id] = info;
            }
        }

        var mensajesDto = mensajes.Select(m => new MensajeDto
        {
            Id = m.Id,
            RemitenteId = m.RemitenteId,
            RemitenteNombre = remitentesInfo.ContainsKey(m.RemitenteId) ? remitentesInfo[m.RemitenteId].Nombre : "Usuario",
            RemitenteAvatar = remitentesInfo.ContainsKey(m.RemitenteId) ? remitentesInfo[m.RemitenteId].AvatarUrl : null,
            DestinatarioId = m.DestinatarioId,
            GrupoId = m.GrupoId,
            Contenido = m.Contenido,
            TipoMensaje = m.TipoMensaje,
            FechaEnvio = m.FechaEnvio,
            Leido = m.Leido,
            FechaLectura = m.FechaLectura,
            ArchivoUrl = m.ArchivoUrl
        }).Reverse().ToList();

        return Ok(mensajesDto);
    }

    /// <summary>
    /// Marcar mensaje como leído
    /// </summary>
    [HttpPatch("{mensajeId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid mensajeId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var mensaje = await _context.Mensajes.FindAsync(mensajeId);
        if (mensaje == null)
        {
            return NotFound();
        }

        // Si es mensaje directo, verificar que soy el destinatario
        if (!mensaje.GrupoId.HasValue && mensaje.DestinatarioId != userId)
        {
            return Forbid();
        }
        
        // Si es mensaje de grupo, por ahora permitimos que cualquiera lo marque como leído
        // TODO: Implementar tabla de lecturas por usuario para grupos

        // Evitar marcar como leído si ya lo está (optimización)
        if (mensaje.Leido) return NoContent();

        mensaje.Leido = true;
        mensaje.FechaLectura = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Notificar al remitente via SignalR
        await _hubContext.Clients.User(mensaje.RemitenteId).SendAsync("MessageRead", new
        {
            mensajeId = mensaje.Id,
            lectorId = userId,
            fechaLectura = mensaje.FechaLectura
        });

        return NoContent();
    }

    /// <summary>
    /// Marcar todos los mensajes de una conversación como leídos
    /// </summary>
    [HttpPatch("conversation/{otroUsuarioId}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(string otroUsuarioId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var mensajes = await _context.Mensajes
            .Where(m => m.RemitenteId == otroUsuarioId && m.DestinatarioId == userId && !m.Leido)
            .ToListAsync();

        foreach (var mensaje in mensajes)
        {
            mensaje.Leido = true;
            mensaje.FechaLectura = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Notificar al remitente
        await _hubContext.Clients.User(otroUsuarioId).SendAsync("ConversationRead", new
        {
            lectorId = userId,
            cantidadMensajes = mensajes.Count
        });

        return NoContent();
    }

    /// <summary>
    /// Eliminar un mensaje
    /// </summary>
    [HttpDelete("{mensajeId}")]
    public async Task<IActionResult> DeleteMessage(Guid mensajeId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var mensaje = await _context.Mensajes.FindAsync(mensajeId);
        if (mensaje == null)
        {
            return NotFound();
        }

        if (mensaje.RemitenteId != userId)
        {
            return Forbid();
        }

        _context.Mensajes.Remove(mensaje);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Eliminar una conversación completa
    /// </summary>
    [HttpDelete("conversation/{conversationId}")]
    public async Task<IActionResult> DeleteConversation(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var conversacion = await _context.Conversaciones.FindAsync(conversationId);
        if (conversacion == null)
        {
            return NotFound();
        }

        // Verificar que el usuario pertenece a la conversación
        if (conversacion.Usuario1Id != userId && conversacion.Usuario2Id != userId)
        {
            return Forbid();
        }

        // Identificar al otro usuario
        var otroUsuarioId = conversacion.Usuario1Id == userId ? conversacion.Usuario2Id : conversacion.Usuario1Id;

        // Eliminar todos los mensajes entre estos dos usuarios
        // Nota: Esto elimina el historial para AMBOS usuarios. 
        // En una app real, quizás solo se ocultarían para el usuario que borra, 
        // pero para este fix, queremos limpiar "ghost data".
        var mensajes = await _context.Mensajes
            .Where(m => 
                (m.RemitenteId == userId && m.DestinatarioId == otroUsuarioId) ||
                (m.RemitenteId == otroUsuarioId && m.DestinatarioId == userId))
            .ToListAsync();

        _context.Mensajes.RemoveRange(mensajes);
        _context.Conversaciones.Remove(conversacion);
        
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<UserInfoDto?> GetUserInfo(string userId)
    {
        try
        {
            var apiUrl = Environment.GetEnvironmentVariable("USUARIOS_API_URL") ?? "http://localhost:5156";
            
            // Intentar obtener por AuthId primero, ya que Mensajes.API usa AuthIds principalmente ahora
            var response = await _httpClient.GetAsync($"{apiUrl}/api/users/auth/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfoDto>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Si no se encuentra por AuthId, intentar por ID interno (legacy fallback)
                // Solo si parece un GUID
                if (Guid.TryParse(userId, out _))
                {
                     var responseId = await _httpClient.GetAsync($"{apiUrl}/api/users/{userId}");
                     if (responseId.IsSuccessStatusCode)
                     {
                         return await responseId.Content.ReadFromJsonAsync<UserInfoDto>();
                     }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error al obtener info del usuario {userId}: {ex.Message}");
        }

        return null;
    }
}

public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string? AuthId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Estado { get; set; } = "offline";
}
