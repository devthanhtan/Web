namespace QuanLiKhoaHocTrucTuyen.Models;

public class QuizAttempt
{
    public int Id { get; set; }

    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int AttemptNumber { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public decimal ScorePercent { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsPassed { get; set; }

    public string? AnswersJson { get; set; }
}
