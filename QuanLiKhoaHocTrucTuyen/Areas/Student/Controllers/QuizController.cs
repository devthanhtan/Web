using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Areas.Student.ViewModels;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = AppRoles.Student)]
public class QuizController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IQuizService _quizService;

    public QuizController(ApplicationDbContext db, IQuizService quizService)
    {
        _db = db;
        _quizService = quizService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Info(int id)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Course)
            .Include(q => q.Chapter)
            .Include(q => q.QuizQuestions)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null) return NotFound();

        var isOwned = await _db.Enrollments.AnyAsync(e => e.UserId == CurrentUserId && e.CourseId == quiz.CourseId);
        if (!isOwned) return Forbid();

        var eligibility = await _quizService.CheckEligibilityAsync(CurrentUserId, id);
        var attempts = await _db.QuizAttempts
            .Where(a => a.QuizId == id && a.UserId == CurrentUserId && a.SubmittedAt != null)
            .OrderByDescending(a => a.AttemptNumber)
            .ToListAsync();

        var cert = await _db.Certificates
            .FirstOrDefaultAsync(c => c.UserId == CurrentUserId && c.CourseId == quiz.CourseId);

        return View(new QuizInfoViewModel
        {
            Quiz = quiz,
            Eligibility = eligibility,
            PastAttempts = attempts,
            Certificate = cert
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        var (success, attempt, error) = await _quizService.StartAttemptAsync(CurrentUserId, id);
        if (!success || attempt == null)
        {
            TempData["Error"] = error ?? "Không thể bắt đầu làm bài.";
            return RedirectToAction(nameof(Info), new { id });
        }

        return RedirectToAction(nameof(Take), new { attemptId = attempt.Id });
    }

    public async Task<IActionResult> Take(int attemptId)
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Course)
            .Include(a => a.Quiz).ThenInclude(q => q.QuizQuestions.OrderBy(qq => qq.SortOrder)).ThenInclude(qq => qq.Question)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == CurrentUserId);

        if (attempt == null) return NotFound();
        if (attempt.SubmittedAt != null)
            return RedirectToAction(nameof(Result), new { attemptId });

        var remaining = (int)Math.Max(0, Math.Ceiling((attempt.ExpiresAt - DateTime.UtcNow).TotalSeconds));
        return View(new QuizTakeViewModel
        {
            Attempt = attempt,
            Questions = attempt.Quiz.QuizQuestions.OrderBy(qq => qq.SortOrder).Select(qq => qq.Question).ToList(),
            RemainingSeconds = remaining
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int attemptId, Dictionary<int, string>? answers)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var (success, attempt, _, error) = await _quizService.SubmitAttemptAsync(
            CurrentUserId,
            attemptId,
            answers ?? new Dictionary<int, string>(),
            baseUrl);

        if (!success || attempt == null)
        {
            TempData["Error"] = error ?? "Không thể nộp bài.";
            return RedirectToAction("Index", "Dashboard");
        }

        return RedirectToAction(nameof(Result), new { attemptId = attempt.Id });
    }

    public async Task<IActionResult> Result(int attemptId)
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.Course)
            .Include(a => a.Quiz).ThenInclude(q => q.QuizQuestions.OrderBy(qq => qq.SortOrder)).ThenInclude(qq => qq.Question)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == CurrentUserId);

        if (attempt == null) return NotFound();
        if (attempt.SubmittedAt == null)
            return RedirectToAction(nameof(Take), new { attemptId });

        var selected = new Dictionary<int, string>();
        if (!string.IsNullOrEmpty(attempt.AnswersJson))
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(attempt.AnswersJson);
            if (raw != null)
            {
                foreach (var kv in raw)
                {
                    if (int.TryParse(kv.Key, out var qid))
                        selected[qid] = kv.Value;
                }
            }
        }

        var eligibility = await _quizService.CheckEligibilityAsync(CurrentUserId, attempt.QuizId);
        var cert = await _db.Certificates
            .FirstOrDefaultAsync(c => c.UserId == CurrentUserId && c.CourseId == attempt.Quiz.CourseId);

        return View(new QuizResultViewModel
        {
            Attempt = attempt,
            SelectedAnswers = selected,
            Questions = attempt.Quiz.QuizQuestions.OrderBy(qq => qq.SortOrder).Select(qq => qq.Question).ToList(),
            Certificate = cert,
            CanRetake = eligibility.CanStart
        });
    }
}
