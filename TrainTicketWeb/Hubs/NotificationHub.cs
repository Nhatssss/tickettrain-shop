using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;

namespace TrainTicketWeb.Hubs;

/// <summary>
/// Hub đẩy thông báo tới trình duyệt của từng khách theo thời gian thực.
/// Mỗi khách được đưa vào group "user-{id}" khi kết nối.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var id = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(id))
            await Groups.AddToGroupAsync(Context.ConnectionId, "user-" + id);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var id = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(id))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "user-" + id);
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>Payload gửi xuống client. Dùng chung cho SignalR và API polling dự phòng.</summary>
public class NotificationPayload
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string TypeLabel { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string LinkUrl { get; set; }
    public string TimeAgo { get; set; }
    public int UnreadCount { get; set; }
}

public interface INotificationPusher
{
    Task PushAsync(int userId, Notification n, int unreadCount);
    NotificationPayload ToPayload(Notification n, int unread = 0);
}

/// <summary>SignalR lấy user id từ claim NameIdentifier (guid mặc định là connection id).</summary>
public class NameUserIdProvider : Microsoft.AspNetCore.SignalR.IUserIdProvider
{
    public string GetUserId(Microsoft.AspNetCore.SignalR.HubConnectionContext conn)
        => conn.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}

public class NotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationPusher(IHubContext<NotificationHub> hub) => _hub = hub;

    public NotificationPayload ToPayload(Notification n, int unread = 0) => new NotificationPayload
    {
        Id = n.NotificationId,
        Type = n.Type,
        TypeLabel = n.TypeLabel,
        Title = n.Title,
        Body = n.Body,
        LinkUrl = n.LinkUrl,
        TimeAgo = n.TimeAgo,
        UnreadCount = unread
    };

    public Task PushAsync(int userId, Notification n, int unreadCount)
        => _hub.Clients.Group("user-" + userId).SendAsync("notify", ToPayload(n, unreadCount));
}
