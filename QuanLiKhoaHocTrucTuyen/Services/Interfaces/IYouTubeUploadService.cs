using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface IYouTubeUploadService
{
    ValueTask EnqueueLessonUploadAsync(int lessonId, CancellationToken ct = default);
    Task<(bool Success, string? YoutubeVideoId, string? Error)> UploadVideoAsync(Lesson lesson, CancellationToken ct = default);
}
