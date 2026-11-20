using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface ICourseManagementService
{
    Task<List<Course>> GetInstructorCoursesAsync(string instructorId);
    Task<Course?> GetCourseOwnedByInstructorAsync(int courseId, string instructorId);
    Task<Course?> GetCourseWithCurriculumAsync(int courseId);

    Task<Course> CreateCourseAsync(string instructorId, string title, int categoryId, string shortDesc, string description, string level, decimal price, string? introYoutubeId, string? thumbnailUrl);
    Task<(bool Success, string? Error)> UpdateCourseAsync(int courseId, string instructorId, string title, int categoryId, string shortDesc, string description, string level, decimal price, string? introYoutubeId, string? thumbnailUrl);

    Task<(bool Success, string? Error)> AddChapterAsync(int courseId, string instructorId, string title, string? description);
    Task<(bool Success, string? Error)> UpdateChapterAsync(int chapterId, string instructorId, string title, string? description);
    Task<(bool Success, string? Error)> DeleteChapterAsync(int chapterId, string instructorId);
    Task<(bool Success, string? Error)> MoveChapterAsync(int chapterId, string instructorId, int direction);

    Task<(bool Success, Lesson? Lesson, string? Error)> AddLessonAsync(
        int chapterId, string instructorId, string title, string? description, LessonType lessonType,
        bool isFreePreview, int durationSeconds, string? contentHtml, IFormFile? videoFile, IFormFile? attachmentFile);

    Task<(bool Success, Lesson? Lesson, string? Error)> UpdateLessonAsync(
        int lessonId, string instructorId, string title, string? description, LessonType lessonType,
        bool isFreePreview, int durationSeconds, string? contentHtml, IFormFile? videoFile, IFormFile? attachmentFile);

    Task<(bool Success, string? Error)> DeleteLessonAsync(int lessonId, string instructorId);
    Task<(bool Success, string? Error)> MoveLessonAsync(int lessonId, string instructorId, int direction);
    Task<(bool Success, string? Error)> DeleteAttachmentAsync(int attachmentId, string instructorId);

    Task<(bool Success, List<string> BlockingReasons)> SubmitForReviewAsync(int courseId, string instructorId);

    // Admin review (BR-B7)
    Task<List<Course>> GetCoursesForAdminReviewAsync(CourseStatus? statusFilter = null);
    Task<(bool Success, string? Error)> ApproveCourseAsync(int courseId, string adminId);
    Task<(bool Success, string? Error)> RejectCourseAsync(int courseId, string adminId, string reason);
}
