using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mensajes.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private static readonly Dictionary<string, string> _userConnections = new();

    public ChatHub(ILogger<ChatHub> _logger)
    {
        this._logger = _logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections[userId] = Context.ConnectionId;
            _logger.LogInformation($"Usuario {userId} conectado con ConnectionId: {Context.ConnectionId}");
            
            // Notificar a todos que el usuario está online
            await Clients.Others.SendAsync("UserConnected", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections.Remove(userId);
            _logger.LogInformation($"Usuario {userId} desconectado");
            
            // Notificar a todos que el usuario está offline
            await Clients.Others.SendAsync("UserDisconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string destinatarioId, string contenido, string tipoMensaje = "texto")
    {
        var remitenteId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(remitenteId))
        {
            _logger.LogWarning("Intento de enviar mensaje sin autenticación");
            return;
        }

        _logger.LogInformation($"Mensaje de {remitenteId} a {destinatarioId}: {contenido}");

        // Si el destinatario está conectado, enviarle el mensaje directamente
        if (_userConnections.TryGetValue(destinatarioId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", new
            {
                remitenteId,
                destinatarioId,
                contenido,
                tipoMensaje,
                fechaEnvio = DateTime.UtcNow
            });
        }

        // También enviar al remitente para confirmación
        await Clients.Caller.SendAsync("MessageSent", new
        {
            remitenteId,
            destinatarioId,
            contenido,
            tipoMensaje,
            fechaEnvio = DateTime.UtcNow
        });
    }

    public async Task MarkAsRead(string mensajeId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User?.FindFirst("sub")?.Value;

        _logger.LogInformation($"Usuario {userId} marcó mensaje {mensajeId} como leído");

        // Notificar al remitente que el mensaje fue leído
        await Clients.Others.SendAsync("MessageRead", new { mensajeId, lectorId = userId });
    }

    public async Task Typing(string destinatarioId)
    {
        var remitenteId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;

        if (_userConnections.TryGetValue(destinatarioId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("UserTyping", remitenteId);
        }
    }

    public async Task StopTyping(string destinatarioId)
    {
        var remitenteId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;

        if (_userConnections.TryGetValue(destinatarioId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("UserStoppedTyping", remitenteId);
        }
    }

    public static bool IsUserOnline(string userId)
    {
        return _userConnections.ContainsKey(userId);
    }
}
