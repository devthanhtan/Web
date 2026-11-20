using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.ViewModels;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface ICourseCatalogService
{
    Task<(List<CourseCardViewModel> Items, int TotalCount)> SearchApprovedCoursesAsync(
        string? keyword,
        int? categoryId,
        decimal? maxPrice,
        int? minRating,
        string? sort,
        int page = 1,
        int pageSize = 9);

    Task<Course?> GetPublicCourseDetailAsync(int id);
}
