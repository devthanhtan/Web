using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class CourseManagementService : ICourseManagementService
{
    private static readonly string[] VideoExtensions = [".mp4", ".webm", ".mov"];
    private static readonly string[] DocumentExtensions = [".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".zip", ".txt"];

    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IYouTubeUploadService _ytUpload;
    private readonly INotificationService _notifications;
    private readonly AppFeaturesOptions _features;

    public CourseManagementService(
        ApplicationDbContext db,
        IFileStorageService fileStorage,
        IYouTubeUploadService ytUpload,
        INotificationService notifications,
        IOptions<AppFeaturesOptions> features)
    {
        _db = db;
        _fileStorage = fileStorage;
        _ytUpload = ytUpload;
        _notifications = notifications;
        _features = features.Value;
    }

    public async Task<List<Course>> GetInstructorCoursesAsync(string instructorId)
    {
        return await _db.Courses
            .Include(c => c.Category)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .Include(c => c.Enrollments)
            .Where(c => c.InstructorId == instructorId)
            .AsSplitQuery()
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Course?> GetCourseOwnedByInstructorAsync(int courseId, string instructorId)
    {
        return await _db.Courses
            .Include(c => c.Category)
            .Include(c => c.Chapters.OrderBy(ch => ch.SortOrder))
                .ThenInclude(ch => ch.Lessons.OrderBy(l => l.SortOrder))
                    .ThenInclude(l => l.Attachments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == instructorId);
    }

    public async Task<Course?> GetCourseWithCurriculumAsync(int courseId)
    {
        return await _db.Courses
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Chapters.OrderBy(ch => ch.SortOrder))
                .ThenInclude(ch => ch.Lessons.OrderBy(l => l.SortOrder))
                    .ThenInclude(l => l.Attachments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public async Task<Course> CreateCourseAsync(
        string instructorId, string title, int categoryId, string shortDesc, string description,
        string level, decimal price, string? introYoutubeId, string? thumbnailUrl)
    {
        var baseSlug = SlugHelper.ToSlug(title);
        var slug = baseSlug;
        var counter = 1;
        while (await _db.Courses.AnyAsync(c => c.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var course = new Course
        {
            Title = title.Trim(),
            Slug = slug,
            CategoryId = categoryId,
            ShortDescription = shortDesc.Trim(),
            Description = description.Trim(),
            Level = level,
            Price = price,
            IntroYoutubeId = introYoutubeId?.Trim(),
            ThumbnailUrl = thumbnailUrl ?? "/images/course-dotnet.svg",
            InstructorId = instructorId,
            Status = CourseStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
        return course;
    }

    public async Task<(bool Success, string? Error)> UpdateCourseAsync(
        int courseId, string instructorId, string title, int categoryId, string shortDesc,
        string description, string level, decimal price, string? introYoutubeId, string? thumbnailUrl)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == instructorId);
        if (course == null) return (false, "Không tìm thấy khóa học hoặc bạn không có quyền sở hữu.");

        course.Title = title.Trim();
        course.CategoryId = categoryId;
        course.ShortDescription = shortDesc.Trim();
        course.Description = description.Trim();
        course.Level = level;
        course.Price = price;
        course.IntroYoutubeId = introYoutubeId?.Trim();
        if (!string.IsNullOrEmpty(thumbnailUrl))
        {
            course.ThumbnailUrl = thumbnailUrl;
        }
        course.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddChapterAsync(int courseId, string instructorId, string title, string? description)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.InstructorId == instructorId);
        if (course == null) return (false, "Không tìm thấy khóa học.");

        var maxOrder = await _db.Chapters.Where(c => c.CourseId == courseId).MaxAsync(c => (int?)c.SortOrder) ?? 0;
        _db.Chapters.Add(new Chapter
        {
            CourseId = courseId,
            Title = title.Trim(),
            Description = description?.Trim(),
            SortOrder = maxOrder + 1
        });
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateChapterAsync(int chapterId, string instructorId, string title, string? description)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == chapterId && c.Course.InstructorId == instructorId);
        if (chapter == null) return (false, "Không tìm thấy chương.");

        chapter.Title = title.Trim();
        chapter.Description = description?.Trim();
        chapter.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteChapterAsync(int chapterId, string instructorId)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Course)
            .Include(c => c.Lessons).ThenInclude(l => l.Attachments)
            .FirstOrDefaultAsync(c => c.Id == chapterId && c.Course.InstructorId == instructorId);
        if (chapter == null) return (false, "Không tìm thấy chương.");

        foreach (var lesson in chapter.Lessons)
        {
            _fileStorage.DeleteRelativeFile(lesson.LocalVideoPath);
            foreach (var att in lesson.Attachments)
                _fileStorage.DeleteRelativeFile(att.FilePath);
        }

        _db.Chapters.Remove(chapter);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> MoveChapterAsync(int chapterId, string instructorId, int direction)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == chapterId && c.Course.InstructorId == instructorId);
        if (chapter == null) return (false, "Không tìm thấy chương.");

        var siblings = await _db.Chapters
            .Where(c => c.CourseId == chapter.CourseId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var index = siblings.FindIndex(c => c.Id == chapterId);
        var swapIndex = index + (direction < 0 ? -1 : 1);
        if (swapIndex < 0 || swapIndex >= siblings.Count) return (true, null);

        (siblings[index].SortOrder, siblings[swapIndex].SortOrder) = (siblings[swapIndex].SortOrder, siblings[index].SortOrder);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, Lesson? Lesson, string? Error)> AddLessonAsync(
        int chapterId, string instructorId, string title, string? description, LessonType lessonType,
        bool isFreePreview, int durationSeconds, string? contentHtml, IFormFile? videoFile, IFormFile? attachmentFile)
    {
        var chapter = await _db.Chapters
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Id == chapterId && c.Course.InstructorId == instructorId);
        if (chapter == null) return (false, null, "Không tìm thấy chương hoặc không có quyền.");

        if (lessonType == LessonType.Video && (videoFile == null || videoFile.Length == 0))
            return (false, null, "Bài học dạng Video bắt buộc phải tải lên tệp video.");

        string? savedVideoPath = null;
        if (lessonType == LessonType.Video && videoFile != null)
        {
            var videoRes = await _fileStorage.SaveFileAsync(videoFile, "videos", VideoExtensions, _features.MaxVideoSizeBytes);
            if (!videoRes.Success)
                return (false, null, videoRes.Error);
            savedVideoPath = videoRes.RelativePath;
        }

        var maxOrder = await _db.Lessons.Where(l => l.ChapterId == chapterId).MaxAsync(l => (int?)l.SortOrder) ?? 0;
        var lesson = new Lesson
        {
            ChapterId = chapterId,
            Title = title.Trim(),
            Description = description?.Trim(),
            LessonType = lessonType,
            SortOrder = maxOrder + 1,
            IsFreePreview = isFreePreview,
            DurationSeconds = lessonType == LessonType.Video ? Math.Max(10, durationSeconds) : Math.Max(60, durationSeconds),
            ContentHtml = contentHtml,
            LocalVideoPath = savedVideoPath,
            VideoStatus = lessonType == LessonType.Video ? VideoUploadStatus.Processing : VideoUploadStatus.None
        };

        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();

        if (attachmentFile != null && attachmentFile.Length > 0)
        {
            var attRes = await _fileStorage.SaveFileAsync(attachmentFile, "attachments", DocumentExtensions, _features.MaxDocumentSizeBytes);
            if (attRes.Success && attRes.RelativePath != null)
            {
                _db.Attachments.Add(new Attachment
                {
                    LessonId = lesson.Id,
                    FileName = Path.GetFileName(attachmentFile.FileName),
                    FilePath = attRes.RelativePath,
                    FileSizeBytes = attachmentFile.Length,
                    ContentType = attachmentFile.ContentType
                });
                await _db.SaveChangesAsync();
            }
        }

        if (lesson.LessonType == LessonType.Video)
        {
            await _ytUpload.EnqueueLessonUploadAsync(lesson.Id);
        }

        return (true, lesson, null);
    }

    public async Task<(bool Success, Lesson? Lesson, string? Error)> UpdateLessonAsync(
        int lessonId, string instructorId, string title, string? description, LessonType lessonType,
        bool isFreePreview, int durationSeconds, string? contentHtml, IFormFile? videoFile, IFormFile? attachmentFile)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.Chapter.Course.InstructorId == instructorId);
        if (lesson == null) return (false, null, "Không tìm thấy bài học hoặc không có quyền.");

        lesson.Title = title.Trim();
        lesson.Description = description?.Trim();
        lesson.LessonType = lessonType;
        lesson.IsFreePreview = isFreePreview;
        lesson.DurationSeconds = durationSeconds > 0 ? durationSeconds : lesson.DurationSeconds;
        lesson.ContentHtml = contentHtml;

        bool enqueueUpload = false;
        if (lessonType == LessonType.Video && videoFile != null && videoFile.Length > 0)
        {
            var videoRes = await _fileStorage.SaveFileAsync(videoFile, "videos", VideoExtensions, _features.MaxVideoSizeBytes);
            if (!videoRes.Success)
                return (false, null, videoRes.Error);

            _fileStorage.DeleteRelativeFile(lesson.LocalVideoPath);
            lesson.LocalVideoPath = videoRes.RelativePath;
            lesson.YoutubeVideoId = null;
            lesson.VideoStatus = VideoUploadStatus.Processing;
            lesson.VideoErrorMessage = null;
            enqueueUpload = true;
        }
        else if (lessonType == LessonType.Document)
        {
            lesson.VideoStatus = VideoUploadStatus.None;
        }

        if (attachmentFile != null && attachmentFile.Length > 0)
        {
            var attRes = await _fileStorage.SaveFileAsync(attachmentFile, "attachments", DocumentExtensions, _features.MaxDocumentSizeBytes);
            if (!attRes.Success)
                return (false, null, attRes.Error);

            _db.Attachments.Add(new Attachment
            {
                LessonId = lesson.Id,
                FileName = Path.GetFileName(attachmentFile.FileName),
                FilePath = attRes.RelativePath!,
                FileSizeBytes = attachmentFile.Length,
                ContentType = attachmentFile.ContentType
            });
        }

        lesson.Chapter.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (enqueueUpload)
        {
            await _ytUpload.EnqueueLessonUploadAsync(lesson.Id);
        }

        return (true, lesson, null);
    }

    public async Task<(bool Success, string? Error)> DeleteLessonAsync(int lessonId, string instructorId)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Course)
            .Include(l => l.Attachments)
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.Chapter.Course.InstructorId == instructorId);
        if (lesson == null) return (false, "Không tìm thấy bài học.");

        _fileStorage.DeleteRelativeFile(lesson.LocalVideoPath);
        foreach (var att in lesson.Attachments)
            _fileStorage.DeleteRelativeFile(att.FilePath);

        _db.Lessons.Remove(lesson);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> MoveLessonAsync(int lessonId, string instructorId, int direction)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.Chapter.Course.InstructorId == instructorId);
        if (lesson == null) return (false, "Không tìm thấy bài học.");

        var siblings = await _db.Lessons
            .Where(l => l.ChapterId == lesson.ChapterId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();

        var index = siblings.FindIndex(l => l.Id == lessonId);
        var swapIndex = index + (direction < 0 ? -1 : 1);
        if (swapIndex < 0 || swapIndex >= siblings.Count) return (true, null);

        (siblings[index].SortOrder, siblings[swapIndex].SortOrder) = (siblings[swapIndex].SortOrder, siblings[index].SortOrder);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAttachmentAsync(int attachmentId, string instructorId)
    {
        var att = await _db.Attachments
            .Include(a => a.Lesson).ThenInclude(l => l.Chapter).ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.Lesson.Chapter.Course.InstructorId == instructorId);
        if (att == null) return (false, "Không tìm thấy tài liệu đính kèm.");

        _fileStorage.DeleteRelativeFile(att.FilePath);
        _db.Attachments.Remove(att);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, List<string> BlockingReasons)> SubmitForReviewAsync(int courseId, string instructorId)
    {
        var reasons = new List<string>();
        var course = await GetCourseOwnedByInstructorAsync(courseId, instructorId);
        if (course == null)
        {
            reasons.Add("Không tìm thấy khóa học hoặc bạn không có quyền sở hữu.");
            return (false, reasons);
        }

        if (course.Status == CourseStatus.Approved)
        {
            reasons.Add("Khóa học đã được duyệt công khai.");
            return (false, reasons);
        }

        if (course.Status == CourseStatus.PendingReview)
        {
            reasons.Add("Khóa học đang ở trạng thái chờ duyệt.");
            return (false, reasons);
        }

        var allLessons = course.Chapters.SelectMany(ch => ch.Lessons).ToList();
        if (!course.Chapters.Any() || !allLessons.Any())
        {
            reasons.Add("Khóa học phải có ít nhất 1 chương và 1 bài học trước khi gửi duyệt.");
        }

        foreach (var lesson in allLessons.Where(l => l.LessonType == LessonType.Video))
        {
            if (lesson.VideoStatus != VideoUploadStatus.Ready)
            {
                var statusLabel = lesson.VideoStatus switch
                {
                    VideoUploadStatus.Processing => "đang xử lý tải lên YouTube",
                    VideoUploadStatus.Error => $"bị lỗi ({lesson.VideoErrorMessage ?? "cần tải lại"})",
                    _ => "chưa có video"
                };
                reasons.Add($"Bài '{lesson.Title}' chưa sẵn sàng ({statusLabel}).");
            }
        }

        if (reasons.Any())
            return (false, reasons);

        course.Status = CourseStatus.PendingReview;
        course.RejectionReason = null;
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, reasons);
    }

    public async Task<List<Course>> GetCoursesForAdminReviewAsync(CourseStatus? statusFilter = null)
    {
        // Admin should NOT see instructors' private Drafts unless explicitly filtered (BR-B5: Draft chỉ Giảng viên sở hữu thấy)
        var query = _db.Courses
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .AsSplitQuery()
            .Where(c => c.Status != CourseStatus.Draft);

        if (statusFilter.HasValue && statusFilter.Value != CourseStatus.Draft)
        {
            query = query.Where(c => c.Status == statusFilter.Value);
        }

        return await query
            .OrderBy(c => c.Status == CourseStatus.PendingReview ? 0 : 1)
            .ThenByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> ApproveCourseAsync(int courseId, string adminId)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return (false, "Không tìm thấy khóa học.");
        if (course.Status == CourseStatus.Draft)
            return (false, "Không thể duyệt khóa học đang ở trạng thái Nháp.");

        course.Status = CourseStatus.Approved;
        course.RejectionReason = null;
        course.ReviewedByAdminId = adminId;
        course.ReviewedAt = DateTime.UtcNow;
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notifications.CreateAsync(
            course.InstructorId,
            "Khóa học đã được phê duyệt",
            $"Khóa học '{course.Title}' đã được Admin phê duyệt và hiển thị công khai.",
            $"/Courses/Details/{course.Id}");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RejectCourseAsync(int courseId, string adminId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Bắt buộc nhập lý do từ chối.");

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return (false, "Không tìm thấy khóa học.");
        if (course.Status == CourseStatus.Draft)
            return (false, "Không thể từ chối khóa học ở trạng thái Nháp.");

        course.Status = CourseStatus.Rejected;
        course.RejectionReason = reason.Trim();
        course.ReviewedByAdminId = adminId;
        course.ReviewedAt = DateTime.UtcNow;
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notifications.CreateAsync(
            course.InstructorId,
            "Khóa học bị từ chối duyệt",
            $"Khóa học '{course.Title}' bị từ chối với lý do: {course.RejectionReason}. Vui lòng chỉnh sửa và gửi duyệt lại.",
            $"/Instructor/Courses/Curriculum/{course.Id}");

        return (true, null);
    }
}
