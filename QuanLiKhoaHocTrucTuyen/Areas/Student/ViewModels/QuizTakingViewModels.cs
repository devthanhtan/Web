using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.ViewModels;

public class QuizInfoViewModel
{
    public Quiz Quiz { get; set; } = null!;
    public QuizEligibilityResult Eligibility { get; set; } = null!;
    public List<QuizAttempt> PastAttempts { get; set; } = new();
    public Certificate? Certificate { get; set; }
}

public class QuizTakeViewModel
{
    public QuizAttempt Attempt { get; set; } = null!;
    public Quiz Quiz => Attempt.Quiz;
    public List<Question> Questions { get; set; } = new();
    public int RemainingSeconds { get; set; }
}

public class QuizResultViewModel
{
    public QuizAttempt Attempt { get; set; } = null!;
    public Quiz Quiz => Attempt.Quiz;
    public Dictionary<int, string> SelectedAnswers { get; set; } = new();
    public List<Question> Questions { get; set; } = new();
    public Certificate? Certificate { get; set; }
    public bool CanRetake { get; set; }
}
