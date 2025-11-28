using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mensajes.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly HttpClient _httpClient;
    private static readonly Dictionary<string, string> _userConnections = new();

    public ChatHub(ILogger<ChatHub> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? Context.User?.FindFirst("sub")?.Value;

        _logger.LogInformation($"[SignalR] OnConnectedAsync llamado. ConnectionId: {Context.ConnectionId}, userId: {userId}");

        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections[userId] = Context.ConnectionId;
            _logger.LogInformation($"[SignalR] Usuario {userId} conectado con ConnectionId: {Context.ConnectionId}");
            
            // Notificar a todos que el usuario está online
            await Clients.Others.SendAsync("UserConnected", userId);

            // Actualizar estado en BD
            await UpdateUserStatus(userId, "online");
        }
        else
        {
            _logger.LogWarning($"[SignalR] No se pudo obtener el userId del token JWT. ConnectionId: {Context.ConnectionId}");
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

            // Actualizar estado en BD
            await UpdateUserStatus(userId, "offline");
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task UpdateUserStatus(string userId, string status)
    {
        try
        {
            var usuariosApiUrl = Environment.GetEnvironmentVariable("USUARIOS_API_URL") ?? "http://localhost:5156";
            // Necesitamos pasar el token actual para autenticarnos contra Usuarios.API
            // O podríamos usar un token de servicio a servicio, pero por simplicidad intentaremos propagar el contexto si es posible
            // Dado que SignalR no propaga headers automáticamente en HttpClient, y Usuarios.API requiere Auth,
            // esto es un punto delicado. 
            
            // POR AHORA: Asumimos que Usuarios.API permite esto o que implementaremos un cliente interno.
            // Si Usuarios.API requiere token de usuario, necesitamos extraerlo del Context.
            
            var httpContext = Context.GetHttpContext();
            var token = httpContext?.Request.Query["access_token"];
            
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.PatchAsJsonAsync($"{usuariosApiUrl}/api/users/status", status);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Error actualizando estado de usuario {userId} a {status}: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Excepción actualizando estado de usuario {userId}");
        }
    }

    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation($"Connection {Context.ConnectionId} joined group {groupId}");
    }

    public async Task LeaveGroup(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        _logger.LogInformation($"Connection {Context.ConnectionId} left group {groupId}");
    }

    public async Task SendMessage(string destinatarioId, string contenido, string tipoMensaje = "texto", string? grupoId = null)
    {
        var remitenteId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(remitenteId))
        {
            _logger.LogWarning("Intento de enviar mensaje sin autenticación");
            return;
        }

        _logger.LogInformation($"[DEBUG] SendMessage: De {remitenteId} para {(grupoId != null ? "Grupo " + grupoId : destinatarioId)}. Contenido: {contenido}");

        if (!string.IsNullOrEmpty(grupoId))
        {
            // Enviar a grupo
            await Clients.Group(grupoId).SendAsync("ReceiveMessage", new
            {
                remitenteId,
                destinatarioId = grupoId, // En grupos, el destinatario es el ID del grupo para el cliente
                grupoId,
                contenido,
                tipoMensaje,
                fechaEnvio = DateTime.UtcNow
            });
            _logger.LogInformation($"[DEBUG] Mensaje enviado al grupo {grupoId}");
        }
        else
        {
            // Enviar a usuario directo usando el mecanismo estándar de SignalR
            await Clients.User(destinatarioId).SendAsync("ReceiveMessage", new
            {
                remitenteId,
                destinatarioId,
                contenido,
                tipoMensaje,
                fechaEnvio = DateTime.UtcNow
            });
            _logger.LogInformation($"[DEBUG] Mensaje enviado al usuario {destinatarioId}");
        }

        // También enviar al remitente para confirmación (siempre)
        await Clients.Caller.SendAsync("MessageSent", new
        {
            remitenteId,
            destinatarioId = grupoId ?? destinatarioId,
            grupoId,
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

        await Clients.User(destinatarioId).SendAsync("UserTyping", remitenteId);
    }

    public async Task StopTyping(string destinatarioId)
    {
        var remitenteId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;

        await Clients.User(destinatarioId).SendAsync("UserStoppedTyping", remitenteId);
    }

    public static bool IsUserOnline(string userId)
    {
        return _userConnections.ContainsKey(userId);
    }
}
