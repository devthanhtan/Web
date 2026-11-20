using System.ComponentModel.DataAnnotations;
using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Areas.Instructor.ViewModels;

public class QuestionFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }

    [StringLength(120)]
    [Display(Name = "Chủ đề / Chương")]
    public string? Topic { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung câu hỏi")]
    [Display(Name = "Nội dung câu hỏi")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đáp án A")]
    [StringLength(500)]
    [Display(Name = "Đáp án A")]
    public string OptionA { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đáp án B")]
    [StringLength(500)]
    [Display(Name = "Đáp án B")]
    public string OptionB { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đáp án C")]
    [StringLength(500)]
    [Display(Name = "Đáp án C")]
    public string OptionC { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đáp án D")]
    [StringLength(500)]
    [Display(Name = "Đáp án D")]
    public string OptionD { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[ABCD]$")]
    [Display(Name = "Đáp án đúng")]
    public string CorrectOption { get; set; } = "A";

    [StringLength(1000)]
    [Display(Name = "Giải thích đáp án (tùy chọn)")]
    public string? Explanation { get; set; }
}

public class QuestionImportViewModel
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn file .xlsx hoặc .csv")]
    public IFormFile? File { get; set; }

    public List<string> RowErrors { get; set; } = new();
}

public class QuizFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Display(Name = "Thuộc chương (để trống nếu là Bài thi cuối khóa)")]
    public int? ChapterId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài kiểm tra")]
    [StringLength(200)]
    [Display(Name = "Tiêu đề bài kiểm tra")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Đây là bài thi cuối khóa (cấp chứng chỉ khi đạt)")]
    public bool IsFinalExam { get; set; }

    [Range(1, 300)]
    [Display(Name = "Thời gian làm bài (phút)")]
    public int DurationMinutes { get; set; } = 15;

    [Range(1, 100)]
    [Display(Name = "Điểm chuẩn đạt (%)")]
    public int PassingScorePercent { get; set; } = 70;

    [Range(0, 50)]
    [Display(Name = "Số lần thi lại tối đa (0 = không giới hạn)")]
    public int MaxAttempts { get; set; } = 3;

    public List<int> SelectedQuestionIds { get; set; } = new();
    public List<Question> AvailableQuestions { get; set; } = new();
    public List<Chapter> CourseChapters { get; set; } = new();
}
