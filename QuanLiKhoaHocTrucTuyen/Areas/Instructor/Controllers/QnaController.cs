using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public class QnaController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public QnaController(ApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(bool? unansweredOnly)
    {
        ViewBag.UnansweredOnly = unansweredOnly;
        var query = _db.CourseQuestions
            .Include(q => q.Course)
            .Include(q => q.Lesson)
            .Include(q => q.Student)
            .Where(q => q.Course.InstructorId == CurrentUserId);

        if (unansweredOnly == true)
        {
            query = query.Where(q => q.AnswerText == null);
        }

        var items = await query
            .OrderBy(q => q.AnswerText != null)
            .ThenByDescending(q => q.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string answerText)
    {
        if (string.IsNullOrWhiteSpace(answerText))
        {
            TempData["Error"] = "Vui lòng nhập nội dung trả lời.";
            return RedirectToAction(nameof(Index));
        }

        var q = await _db.CourseQuestions
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id && x.Course.InstructorId == CurrentUserId);
        if (q == null) return Forbid();

        q.AnswerText = answerText.Trim();
        q.AnsweredById = CurrentUserId;
        q.AnsweredAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notifications.CreateAsync(
            q.StudentId,
            "Giảng viên đã trả lời câu hỏi của bạn",
            $"Câu hỏi của bạn trong khóa '{q.Course.Title}' đã được giải đáp.",
            $"/Student/Learn?courseId={q.CourseId}&lessonId={q.LessonId}");

        TempData["Success"] = "Đã gửi câu trả lời cho học viên.";
        return RedirectToAction(nameof(Index));
    }
}
