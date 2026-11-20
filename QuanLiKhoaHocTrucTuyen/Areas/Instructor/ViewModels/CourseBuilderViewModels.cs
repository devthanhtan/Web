using System.ComponentModel.DataAnnotations;
using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Areas.Instructor.ViewModels;

public class CourseFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên khóa học")]
    [StringLength(200)]
    [Display(Name = "Tên khóa học")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn")]
    [StringLength(500)]
    [Display(Name = "Mô tả ngắn")]
    public string ShortDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết")]
    [Display(Name = "Mô tả chi tiết")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Trình độ")]
    public string Level { get; set; } = "Cơ bản";

    [Range(0, 100000000, ErrorMessage = "Học phí phải từ 0 đến 100.000.000 đ")]
    [Display(Name = "Học phí (VNĐ)")]
    public decimal Price { get; set; }

    [StringLength(100)]
    [Display(Name = "YouTube Video ID giới thiệu (tùy chọn)")]
    public string? IntroYoutubeId { get; set; }

    public string? CurrentThumbnailUrl { get; set; }

    [Display(Name = "Ảnh đại diện khóa học (.jpg, .png, .webp)")]
    public IFormFile? ThumbnailFile { get; set; }
}

public class LessonFormViewModel
{
    public int Id { get; set; }
    public int ChapterId { get; set; }
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên bài học")]
    [StringLength(200)]
    [Display(Name = "Tên bài học")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Mô tả bài học")]
    public string? Description { get; set; }

    [Display(Name = "Loại bài học")]
    public LessonType LessonType { get; set; } = LessonType.Video;

    [Display(Name = "Cho phép học thử miễn phí")]
    public bool IsFreePreview { get; set; }

    [Range(0, 86400)]
    [Display(Name = "Thời lượng video (giây)")]
    public int DurationSeconds { get; set; } = 600;

    [Display(Name = "Nội dung bài học dạng tài liệu (HTML/Text)")]
    public string? ContentHtml { get; set; }

    [Display(Name = "Tải lên file video (.mp4, .webm, .mov)")]
    public IFormFile? VideoFile { get; set; }

    [Display(Name = "Đính kèm tài liệu (.pdf, .docx, .pptx, .zip, .txt)")]
    public IFormFile? AttachmentFile { get; set; }
}
