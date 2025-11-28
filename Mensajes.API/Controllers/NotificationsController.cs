using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Mensajes.API.DTOs;
using Mensajes.API.Hubs;

namespace Mensajes.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(IHubContext<ChatHub> hubContext, ILogger<NotificationsController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("group-added")]
    public async Task<IActionResult> NotifyGroupAdded([FromBody] GroupNotificationDto dto)
    {
        _logger.LogInformation($"[Notifications] Notificando nuevo grupo '{dto.GroupName}' ({dto.GroupId}) a {dto.MemberIds.Count} miembros.");

        foreach (var userId in dto.MemberIds)
        {
            // Enviar evento 'UserAddedToGroup' a cada usuario
            await _hubContext.Clients.User(userId.ToString()).SendAsync("UserAddedToGroup", new
            {
                groupId = dto.GroupId,
                groupName = dto.GroupName
            });
        }

        return Ok();
    }
}
