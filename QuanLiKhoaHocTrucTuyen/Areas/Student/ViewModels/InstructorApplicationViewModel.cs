using System.ComponentModel.DataAnnotations;
using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.ViewModels;

public class InstructorApplicationViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập lĩnh vực chuyên môn")]
    [StringLength(200)]
    [Display(Name = "Lĩnh vực chuyên môn")]
    public string Expertise { get; set; } = string.Empty;

    [Range(0, 60, ErrorMessage = "Số năm kinh nghiệm từ 0 đến 60")]
    [Display(Name = "Số năm kinh nghiệm")]
    public int ExperienceYears { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả năng lực giảng dạy")]
    [StringLength(2000)]
    [Display(Name = "Giới thiệu kinh nghiệm & định hướng giảng dạy")]
    public string Bio { get; set; } = string.Empty;

    [Display(Name = "Tải lên CV / Hồ sơ năng lực (.pdf, .doc, .docx, .txt)")]
    public IFormFile? CvFile { get; set; }

    [Display(Name = "Chứng chỉ chuyên môn (tùy chọn)")]
    public IFormFile? CertificateFile { get; set; }

    public InstructorRequest? LatestRequest { get; set; }
    public bool IsAlreadyInstructor { get; set; }
}
