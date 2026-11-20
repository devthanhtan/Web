using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class LearningProgressService : ILearningProgressService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public LearningProgressService(ApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public bool IsVideoCompletionReached(int watchedSeconds, int durationSeconds)
    {
        if (durationSeconds <= 0) return watchedSeconds > 0;
        return watchedSeconds >= durationSeconds * 0.90;
    }

    public async Task<Enrollment?> GetEnrollmentWithCourseAsync(string userId, int courseId)
    {
        return await _db.Enrollments
            .Include(e => e.LessonProgresses)
            .Include(e => e.Course).ThenInclude(c => c.Category)
            .Include(e => e.Course).ThenInclude(c => c.Instructor)
            .Include(e => e.Course).ThenInclude(c => c.Chapters.OrderBy(ch => ch.SortOrder))
                .ThenInclude(ch => ch.Lessons.OrderBy(l => l.SortOrder))
                    .ThenInclude(l => l.Attachments)
            .Include(e => e.Course).ThenInclude(c => c.Quizzes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
    }

    public async Task<WatchProgressUpdateResult> RecordWatchProgressAsync(
        string userId,
        int lessonId,
        int deltaWatchedSeconds,
        int currentPositionSeconds)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Chapter)
            .FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson == null)
            return new WatchProgressUpdateResult(false, 0, 0, false, 0, 0, 0, false, "Không tìm thấy bài học.");

        var enrollment = await _db.Enrollments
            .Include(e => e.LessonProgresses)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == lesson.Chapter.CourseId);
        if (enrollment == null)
            return new WatchProgressUpdateResult(false, 0, 0, false, 0, 0, 0, false, "Bạn chưa sở hữu khóa học này.");

        var progress = enrollment.LessonProgresses.FirstOrDefault(p => p.LessonId == lessonId);
        var now = DateTime.UtcNow;

        if (progress == null)
        {
            progress = new LessonProgress
            {
                EnrollmentId = enrollment.Id,
                LessonId = lessonId,
                WatchedSeconds = 0,
                LastPositionSeconds = 0,
                LastWatchedAt = now
            };
            _db.LessonProgresses.Add(progress);
        }

        // Anti-skip / anti-cheat clamping: accept at most 15s per heartbeat AND cap by real wall-clock gap
        var boundedDelta = Math.Clamp(deltaWatchedSeconds, 0, 15);
        if (progress.WatchedSeconds > 0)
        {
            var wallSeconds = Math.Max(1, (int)Math.Ceiling((now - progress.LastWatchedAt).TotalSeconds) + 3);
            boundedDelta = Math.Min(boundedDelta, wallSeconds);
        }

        progress.WatchedSeconds = Math.Min(
            lesson.DurationSeconds > 0 ? lesson.DurationSeconds : int.MaxValue,
            progress.WatchedSeconds + boundedDelta);

        progress.LastPositionSeconds = lesson.DurationSeconds > 0
            ? Math.Clamp(currentPositionSeconds, 0, lesson.DurationSeconds)
            : Math.Max(0, currentPositionSeconds);

        progress.LastWatchedAt = now;

        if (!progress.IsCompleted && IsVideoCompletionReached(progress.WatchedSeconds, lesson.DurationSeconds))
        {
            progress.IsCompleted = true;
            progress.CompletedAt = now;
        }

        var (pct, completedCount, totalLessons) = await RecalculateCourseProgressAsync(enrollment);
        await _db.SaveChangesAsync();

        return new WatchProgressUpdateResult(
            true,
            progress.WatchedSeconds,
            progress.LastPositionSeconds,
            progress.IsCompleted,
            pct,
            completedCount,
            totalLessons,
            pct >= 100m);
    }

    public async Task<WatchProgressUpdateResult> MarkDocumentLessonCompletedAsync(string userId, int lessonId)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Chapter)
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.LessonType == LessonType.Document);
        if (lesson == null)
            return new WatchProgressUpdateResult(false, 0, 0, false, 0, 0, 0, false, "Không tìm thấy bài học tài liệu.");

        var enrollment = await _db.Enrollments
            .Include(e => e.LessonProgresses)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == lesson.Chapter.CourseId);
        if (enrollment == null)
            return new WatchProgressUpdateResult(false, 0, 0, false, 0, 0, 0, false, "Bạn chưa sở hữu khóa học này.");

        var progress = enrollment.LessonProgresses.FirstOrDefault(p => p.LessonId == lessonId);
        var now = DateTime.UtcNow;

        if (progress == null)
        {
            progress = new LessonProgress
            {
                EnrollmentId = enrollment.Id,
                LessonId = lessonId,
                WatchedSeconds = lesson.DurationSeconds,
                LastPositionSeconds = lesson.DurationSeconds,
                IsCompleted = true,
                CompletedAt = now,
                LastWatchedAt = now
            };
            _db.LessonProgresses.Add(progress);
        }
        else if (!progress.IsCompleted)
        {
            progress.IsCompleted = true;
            progress.CompletedAt = now;
            progress.LastWatchedAt = now;
        }

        var (pct, completedCount, totalLessons) = await RecalculateCourseProgressAsync(enrollment);
        await _db.SaveChangesAsync();

        return new WatchProgressUpdateResult(
            true,
            progress.WatchedSeconds,
            progress.LastPositionSeconds,
            true,
            pct,
            completedCount,
            totalLessons,
            pct >= 100m);
    }

    public async Task<(bool Success, string? Error)> AskQuestionAsync(string userId, int courseId, int? lessonId, string questionText)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            return (false, "Vui lòng nhập nội dung câu hỏi.");

        var enrollment = await _db.Enrollments
            .Include(e => e.Course)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
        if (enrollment == null)
            return (false, "Bạn cần sở hữu khóa học để gửi câu hỏi.");

        _db.CourseQuestions.Add(new CourseQuestion
        {
            CourseId = courseId,
            LessonId = lessonId,
            StudentId = userId,
            QuestionText = questionText.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _notifications.CreateAsync(
            enrollment.Course.InstructorId,
            "Câu hỏi mới từ học viên",
            $"{enrollment.User.FullName} vừa đặt câu hỏi trong khóa '{enrollment.Course.Title}'.",
            "/Instructor/Qna");

        return (true, null);
    }

    private async Task<(decimal Percent, int CompletedLessons, int TotalLessons)> RecalculateCourseProgressAsync(Enrollment enrollment)
    {
        var totalLessons = await _db.Lessons
            .CountAsync(l => l.Chapter.CourseId == enrollment.CourseId);

        if (totalLessons == 0)
        {
            enrollment.ProgressPercent = 0m;
            return (0m, 0, 0);
        }

        var completedCount = enrollment.LessonProgresses
            .Where(p => p.IsCompleted)
            .Select(p => p.LessonId)
            .Distinct()
            .Count();
        var pct = Math.Min(100m, Math.Round((decimal)completedCount / totalLessons * 100m, 2));
        enrollment.ProgressPercent = pct;
        if (pct >= 100m && enrollment.CompletedAt == null)
        {
            enrollment.CompletedAt = DateTime.UtcNow;
        }

        return (pct, completedCount, totalLessons);
    }
}
