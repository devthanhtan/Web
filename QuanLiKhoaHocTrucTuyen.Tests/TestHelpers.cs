using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Tests;

internal static class TestHelpers
{
    public static ApplicationDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    public sealed class FakeAppEmailService : IAppEmailService
    {
        public Task<string?> GetLatestMockConfirmationUrlAsync(string email) => Task.FromResult<string?>(null);
        public Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
    }

    public sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    public sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "eduspark_test_wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "QuanLiKhoaHocTrucTuyen.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
    }

    public sealed class FakeYouTubeUploadService : IYouTubeUploadService
    {
        public ValueTask EnqueueLessonUploadAsync(int lessonId, CancellationToken ct = default) => ValueTask.CompletedTask;
        public Task<(bool Success, string? YoutubeVideoId, string? Error)> UploadVideoAsync(Lesson lesson, CancellationToken ct = default)
            => Task.FromResult<(bool, string?, string?)>((true, "MOCK_YT", null));
    }

    public static PaymentGatewayService CreatePaymentService(ApplicationDbContext db, string vnpSecret = "TEST_VNP_SECRET", string momoSecret = "TEST_MOMO_SECRET", VnpayOptions? vnpayOptions = null)
    {
        return new PaymentGatewayService(
            db,
            Microsoft.Extensions.Options.Options.Create(vnpayOptions ?? new VnpayOptions { TmnCode = "TESTTMN", HashSecret = vnpSecret }),
            Microsoft.Extensions.Options.Options.Create(new MomoOptions { PartnerCode = "MOMO", AccessKey = "ACC", SecretKey = momoSecret }),
            Microsoft.Extensions.Options.Options.Create(new AppFeaturesOptions { UseMockServices = true }),
            new FakeHttpClientFactory(),
            new NotificationService(db),
            new FakeAppEmailService(),
            NullLogger<PaymentGatewayService>.Instance);
    }
}
