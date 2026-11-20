using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class VideoUploadWorker : BackgroundService
{
    private readonly Channel<int> _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoUploadWorker> _logger;

    public VideoUploadWorker(
        Channel<int> queue,
        IServiceScopeFactory scopeFactory,
        ILogger<VideoUploadWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VideoUploadWorker đã khởi động.");

        // Re-enqueue any lessons left in Processing state before restart
        try
        {
            using var initScope = _scopeFactory.CreateScope();
            var db = initScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pendingIds = await db.Lessons
                .Where(l => l.LessonType == LessonType.Video && l.VideoStatus == VideoUploadStatus.Processing)
                .Select(l => l.Id)
                .ToListAsync(stoppingToken);

            foreach (var id in pendingIds)
            {
                await _queue.Writer.WriteAsync(id, stoppingToken);
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Không thể quét hàng đợi video khi khởi động.");
        }

        await foreach (var lessonId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessLessonAsync(lessonId, stoppingToken);
        }
    }

    private async Task ProcessLessonAsync(int lessonId, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ytService = scope.ServiceProvider.GetRequiredService<IYouTubeUploadService>();
            var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var features = scope.ServiceProvider.GetRequiredService<IOptions<AppFeaturesOptions>>().Value;

            var lesson = await db.Lessons
                .Include(l => l.Chapter).ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

            if (lesson == null || lesson.LessonType != LessonType.Video)
                return;

            var (success, youtubeVideoId, error) = await ytService.UploadVideoAsync(lesson, ct);
            if (success && !string.IsNullOrEmpty(youtubeVideoId))
            {
                lesson.YoutubeVideoId = youtubeVideoId;
                lesson.VideoStatus = VideoUploadStatus.Ready;
                lesson.VideoErrorMessage = null;

                // Clean up temporary file only when actually uploaded to YouTube (keep in mock mode so local video plays)
                if (!features.UseMockServices && !youtubeVideoId.StartsWith("MOCK_", StringComparison.Ordinal))
                {
                    fileStorage.DeleteRelativeFile(lesson.LocalVideoPath);
                    lesson.LocalVideoPath = null;
                }

                await db.SaveChangesAsync(ct);

                await notifications.CreateAsync(
                    lesson.Chapter.Course.InstructorId,
                    "Video bài học đã sẵn sàng",
                    $"Video bài '{lesson.Title}' trong khóa '{lesson.Chapter.Course.Title}' đã xử lý thành công.",
                    $"/Instructor/Courses/Curriculum/{lesson.Chapter.CourseId}");
            }
            else
            {
                lesson.VideoStatus = VideoUploadStatus.Error;
                lesson.VideoErrorMessage = error ?? "Không thể xử lý video.";
                await db.SaveChangesAsync(ct);

                await notifications.CreateAsync(
                    lesson.Chapter.Course.InstructorId,
                    "Lỗi tải video bài học",
                    $"Video bài '{lesson.Title}' gặp lỗi: {lesson.VideoErrorMessage}. Vui lòng tải lại video.",
                    $"/Instructor/Courses/EditLesson/{lesson.Id}");
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Lỗi xử lý nền video bài học {LessonId}", lessonId);
        }
    }
}
