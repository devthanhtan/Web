using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class CourseQuestion
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập nội dung câu hỏi")]
    [StringLength(2000)]
    public string QuestionText { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? AnswerText { get; set; }

    public string? AnsweredById { get; set; }
    public ApplicationUser? AnsweredBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
}
