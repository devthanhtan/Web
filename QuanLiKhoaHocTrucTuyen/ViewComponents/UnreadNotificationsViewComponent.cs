using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.ViewComponents;

public class UnreadNotificationsViewComponent : ViewComponent
{
    private readonly INotificationService _notifications;

    public UnreadNotificationsViewComponent(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return View((0, new List<QuanLiKhoaHocTrucTuyen.Models.Notification>()));

        var count = await _notifications.CountUnreadAsync(userId);
        var items = await _notifications.GetRecentForUserAsync(userId, 6);
        return View((count, items));
    }
}
