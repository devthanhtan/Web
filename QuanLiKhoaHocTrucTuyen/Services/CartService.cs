using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _db;

    public CartService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CartItem>> GetCartItemsAsync(string userId)
    {
        return await _db.CartItems
            .Include(c => c.Course).ThenInclude(co => co.Category)
            .Include(c => c.Course).ThenInclude(co => co.Instructor)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.AddedAt)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddToCartAsync(string userId, int courseId)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.Status == CourseStatus.Approved);
        if (course == null)
            return (false, "Khóa học không tồn tại hoặc chưa được xuất bản.");

        // BR-C2: Block already owned courses
        var alreadyOwned = await _db.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
        if (alreadyOwned)
            return (false, "Bạn đã sở hữu khóa học này rồi, không thể thêm vào giỏ hàng.");

        var existsInCart = await _db.CartItems.AnyAsync(c => c.UserId == userId && c.CourseId == courseId);
        if (existsInCart)
            return (false, "Khóa học này đã có trong giỏ hàng của bạn.");

        _db.CartItems.Add(new CartItem
        {
            UserId = userId,
            CourseId = courseId,
            AddedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task RemoveFromCartAsync(string userId, int courseId)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);
        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsInCartAsync(string userId, int courseId)
    {
        return await _db.CartItems.AnyAsync(c => c.UserId == userId && c.CourseId == courseId);
    }

    public async Task<bool> IsOwnedAsync(string userId, int courseId)
    {
        return await _db.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
    }
}
