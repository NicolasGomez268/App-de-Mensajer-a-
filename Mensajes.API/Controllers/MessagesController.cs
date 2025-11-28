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

        // Crear el mensaje
        var mensaje = new Mensaje
        {
            Id = Guid.NewGuid(),
            RemitenteId = remitenteId,
            DestinatarioId = request.DestinatarioId,
            GrupoId = request.GrupoId,
            Contenido = request.Contenido,
            TipoMensaje = request.TipoMensaje,
            ArchivoUrl = request.ArchivoUrl,
            FechaEnvio = DateTime.UtcNow,
            Leido = false
        };

        _context.Mensajes.Add(mensaje);

        // Crear o actualizar conversación
        var conversacion = await _context.Conversaciones
            .FirstOrDefaultAsync(c =>
                (c.Usuario1Id == remitenteId && c.Usuario2Id == request.DestinatarioId) ||
                (c.Usuario1Id == request.DestinatarioId && c.Usuario2Id == remitenteId));

        if (conversacion == null)
        {
            conversacion = new Conversacion
            {
                Id = Guid.NewGuid(),
                Usuario1Id = remitenteId,
                Usuario2Id = request.DestinatarioId,
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

        await _context.SaveChangesAsync();

        // Enviar notificación via SignalR
        await _hubContext.Clients.User(request.DestinatarioId).SendAsync("ReceiveMessage", new
        {
            id = mensaje.Id,
            remitenteId = mensaje.RemitenteId,
            destinatarioId = mensaje.DestinatarioId,
            contenido = mensaje.Contenido,
            tipoMensaje = mensaje.TipoMensaje,
            fechaEnvio = mensaje.FechaEnvio,
            leido = mensaje.Leido
        });

        _logger.LogInformation($"Mensaje enviado de {remitenteId} a {request.DestinatarioId}");

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

        if (mensaje.DestinatarioId != userId)
        {
            return Forbid();
        }

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

    private async Task<UserInfoDto?> GetUserInfo(string userId)
    {
        try
        {
            var apiUrl = Environment.GetEnvironmentVariable("USUARIOS_API_URL") ?? "http://localhost:5156";
            var response = await _httpClient.GetAsync($"{apiUrl}/api/users/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfoDto>();
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
    public string Nombre { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Estado { get; set; } = "offline";
}
