namespace QuanLiKhoaHocTrucTuyen.Models;

public class QuizQuestion
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public int SortOrder { get; set; }
}
