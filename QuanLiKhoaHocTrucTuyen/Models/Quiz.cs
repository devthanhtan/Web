using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class Quiz
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? ChapterId { get; set; }
    public Chapter? Chapter { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài kiểm tra")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsFinalExam { get; set; }

    [Range(1, 300, ErrorMessage = "Thời gian làm bài từ 1 đến 300 phút")]
    public int DurationMinutes { get; set; } = 15;

    [Range(1, 100, ErrorMessage = "Điểm chuẩn đạt từ 1% đến 100%")]
    public int PassingScorePercent { get; set; } = 70;

    [Range(0, 50, ErrorMessage = "Số lần thi lại tối đa từ 0 (không giới hạn) đến 50")]
    public int MaxAttempts { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
