using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class InstructorRequest
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập lĩnh vực chuyên môn")]
    [StringLength(200)]
    public string Expertise { get; set; } = string.Empty;

    [Range(0, 60, ErrorMessage = "Số năm kinh nghiệm phải từ 0 đến 60")]
    public int ExperienceYears { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giới thiệu năng lực giảng dạy")]
    [StringLength(2000)]
    public string Bio { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string CvFilePath { get; set; } = string.Empty;

    [StringLength(500)]
    public string? CertificateFilePath { get; set; }

    public InstructorRequestStatus Status { get; set; } = InstructorRequestStatus.Pending;

    [StringLength(1000)]
    public string? RejectionReason { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedByAdminId { get; set; }
    public ApplicationUser? ReviewedByAdmin { get; set; }
}
