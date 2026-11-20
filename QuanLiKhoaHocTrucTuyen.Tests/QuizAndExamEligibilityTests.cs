using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services;

namespace QuanLiKhoaHocTrucTuyen.Tests;

public class QuizAndExamEligibilityTests
{
    [Fact]
    public async Task FinalExam_BlockedBelow100Percent_AndUnlockedAt100Percent()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        var user = new ApplicationUser { Id = "stu1", UserName = "s@t.vn", Email = "s@t.vn", FullName = "SV" };
        var inst = new ApplicationUser { Id = "inst1", UserName = "i@t.vn", Email = "i@t.vn", FullName = "GV" };
        var course = new Course { Id = 1, Title = "Course", Slug = "c", ShortDescription = "s", Description = "d", InstructorId = "inst1" };
        var question = new Question { Id = 1, CourseId = 1, Content = "Q?", OptionA = "A", OptionB = "B", OptionC = "C", OptionD = "D", CorrectOption = "A" };
        var finalQuiz = new Quiz
        {
            Id = 1,
            CourseId = 1,
            Title = "Final",
            IsFinalExam = true,
            DurationMinutes = 10,
            PassingScorePercent = 70,
            MaxAttempts = 2,
            QuizQuestions = [new QuizQuestion { QuizId = 1, QuestionId = 1, SortOrder = 1 }]
        };
        var enrollment = new Enrollment { Id = 1, UserId = "stu1", CourseId = 1, ProgressPercent = 99m };

        db.Users.AddRange(user, inst);
        db.Courses.Add(course);
        db.Questions.Add(question);
        db.Quizzes.Add(finalQuiz);
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var certSvc = new CertificateService(db, new TestHelpers.FakeWebHostEnvironment());
        var quizSvc = new QuizService(db, certSvc, new NotificationService(db));

        // At 99%: blocked (BR-E3)
        var blocked = await quizSvc.CheckEligibilityAsync("stu1", 1);
        Assert.False(blocked.CanStart);

        // At 100%: unlocked
        enrollment.ProgressPercent = 100m;
        await db.SaveChangesAsync();

        var unlocked = await quizSvc.CheckEligibilityAsync("stu1", 1);
        Assert.True(unlocked.CanStart);

        // Start & submit failing attempt -> allows retake up to MaxAttempts = 2 (BR-E6)
        var start1 = await quizSvc.StartAttemptAsync("stu1", 1);
        Assert.True(start1.Success);
        var sub1 = await quizSvc.SubmitAttemptAsync("stu1", start1.Attempt!.Id, new Dictionary<int, string> { [1] = "B" }, "https://localhost");
        Assert.False(sub1.Attempt!.IsPassed);
        Assert.Null(sub1.IssuedCertificate);

        var retakeEligible = await quizSvc.CheckEligibilityAsync("stu1", 1);
        Assert.True(retakeEligible.CanStart);
        Assert.Equal(1, retakeEligible.AttemptsUsed);
    }
}
