namespace QuanLiKhoaHocTrucTuyen.Models;

public class LessonProgress
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;

    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public int WatchedSeconds { get; set; }
    public int LastPositionSeconds { get; set; }
    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;
}
