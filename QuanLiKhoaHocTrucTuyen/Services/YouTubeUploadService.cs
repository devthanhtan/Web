using System.Threading.Channels;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Options;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;
using YtData = Google.Apis.YouTube.v3.Data;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class YouTubeUploadService : IYouTubeUploadService
{
    private readonly Channel<int> _queue;
    private readonly YouTubeOptions _yt;
    private readonly AppFeaturesOptions _features;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<YouTubeUploadService> _logger;

    public YouTubeUploadService(
        Channel<int> queue,
        IOptions<YouTubeOptions> yt,
        IOptions<AppFeaturesOptions> features,
        IFileStorageService fileStorage,
        ILogger<YouTubeUploadService> logger)
    {
        _queue = queue;
        _yt = yt.Value;
        _features = features.Value;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public ValueTask EnqueueLessonUploadAsync(int lessonId, CancellationToken ct = default)
    {
        _logger.LogInformation("Đưa bài học {LessonId} vào hàng đợi xử lý video nền.", lessonId);
        return _queue.Writer.WriteAsync(lessonId, ct);
    }

    public async Task<(bool Success, string? YoutubeVideoId, string? Error)> UploadVideoAsync(Lesson lesson, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(lesson.LocalVideoPath))
            return (false, null, "Không tìm thấy đường dẫn tệp video tạm.");

        var physicalPath = _fileStorage.ResolvePhysicalPath(lesson.LocalVideoPath);
        if (!File.Exists(physicalPath))
            return (false, null, "Tệp video không tồn tại trên máy chủ.");

        if (_features.UseMockServices ||
            string.IsNullOrWhiteSpace(_yt.ClientId) ||
            string.IsNullOrWhiteSpace(_yt.ClientSecret) ||
            string.IsNullOrWhiteSpace(_yt.RefreshToken))
        {
            _logger.LogInformation("[MockYouTube] Giả lập đăng video bài học {LessonId} ({Title}) ở chế độ unlisted.", lesson.Id, lesson.Title);
            await Task.Delay(1500, ct);

            if (Path.GetFileName(physicalPath).Contains("fail_test", StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, "Giả lập lỗi upload YouTube (tên file chứa fail_test).");
            }

            return (true, $"MOCK_{lesson.Id}", null);
        }

        try
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _yt.ClientId,
                    ClientSecret = _yt.ClientSecret
                },
                Scopes = [YouTubeService.Scope.YoutubeUpload]
            });

            var credential = new UserCredential(flow, "platform-channel", new TokenResponse
            {
                RefreshToken = _yt.RefreshToken
            });

            var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "HUIT Learning"
            });

            var video = new YtData.Video
            {
                Snippet = new YtData.VideoSnippet
                {
                    Title = lesson.Title,
                    Description = lesson.Description ?? "Bài giảng trên nền tảng HUIT Learning",
                    CategoryId = "27" // Education
                },
                Status = new YtData.VideoStatus
                {
                    PrivacyStatus = "unlisted",
                    SelfDeclaredMadeForKids = false
                }
            };

            string? uploadedVideoId = null;
            string? uploadError = null;

            await using var fileStream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read);
            var insertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
            insertRequest.ResponseReceived += v => uploadedVideoId = v.Id;
            insertRequest.ProgressChanged += progress =>
            {
                if (progress.Status == UploadStatus.Failed)
                {
                    uploadError = progress.Exception?.Message ?? "Lỗi tải video lên YouTube.";
                }
            };

            var result = await insertRequest.UploadAsync(ct);
            if (result.Status == UploadStatus.Completed && !string.IsNullOrEmpty(uploadedVideoId))
            {
                _logger.LogInformation("Đã đăng video YouTube unlisted thành công cho bài học {LessonId}: {VideoId}", lesson.Id, uploadedVideoId);
                return (true, uploadedVideoId, null);
            }

            uploadError ??= result.Exception?.Message ?? "Tải video lên YouTube thất bại.";
            _logger.LogError("Upload YouTube thất bại cho bài học {LessonId}: {Error}", lesson.Id, uploadError);
            return (false, null, uploadError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ngoại lệ khi upload YouTube bài học {LessonId}", lesson.Id);
            return (false, null, $"Lỗi kết nối YouTube API: {ex.Message}");
        }
    }
}
