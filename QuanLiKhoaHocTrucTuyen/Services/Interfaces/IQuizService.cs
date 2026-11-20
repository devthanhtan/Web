using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public record QuizEligibilityResult(
    bool CanStart,
    string? Reason,
    int AttemptsUsed,
    int MaxAttempts);

public interface IQuizService
{
    Task<QuizEligibilityResult> CheckEligibilityAsync(string userId, int quizId);
    Task<(bool Success, QuizAttempt? Attempt, string? Error)> StartAttemptAsync(string userId, int quizId);
    Task<(bool Success, QuizAttempt? Attempt, Certificate? IssuedCertificate, string? Error)> SubmitAttemptAsync(
        string userId,
        int attemptId,
        Dictionary<int, string> selectedAnswers,
        string baseUrl);

    bool IsFinalExamUnlocked(Enrollment enrollment);
}
