using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.ViewModels;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    [StringLength(120)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    [Display(Name = "Chức danh / Giới thiệu ngắn")]
    public string? Headline { get; set; }

    [StringLength(2000)]
    [Display(Name = "Tiểu sử / Thông tin giới thiệu")]
    public string? Bio { get; set; }

    public string? CurrentAvatarUrl { get; set; }

    [Display(Name = "Cập nhật ảnh đại diện (.jpg, .png, .webp)")]
    public IFormFile? AvatarFile { get; set; }
}
