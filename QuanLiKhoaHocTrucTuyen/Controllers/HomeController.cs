using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;
using QuanLiKhoaHocTrucTuyen.ViewModels;

namespace QuanLiKhoaHocTrucTuyen.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public HomeController(ApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        var courses = await _db.Courses
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Reviews)
            .Include(c => c.Enrollments)
            .Where(c => c.Status == CourseStatus.Approved)
            .OrderByDescending(c => c.Enrollments.Count)
            .ThenByDescending(c => c.CreatedAt)
            .Take(6)
            .ToListAsync();

        var vm = new HomeIndexViewModel
        {
            Categories = categories,
            FeaturedCourses = courses.Select(c =>
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
            }).ToList(),
            TotalCourses = await _db.Courses.CountAsync(c => c.Status == CourseStatus.Approved),
            TotalStudents = await _db.Enrollments.Select(e => e.UserId).Distinct().CountAsync(),
            TotalInstructors = await _db.Courses.Where(c => c.Status == CourseStatus.Approved).Select(c => c.InstructorId).Distinct().CountAsync()
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await _notifications.MarkAllReadAsync(userId);
        }

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            return Redirect(referer);

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
