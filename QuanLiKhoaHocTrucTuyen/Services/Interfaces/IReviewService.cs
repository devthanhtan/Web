using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface IReviewService
{
    bool CanStudentReview(Enrollment enrollment);
    Task<(bool Success, string? Error)> SaveReviewAsync(string userId, int courseId, int rating, string? comment);
    Task<List<Review>> GetAllReviewsForAdminAsync(bool? hiddenOnly = null);
    Task<(bool Success, string? Error)> ToggleHideReviewAsync(int reviewId);
    Task<(bool Success, string? Error)> DeleteReviewAsync(int reviewId);
}
