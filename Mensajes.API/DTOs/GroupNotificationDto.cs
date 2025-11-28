namespace Mensajes.API.DTOs;

public class GroupNotificationDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<Guid> MemberIds { get; set; } = new();
}
