using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.ViewModels;

public class TrackWatchProgressRequest
{
    public int LessonId { get; set; }
    public int DeltaWatchedSeconds { get; set; }
    public int CurrentPositionSeconds { get; set; }
}

public class LearningRoomViewModel
{
    public Enrollment Enrollment { get; set; } = null!;
    public Course Course => Enrollment.Course;
    public Lesson? CurrentLesson { get; set; }
    public LessonProgress? CurrentProgress { get; set; }
    public Dictionary<int, LessonProgress> ProgressByLessonId { get; set; } = new();
    public int CompletedLessonsCount { get; set; }
    public int TotalLessonsCount { get; set; }
    public bool CanTakeFinalExam => Enrollment.ProgressPercent >= 100m;
    public bool CanReview => Enrollment.ProgressPercent >= 50m;
    public Review? ExistingReview { get; set; }
    public Certificate? ExistingCertificate { get; set; }
    public List<CourseQuestion> Questions { get; set; } = new();
}
