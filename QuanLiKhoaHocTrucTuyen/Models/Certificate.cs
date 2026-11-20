using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class Certificate
{
    public int Id { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? QuizAttemptId { get; set; }
    public QuizAttempt? QuizAttempt { get; set; }

    [Required]
    [StringLength(150)]
    public string HolderName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CourseTitle { get; set; } = string.Empty;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(500)]
    public string QrData { get; set; } = string.Empty;

    [StringLength(500)]
    public string? PdfPath { get; set; }
}
