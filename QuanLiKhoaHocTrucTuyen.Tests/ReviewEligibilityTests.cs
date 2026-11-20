using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services;

namespace QuanLiKhoaHocTrucTuyen.Tests;

public class ReviewEligibilityTests
{
    [Fact]
    public async Task SaveReview_BlockedBelow50Percent_AllowedAt50Percent_AndAllowsSingleUpdate()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        var enrollment = new Enrollment { Id = 1, UserId = "stu1", CourseId = 1, ProgressPercent = 49.99m };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var svc = new ReviewService(db);

        // BR-F1/F2: Below 50% blocked
        var blocked = await svc.SaveReviewAsync("stu1", 1, 5, "Quá hay");
        Assert.False(blocked.Success);

        // Reach 50% -> allowed
        enrollment.ProgressPercent = 50.00m;
        await db.SaveChangesAsync();

        var ok = await svc.SaveReviewAsync("stu1", 1, 4, "Tốt");
        Assert.True(ok.Success);
        Assert.Equal(1, await db.Reviews.CountAsync());

        // BR-F3: Updating existing review edits in place instead of inserting duplicate
        var updated = await svc.SaveReviewAsync("stu1", 1, 5, "Cập nhật 5 sao");
        Assert.True(updated.Success);
        Assert.Equal(1, await db.Reviews.CountAsync());
        var review = await db.Reviews.FirstAsync();
        Assert.Equal(5, review.Rating);
        Assert.Equal("Cập nhật 5 sao", review.Comment);
    }
}
