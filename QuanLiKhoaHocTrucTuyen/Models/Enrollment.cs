namespace QuanLiKhoaHocTrucTuyen.Models;

public class Enrollment
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public decimal ProgressPercent { get; set; }

    public DateTime? CompletedAt { get; set; }

    public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
}
