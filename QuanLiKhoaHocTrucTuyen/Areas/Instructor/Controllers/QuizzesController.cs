using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Areas.Instructor.ViewModels;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public class QuizzesController : Controller
{
    private readonly ApplicationDbContext _db;

    public QuizzesController(ApplicationDbContext db)
    {
        _db = db;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(int? courseId)
    {
        var courses = await _db.Courses
            .Where(c => c.InstructorId == CurrentUserId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        var selectedCourseId = courseId ?? courses.FirstOrDefault()?.Id ?? 0;
        ViewBag.Courses = new SelectList(courses, "Id", "Title", selectedCourseId);
        ViewBag.SelectedCourseId = selectedCourseId;

        var quizzes = selectedCourseId == 0
            ? new List<Quiz>()
            : await _db.Quizzes
                .Include(q => q.Chapter)
                .Include(q => q.QuizQuestions)
                .Include(q => q.Attempts)
                .Where(q => q.CourseId == selectedCourseId && q.Course.InstructorId == CurrentUserId)
                .OrderByDescending(q => q.IsFinalExam).ThenBy(q => q.Id)
                .ToListAsync();

        return View(quizzes);
    }

    public async Task<IActionResult> Create(int courseId)
    {
        var course = await _db.Courses
            .Include(c => c.Chapters.OrderBy(ch => ch.SortOrder))
            .Include(c => c.Questions.OrderByDescending(q => q.Id))
            .FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == CurrentUserId);
        if (course == null) return NotFound();

        ViewBag.CourseTitle = course.Title;
        return View(new QuizFormViewModel
        {
            CourseId = course.Id,
            CourseChapters = course.Chapters.ToList(),
            AvailableQuestions = course.Questions.ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuizFormViewModel vm)
    {
        var course = await _db.Courses
            .Include(c => c.Chapters)
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.Id == vm.CourseId && c.InstructorId == CurrentUserId);
        if (course == null) return Forbid();

        if (!ModelState.IsValid)
        {
            vm.CourseChapters = course.Chapters.ToList();
            vm.AvailableQuestions = course.Questions.ToList();
            ViewBag.CourseTitle = course.Title;
            return View(vm);
        }

        var quiz = new Quiz
        {
            CourseId = vm.CourseId,
            ChapterId = vm.IsFinalExam ? null : vm.ChapterId,
            Title = vm.Title.Trim(),
            Description = vm.Description?.Trim(),
            IsFinalExam = vm.IsFinalExam,
            DurationMinutes = vm.DurationMinutes,
            PassingScorePercent = vm.PassingScorePercent,
            MaxAttempts = vm.MaxAttempts,
            CreatedAt = DateTime.UtcNow
        };

        var validQuestionIds = course.Questions.Select(q => q.Id).ToHashSet();
        int sort = 1;
        foreach (var qid in vm.SelectedQuestionIds.Distinct().Where(id => validQuestionIds.Contains(id)))
        {
            quiz.QuizQuestions.Add(new QuizQuestion { QuestionId = qid, SortOrder = sort++ });
        }

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã tạo bài kiểm tra mới.";
        return RedirectToAction(nameof(Index), new { courseId = vm.CourseId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Course).ThenInclude(c => c.Chapters.OrderBy(ch => ch.SortOrder))
            .Include(q => q.Course).ThenInclude(c => c.Questions.OrderByDescending(qu => qu.Id))
            .Include(q => q.QuizQuestions)
            .FirstOrDefaultAsync(q => q.Id == id && q.Course.InstructorId == CurrentUserId);
        if (quiz == null) return NotFound();

        ViewBag.CourseTitle = quiz.Course.Title;
        return View(new QuizFormViewModel
        {
            Id = quiz.Id,
            CourseId = quiz.CourseId,
            ChapterId = quiz.ChapterId,
            Title = quiz.Title,
            Description = quiz.Description,
            IsFinalExam = quiz.IsFinalExam,
            DurationMinutes = quiz.DurationMinutes,
            PassingScorePercent = quiz.PassingScorePercent,
            MaxAttempts = quiz.MaxAttempts,
            SelectedQuestionIds = quiz.QuizQuestions.Select(qq => qq.QuestionId).ToList(),
            CourseChapters = quiz.Course.Chapters.ToList(),
            AvailableQuestions = quiz.Course.Questions.ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(QuizFormViewModel vm)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Course).ThenInclude(c => c.Chapters)
            .Include(q => q.Course).ThenInclude(c => c.Questions)
            .Include(q => q.QuizQuestions)
            .FirstOrDefaultAsync(q => q.Id == vm.Id && q.Course.InstructorId == CurrentUserId);
        if (quiz == null) return Forbid();

        if (!ModelState.IsValid)
        {
            vm.CourseChapters = quiz.Course.Chapters.ToList();
            vm.AvailableQuestions = quiz.Course.Questions.ToList();
            ViewBag.CourseTitle = quiz.Course.Title;
            return View(vm);
        }

        quiz.Title = vm.Title.Trim();
        quiz.Description = vm.Description?.Trim();
        quiz.IsFinalExam = vm.IsFinalExam;
        quiz.ChapterId = vm.IsFinalExam ? null : vm.ChapterId;
        quiz.DurationMinutes = vm.DurationMinutes;
        quiz.PassingScorePercent = vm.PassingScorePercent;
        quiz.MaxAttempts = vm.MaxAttempts;

        _db.QuizQuestions.RemoveRange(quiz.QuizQuestions);
        var validIds = quiz.Course.Questions.Select(q => q.Id).ToHashSet();
        int sort = 1;
        foreach (var qid in vm.SelectedQuestionIds.Distinct().Where(id => validIds.Contains(id)))
        {
            _db.QuizQuestions.Add(new QuizQuestion { QuizId = quiz.Id, QuestionId = qid, SortOrder = sort++ });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật bài kiểm tra.";
        return RedirectToAction(nameof(Index), new { courseId = quiz.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Course)
            .FirstOrDefaultAsync(q => q.Id == id && q.Course.InstructorId == CurrentUserId);
        if (quiz == null) return Forbid();

        var courseId = quiz.CourseId;
        _db.Quizzes.Remove(quiz);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa bài kiểm tra.";
        return RedirectToAction(nameof(Index), new { courseId });
    }
}
