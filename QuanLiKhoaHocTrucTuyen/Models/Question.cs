using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class Question
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    [StringLength(120)]
    public string? Topic { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung câu hỏi")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đáp án A")]
    [StringLength(500)]
    public string OptionA { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đáp án B")]
    [StringLength(500)]
    public string OptionB { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đáp án C")]
    [StringLength(500)]
    public string OptionC { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đáp án D")]
    [StringLength(500)]
    public string OptionD { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[ABCD]$", ErrorMessage = "Đáp án đúng phải là A, B, C hoặc D")]
    [StringLength(1)]
    public string CorrectOption { get; set; } = "A";

    [StringLength(1000)]
    public string? Explanation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
}
