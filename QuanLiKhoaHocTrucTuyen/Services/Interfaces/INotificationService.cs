using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface INotificationService
{
    Task CreateAsync(string userId, string title, string message, string? linkUrl = null);
    Task<List<Notification>> GetRecentForUserAsync(string userId, int take = 10);
    Task<int> CountUnreadAsync(string userId);
    Task MarkAllReadAsync(string userId);
}
