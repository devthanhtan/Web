using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;
using QuanLiKhoaHocTrucTuyen.ViewModels;

namespace QuanLiKhoaHocTrucTuyen.Controllers;

public class CoursesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICourseCatalogService _catalog;
    private readonly ICartService _cart;

    public CoursesController(
        ApplicationDbContext db,
        ICourseCatalogService catalog,
        ICartService cart)
    {
        _db = db;
        _catalog = catalog;
        _cart = cart;
    }

    public async Task<IActionResult> Index(
        string? keyword,
        int? categoryId,
        decimal? maxPrice,
        int? minRating,
        string? sort,
        int page = 1)
    {
        var (items, total) = await _catalog.SearchApprovedCoursesAsync(keyword, categoryId, maxPrice, minRating, sort, page, 9);
        var vm = new CourseCatalogViewModel
        {
            Keyword = keyword,
            CategoryId = categoryId,
            MaxPrice = maxPrice,
            MinRating = minRating,
            Sort = sort,
            Page = page,
            PageSize = 9,
            TotalCount = total,
            Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
            Courses = items
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var course = await _catalog.GetPublicCourseDetailAsync(id);
        if (course == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isOwned = !string.IsNullOrEmpty(userId) && await _cart.IsOwnedAsync(userId, id);
        var isInCart = !string.IsNullOrEmpty(userId) && await _cart.IsInCartAsync(userId, id);

        var visibleReviews = course.Reviews.Where(r => !r.IsHidden).OrderByDescending(r => r.CreatedAt).ToList();
        var allLessons = course.Chapters.SelectMany(ch => ch.Lessons).ToList();

        var vm = new CourseDetailViewModel
        {
            Course = course,
            IsOwned = isOwned,
            IsInCart = isInCart,
            VisibleReviews = visibleReviews,
            ReviewCount = visibleReviews.Count,
            AverageRating = visibleReviews.Count > 0 ? Math.Round(visibleReviews.Average(r => r.Rating), 1) : 0,
            TotalLessons = allLessons.Count,
            TotalDurationMinutes = (int)Math.Ceiling(allLessons.Sum(l => l.DurationSeconds) / 60.0)
        };

        return View(vm);
    }
}
