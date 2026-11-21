using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainTicketWeb.Hubs;
using TrainTicketWeb.Models;
using TrainTicketWeb.Repositories;

namespace TrainTicketWeb.Controllers;

/// <summary>
/// API nhỏ cho chuông thông báo: nạp danh sách, đếm chưa đọc, đánh dấu đã đọc.
/// SignalR đẩy thông báo mới; các endpoint này là dự phòng khi mất kết nối real-time.
/// </summary>
[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationRepository _notes;
    private readonly INotificationPusher _push;

    public NotificationController(INotificationRepository notes, INotificationPusher push)
    {
        _notes = notes;
        _push = push;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    private bool IsAdmin => User.IsInRole("admin");

    [HttpGet]
    public IActionResult Feed(int take = 20)
    {
        if (IsAdmin) return Json(new { ok = true, unread = 0, items = new object[0] });

        var items = _notes.GetForUser(CurrentUserId, take)
            .Select(n => new
            {
                id = n.NotificationId,
                type = n.Type,
                typeLabel = n.TypeLabel,
                title = n.Title,
                body = n.Body,
                linkUrl = n.LinkUrl,
                timeAgo = n.TimeAgo,
                unread = !n.IsRead
            })
            .ToList();

        return Json(new { ok = true, unread = _notes.CountUnread(CurrentUserId), items });
    }

    [HttpGet]
    public IActionResult UnreadCount()
        => Json(new { unread = IsAdmin ? 0 : _notes.CountUnread(CurrentUserId) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarkRead(int id)
    {
        if (!IsAdmin) _notes.MarkRead(id, CurrentUserId);
        return Json(new { ok = true, unread = IsAdmin ? 0 : _notes.CountUnread(CurrentUserId) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarkAllRead()
    {
        if (!IsAdmin) _notes.MarkAllRead(CurrentUserId);
        return Json(new { ok = true, unread = 0 });
    }

    /// <summary>Dùng khi cần đẩy thử 1 thông báo (kiểm thử real-time).</summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public IActionResult Test(int userId)
    {
        var n = new Notification
        {
            UserId = userId,
            Type = "system",
            Title = "Kiểm tra thông báo real-time",
            Body = "Nếu bạn thấy hộp thư này hiện ra ngay lập tức, kênh real-time đang hoạt động.",
            LinkUrl = "/",
            NotificationId = 0
        };
        int id = _notes.Add(n);
        n.NotificationId = id;
        _push.PushAsync(userId, n, _notes.CountUnread(userId));
        return Json(new { ok = true, id });
    }
}
