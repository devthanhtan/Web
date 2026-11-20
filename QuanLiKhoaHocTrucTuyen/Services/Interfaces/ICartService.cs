using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface ICartService
{
    Task<List<CartItem>> GetCartItemsAsync(string userId);
    Task<(bool Success, string? Error)> AddToCartAsync(string userId, int courseId);
    Task RemoveFromCartAsync(string userId, int courseId);
    Task<bool> IsInCartAsync(string userId, int courseId);
    Task<bool> IsOwnedAsync(string userId, int courseId);
}
