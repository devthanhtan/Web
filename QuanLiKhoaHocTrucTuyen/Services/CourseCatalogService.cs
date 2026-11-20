using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;
using QuanLiKhoaHocTrucTuyen.ViewModels;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class CourseCatalogService : ICourseCatalogService
{
    private readonly ApplicationDbContext _db;

    public CourseCatalogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(List<CourseCardViewModel> Items, int TotalCount)> SearchApprovedCoursesAsync(
        string? keyword,
        int? categoryId,
        decimal? maxPrice,
        int? minRating,
        string? sort,
        int page = 1,
        int pageSize = 9)
    {
        var query = _db.Courses
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Reviews)
            .Include(c => c.Enrollments)
            .AsSplitQuery()
            .Where(c => c.Status == CourseStatus.Approved);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.ShortDescription.ToLower().Contains(term) ||
                c.Instructor.FullName.ToLower().Contains(term));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(c => c.Price <= maxPrice.Value);
        }

        var all = await query.ToListAsync();

        var projected = all.Select(c =>
        {
            var visibleReviews = c.Reviews.Where(r => !r.IsHidden).ToList();
            return new CourseCardViewModel
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                ShortDescription = c.ShortDescription,
                ThumbnailUrl = c.ThumbnailUrl,
                CategoryName = c.Category.Name,
                InstructorName = c.Instructor.FullName,
                Level = c.Level,
                Price = c.Price,
                ReviewCount = visibleReviews.Count,
                AverageRating = visibleReviews.Count > 0 ? Math.Round(visibleReviews.Average(r => r.Rating), 1) : 0,
                StudentCount = c.Enrollments.Count
            };
        });

        if (minRating.HasValue && minRating.Value > 0)
        {
            projected = projected.Where(x => x.AverageRating >= minRating.Value);
        }

        projected = sort switch
        {
            "price_asc" => projected.OrderBy(x => x.Price),
            "price_desc" => projected.OrderByDescending(x => x.Price),
            "rating" => projected.OrderByDescending(x => x.AverageRating).ThenByDescending(x => x.ReviewCount),
            _ => projected.OrderByDescending(x => x.StudentCount).ThenByDescending(x => x.Id)
        };

        var list = projected.ToList();
        var total = list.Count;
        var paged = list.Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize).ToList();
        return (paged, total);
    }

    public async Task<Course?> GetPublicCourseDetailAsync(int id)
    {
        return await _db.Courses
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Chapters.OrderBy(ch => ch.SortOrder))
                .ThenInclude(ch => ch.Lessons.OrderBy(l => l.SortOrder))
            .Include(c => c.Reviews.Where(r => !r.IsHidden)).ThenInclude(r => r.User)
            .Include(c => c.Enrollments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id && c.Status == CourseStatus.Approved);
    }
}
