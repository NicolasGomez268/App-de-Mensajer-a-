using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mensajes.API.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var userId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? connection.User?.FindFirst("sub")?.Value;
            
        Console.WriteLine($"[CustomUserIdProvider] Connection: {connection.ConnectionId}, UserId: {userId}");
        return userId;
    }
}
