using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services;

namespace QuanLiKhoaHocTrucTuyen.Tests;

public class LearningProgressThresholdTests
{
    [Theory]
    [InlineData(89, 100, false)]
    [InlineData(90, 100, true)]
    [InlineData(100, 100, true)]
    [InlineData(179, 200, false)]
    [InlineData(180, 200, true)]
    public void VideoCompletion_FollowsNinetyPercentRule(int watched, int duration, bool expectedCompleted)
    {
        using var db = TestHelpers.CreateInMemoryDb();
        var svc = new LearningProgressService(db, new NotificationService(db));
        Assert.Equal(expectedCompleted, svc.IsVideoCompletionReached(watched, duration));
    }

    [Fact]
    public async Task RecordWatchProgress_ClampsAntiSkipAndCompletesAt90Percent()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        var course = new Course { Id = 1, Title = "Test", Slug = "test", ShortDescription = "s", Description = "d", InstructorId = "inst" };
        var chapter = new Chapter { Id = 1, CourseId = 1, Title = "Ch1", SortOrder = 1 };
        var lesson = new Lesson { Id = 1, ChapterId = 1, Title = "L1", LessonType = LessonType.Video, DurationSeconds = 100, SortOrder = 1 };
        var enrollment = new Enrollment { Id = 1, UserId = "stu1", CourseId = 1, ProgressPercent = 0m };

        db.Courses.Add(course);
        db.Chapters.Add(chapter);
        db.Lessons.Add(lesson);
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var svc = new LearningProgressService(db, new NotificationService(db));

        // Attempt to fake 500s jump in one call -> clamped to 15s (BR-D2 anti-skip)
        var r1 = await svc.RecordWatchProgressAsync("stu1", 1, 500, 95);
        Assert.True(r1.Success);
        Assert.Equal(15, r1.WatchedSeconds);
        Assert.False(r1.LessonCompleted);

        // Simulate prior accumulated 75s, then 15s more -> reaches 90/100 (90%) -> auto-completed!
        var lp = db.LessonProgresses.First();
        lp.WatchedSeconds = 75;
        lp.LastWatchedAt = DateTime.UtcNow.AddSeconds(-20);
        await db.SaveChangesAsync();

        var r2 = await svc.RecordWatchProgressAsync("stu1", 1, 15, 95);
        Assert.True(r2.Success);
        Assert.Equal(90, r2.WatchedSeconds);
        Assert.True(r2.LessonCompleted);
        Assert.Equal(100m, r2.CourseProgressPercent);
        Assert.True(r2.FinalExamUnlocked);
    }

    [Fact]
    public async Task MarkDocumentLessonCompleted_DoesNotDuplicateTrackedProgressEntries()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        var cat = new Category { Id = 1, Name = "Dev", Slug = "dev" };
        var inst = new ApplicationUser { Id = "inst", FullName = "GV", UserName = "gv@test.vn", Email = "gv@test.vn" };
        var course = new Course { Id = 1, Title = "Test", Slug = "test", ShortDescription = "s", Description = "d", CategoryId = 1, InstructorId = "inst" };
        var chapter = new Chapter { Id = 1, CourseId = 1, Title = "Ch1", SortOrder = 1 };
        var l1 = new Lesson { Id = 1, ChapterId = 1, Title = "Doc 1", LessonType = LessonType.Document, DurationSeconds = 60, SortOrder = 1 };
        var l2 = new Lesson { Id = 2, ChapterId = 1, Title = "Vid 2", LessonType = LessonType.Video, DurationSeconds = 120, SortOrder = 2 };
        var enrollment = new Enrollment { Id = 1, UserId = "stu1", CourseId = 1, ProgressPercent = 0m };

        db.Categories.Add(cat);
        db.Users.Add(inst);
        db.Courses.Add(course);
        db.Chapters.Add(chapter);
        db.Lessons.AddRange(l1, l2);
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var svc = new LearningProgressService(db, new NotificationService(db));

        // Load enrollment into change tracker first (same pattern as LearnController.Index)
        var initial = await svc.GetEnrollmentWithCourseAsync("stu1", 1);
        Assert.NotNull(initial);

        var res = await svc.MarkDocumentLessonCompletedAsync("stu1", 1);
        Assert.True(res.Success);
        Assert.Equal(1, res.CompletedLessons);
        Assert.Equal(2, res.TotalLessons);
        Assert.Equal(50m, res.CourseProgressPercent);

        var reloaded = await svc.GetEnrollmentWithCourseAsync("stu1", 1);
        Assert.NotNull(reloaded);
        Assert.Single(reloaded!.LessonProgresses);
        var dict = reloaded.LessonProgresses.ToDictionary(p => p.LessonId, p => p);
        Assert.True(dict[1].IsCompleted);
    }
}
