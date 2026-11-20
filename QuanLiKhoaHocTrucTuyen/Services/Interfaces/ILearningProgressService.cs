using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public record WatchProgressUpdateResult(
    bool Success,
    int WatchedSeconds,
    int LastPositionSeconds,
    bool LessonCompleted,
    decimal CourseProgressPercent,
    int CompletedLessons,
    int TotalLessons,
    bool FinalExamUnlocked,
    string? Error = null);

public interface ILearningProgressService
{
    Task<Enrollment?> GetEnrollmentWithCourseAsync(string userId, int courseId);
    Task<WatchProgressUpdateResult> RecordWatchProgressAsync(string userId, int lessonId, int deltaWatchedSeconds, int currentPositionSeconds);
    Task<WatchProgressUpdateResult> MarkDocumentLessonCompletedAsync(string userId, int lessonId);
    Task<(bool Success, string? Error)> AskQuestionAsync(string userId, int courseId, int? lessonId, string questionText);
    bool IsVideoCompletionReached(int watchedSeconds, int durationSeconds);
}
