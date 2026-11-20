using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Areas.Student.ViewModels;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = AppRoles.Student)]
public class LearnController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILearningProgressService _progressService;
    private readonly IFileStorageService _fileStorage;

    public LearnController(
        ApplicationDbContext db,
        ILearningProgressService progressService,
        IFileStorageService fileStorage)
    {
        _db = db;
        _progressService = progressService;
        _fileStorage = fileStorage;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(int courseId, int? lessonId)
    {
        var enrollment = await _progressService.GetEnrollmentWithCourseAsync(CurrentUserId, courseId);
        if (enrollment == null)
        {
            TempData["Error"] = "Bạn chưa sở hữu khóa học này nên không thể vào giao diện học.";
            return RedirectToAction("Details", "Courses", new { area = "", id = courseId });
        }

        var orderedLessons = enrollment.Course.Chapters
            .OrderBy(ch => ch.SortOrder)
            .SelectMany(ch => ch.Lessons.OrderBy(l => l.SortOrder))
            .ToList();

        Lesson? currentLesson = null;
        if (lessonId.HasValue)
        {
            currentLesson = orderedLessons.FirstOrDefault(l => l.Id == lessonId.Value);
        }
        currentLesson ??= orderedLessons.FirstOrDefault(l =>
            !enrollment.LessonProgresses.Any(p => p.LessonId == l.Id && p.IsCompleted))
            ?? orderedLessons.FirstOrDefault();

        // BR-D3: Document lesson is marked completed when student opens and views it
        if (currentLesson != null && currentLesson.LessonType == LessonType.Document)
        {
            await _progressService.MarkDocumentLessonCompletedAsync(CurrentUserId, currentLesson.Id);
            enrollment = (await _progressService.GetEnrollmentWithCourseAsync(CurrentUserId, courseId))!;
        }

        var progressDict = enrollment.LessonProgresses
            .GroupBy(p => p.LessonId)
            .ToDictionary(g => g.Key, g => g.First());
        LessonProgress? currentProgress = null;
        if (currentLesson != null)
        {
            progressDict.TryGetValue(currentLesson.Id, out currentProgress);
        }

        var vm = new LearningRoomViewModel
        {
            Enrollment = enrollment,
            CurrentLesson = currentLesson,
            CurrentProgress = currentProgress,
            ProgressByLessonId = progressDict,
            CompletedLessonsCount = progressDict.Values.Count(p => p.IsCompleted),
            TotalLessonsCount = orderedLessons.Count,
            ExistingReview = await _db.Reviews.FirstOrDefaultAsync(r => r.UserId == CurrentUserId && r.CourseId == courseId),
            ExistingCertificate = await _db.Certificates.FirstOrDefaultAsync(c => c.UserId == CurrentUserId && c.CourseId == courseId),
            Questions = await _db.CourseQuestions
                .Include(q => q.Student)
                .Include(q => q.Lesson)
                .Include(q => q.AnsweredBy)
                .Where(q => q.CourseId == courseId)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrackProgress([FromBody] TrackWatchProgressRequest req)
    {
        if (req == null || req.LessonId <= 0)
            return BadRequest(new { success = false, error = "Dữ liệu không hợp lệ." });

        var res = await _progressService.RecordWatchProgressAsync(
            CurrentUserId,
            req.LessonId,
            req.DeltaWatchedSeconds,
            req.CurrentPositionSeconds);

        if (!res.Success)
            return StatusCode(403, res);

        return Json(res);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDocumentViewed(int lessonId)
    {
        var res = await _progressService.MarkDocumentLessonCompletedAsync(CurrentUserId, lessonId);
        if (!res.Success) return Forbid();
        return Json(res);
    }

    public async Task<IActionResult> DownloadAttachment(int attachmentId)
    {
        var att = await _db.Attachments
            .Include(a => a.Lesson).ThenInclude(l => l.Chapter)
            .FirstOrDefaultAsync(a => a.Id == attachmentId);
        if (att == null) return NotFound();

        var isOwned = await _db.Enrollments.AnyAsync(e => e.UserId == CurrentUserId && e.CourseId == att.Lesson.Chapter.CourseId);
        if (!isOwned) return Forbid();

        var physicalPath = _fileStorage.ResolvePhysicalPath(att.FilePath);
        if (!System.IO.File.Exists(physicalPath))
            return NotFound("Tệp không còn tồn tại trên máy chủ.");

        return PhysicalFile(physicalPath, att.ContentType, att.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AskQuestion(int courseId, int? lessonId, string questionText)
    {
        var res = await _progressService.AskQuestionAsync(CurrentUserId, courseId, lessonId, questionText);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã gửi câu hỏi cho Giảng viên."
            : res.Error;

        return RedirectToAction(nameof(Index), new { courseId, lessonId });
    }
}
