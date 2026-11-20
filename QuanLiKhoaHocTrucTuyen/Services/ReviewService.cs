using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;

    public ReviewService(ApplicationDbContext db)
    {
        _db = db;
    }

    public bool CanStudentReview(Enrollment enrollment)
    {
        return enrollment.ProgressPercent >= 50m;
    }

    public async Task<(bool Success, string? Error)> SaveReviewAsync(string userId, int courseId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            return (false, "Điểm đánh giá phải từ 1 đến 5 sao.");

        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

        if (enrollment == null)
            return (false, "Bạn phải sở hữu khóa học này mới có thể đánh giá.");

        if (!CanStudentReview(enrollment))
            return (false, $"Bạn cần đạt tiến độ học từ 50% trở lên (hiện tại: {enrollment.ProgressPercent:0.#}%) để đánh giá khóa học.");

        var existing = await _db.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId);

        if (existing == null)
        {
            _db.Reviews.Add(new Review
            {
                UserId = userId,
                CourseId = courseId,
                Rating = rating,
                Comment = comment?.Trim(),
                IsHidden = false,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Rating = rating;
            existing.Comment = comment?.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<Review>> GetAllReviewsForAdminAsync(bool? hiddenOnly = null)
    {
        var query = _db.Reviews
            .Include(r => r.User)
            .Include(r => r.Course)
            .AsQueryable();

        if (hiddenOnly.HasValue)
        {
            query = query.Where(r => r.IsHidden == hiddenOnly.Value);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> ToggleHideReviewAsync(int reviewId)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null) return (false, "Không tìm thấy đánh giá.");

        review.IsHidden = !review.IsHidden;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteReviewAsync(int reviewId)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null) return (false, "Không tìm thấy đánh giá.");

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
