using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class QuizService : IQuizService
{
    private readonly ApplicationDbContext _db;
    private readonly ICertificateService _certificates;
    private readonly INotificationService _notifications;

    public QuizService(
        ApplicationDbContext db,
        ICertificateService certificates,
        INotificationService notifications)
    {
        _db = db;
        _certificates = certificates;
        _notifications = notifications;
    }

    public bool IsFinalExamUnlocked(Enrollment enrollment)
    {
        return enrollment.ProgressPercent >= 100m;
    }

    public async Task<QuizEligibilityResult> CheckEligibilityAsync(string userId, int quizId)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.QuizQuestions)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz == null)
            return new QuizEligibilityResult(false, "Không tìm thấy bài kiểm tra.", 0, 0);

        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == quiz.CourseId);
        if (enrollment == null)
            return new QuizEligibilityResult(false, "Bạn chưa sở hữu khóa học này.", 0, quiz.MaxAttempts);

        // BR-E3: Final exam only unlocks when 100% of lessons are completed
        if (quiz.IsFinalExam && !IsFinalExamUnlocked(enrollment))
            return new QuizEligibilityResult(
                false,
                $"Bạn cần hoàn thành 100% bài học (hiện tại: {enrollment.ProgressPercent:0.#}%) mới có thể làm bài thi cuối khóa.",
                0,
                quiz.MaxAttempts);

        if (!quiz.QuizQuestions.Any())
            return new QuizEligibilityResult(false, "Bài kiểm tra chưa có câu hỏi nào.", 0, quiz.MaxAttempts);

        var attemptsUsed = await _db.QuizAttempts.CountAsync(a => a.QuizId == quizId && a.UserId == userId && a.SubmittedAt != null);
        if (quiz.MaxAttempts > 0 && attemptsUsed >= quiz.MaxAttempts)
        {
            var hasPassed = await _db.QuizAttempts.AnyAsync(a => a.QuizId == quizId && a.UserId == userId && a.IsPassed);
            var msg = hasPassed
                ? "Bạn đã vượt qua bài kiểm tra này và sử dụng hết số lần làm bài."
                : $"Bạn đã hết lượt thi lại ({attemptsUsed}/{quiz.MaxAttempts} lần).";
            return new QuizEligibilityResult(false, msg, attemptsUsed, quiz.MaxAttempts);
        }

        return new QuizEligibilityResult(true, null, attemptsUsed, quiz.MaxAttempts);
    }

    public async Task<(bool Success, QuizAttempt? Attempt, string? Error)> StartAttemptAsync(string userId, int quizId)
    {
        // Resume an active unsubmitted attempt if not expired yet
        var active = await _db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.QuizQuestions.OrderBy(qq => qq.SortOrder)).ThenInclude(qq => qq.Question)
            .FirstOrDefaultAsync(a => a.QuizId == quizId && a.UserId == userId && a.SubmittedAt == null && a.ExpiresAt > DateTime.UtcNow);

        if (active != null)
            return (true, active, null);

        var check = await CheckEligibilityAsync(userId, quizId);
        if (!check.CanStart)
            return (false, null, check.Reason);

        var quiz = await _db.Quizzes
            .Include(q => q.QuizQuestions)
            .FirstAsync(q => q.Id == quizId);

        var now = DateTime.UtcNow;
        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            AttemptNumber = check.AttemptsUsed + 1,
            StartedAt = now,
            ExpiresAt = now.AddMinutes(quiz.DurationMinutes),
            TotalQuestions = quiz.QuizQuestions.Count
        };

        _db.QuizAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        var loaded = await _db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.QuizQuestions.OrderBy(qq => qq.SortOrder)).ThenInclude(qq => qq.Question)
            .FirstAsync(a => a.Id == attempt.Id);

        return (true, loaded, null);
    }

    public async Task<(bool Success, QuizAttempt? Attempt, Certificate? IssuedCertificate, string? Error)> SubmitAttemptAsync(
        string userId,
        int attemptId,
        Dictionary<int, string> selectedAnswers,
        string baseUrl)
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.QuizQuestions).ThenInclude(qq => qq.Question)
            .Include(a => a.Quiz).ThenInclude(q => q.Course)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId);

        if (attempt == null)
            return (false, null, null, "Không tìm thấy lượt làm bài.");

        if (attempt.SubmittedAt != null)
            return (true, attempt, null, null); // Idempotent submit

        var now = DateTime.UtcNow;
        var graceLimit = attempt.ExpiresAt.AddSeconds(20);
        // Server enforces timer; if submitted well past expiry, still grades submitted answers with timestamp capped at expiry
        attempt.SubmittedAt = now > graceLimit ? attempt.ExpiresAt : now;

        int correctCount = 0;
        var questions = attempt.Quiz.QuizQuestions.Select(qq => qq.Question).ToList();
        var serializedAnswers = new Dictionary<string, string>();

        foreach (var q in questions)
        {
            selectedAnswers.TryGetValue(q.Id, out var chosen);
            var normalized = (chosen ?? "").Trim().ToUpperInvariant();
            serializedAnswers[q.Id.ToString()] = normalized;
            if (normalized == q.CorrectOption.ToUpperInvariant())
            {
                correctCount++;
            }
        }

        attempt.CorrectCount = correctCount;
        attempt.TotalQuestions = questions.Count;
        attempt.ScorePercent = questions.Count > 0
            ? Math.Round((decimal)correctCount / questions.Count * 100m, 2)
            : 0m;
        attempt.IsPassed = attempt.ScorePercent >= attempt.Quiz.PassingScorePercent;
        attempt.AnswersJson = JsonSerializer.Serialize(serializedAnswers);

        await _db.SaveChangesAsync();

        Certificate? issuedCert = null;
        if (attempt.Quiz.IsFinalExam && attempt.IsPassed)
        {
            issuedCert = await _certificates.IssueOrGetCertificateAsync(
                userId,
                attempt.Quiz.CourseId,
                attempt.Id,
                baseUrl);

            await _notifications.CreateAsync(
                userId,
                "Bạn đã nhận được chứng chỉ mới!",
                $"Chúc mừng bạn đã vượt qua bài thi cuối khóa '{attempt.Quiz.Course.Title}'. Mã chứng chỉ: {issuedCert.Code}",
                "/Student/Certificates");
        }

        return (true, attempt, issuedCert, null);
    }
}
