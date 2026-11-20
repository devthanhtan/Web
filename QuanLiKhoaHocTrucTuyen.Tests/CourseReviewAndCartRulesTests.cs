using Microsoft.Extensions.Options;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services;

namespace QuanLiKhoaHocTrucTuyen.Tests;

public class CourseReviewAndCartRulesTests
{
    [Fact]
    public async Task SubmitForReview_BlocksWhenVideoNotReady()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        db.Categories.Add(new Category { Id = 1, Name = "Web", Slug = "web" });
        var course = new Course
        {
            Id = 1,
            Title = "Course 1",
            Slug = "c1",
            ShortDescription = "s",
            Description = "d",
            CategoryId = 1,
            InstructorId = "inst1",
            Status = CourseStatus.Draft,
            Chapters =
            [
                new Chapter
                {
                    Id = 1,
                    Title = "Ch1",
                    SortOrder = 1,
                    Lessons =
                    [
                        new Lesson { Id = 1, Title = "Video Processing", LessonType = LessonType.Video, VideoStatus = VideoUploadStatus.Processing }
                    ]
                }
            ]
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var env = new TestHelpers.FakeWebHostEnvironment();
        var svc = new CourseManagementService(
            db,
            new LocalFileStorageService(env),
            new TestHelpers.FakeYouTubeUploadService(),
            new NotificationService(db),
            Microsoft.Extensions.Options.Options.Create(new AppFeaturesOptions()));

        var (success, reasons) = await svc.SubmitForReviewAsync(1, "inst1");
        Assert.False(success);
        Assert.Contains(reasons, r => r.Contains("Video Processing"));
    }

    [Fact]
    public async Task AddToCart_BlocksAlreadyOwnedCourse()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        db.Courses.Add(new Course { Id = 1, Title = "C1", Slug = "c1", ShortDescription = "s", Description = "d", InstructorId = "inst", Status = CourseStatus.Approved });
        db.Enrollments.Add(new Enrollment { UserId = "stu1", CourseId = 1 });
        await db.SaveChangesAsync();

        var cart = new CartService(db);
        var (success, error) = await cart.AddToCartAsync("stu1", 1);
        Assert.False(success);
        Assert.Contains("sở hữu", error);
    }
}
