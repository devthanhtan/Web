using System.ComponentModel.DataAnnotations;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class Course
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên khóa học")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(220)]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn")]
    [StringLength(500)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết")]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    [StringLength(100)]
    public string? IntroYoutubeId { get; set; }

    [StringLength(50)]
    public string Level { get; set; } = "Cơ bản";

    [Range(0, 100000000, ErrorMessage = "Học phí phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    [Required]
    public string InstructorId { get; set; } = string.Empty;
    public ApplicationUser Instructor { get; set; } = null!;

    public CourseStatus Status { get; set; } = CourseStatus.Draft;

    [StringLength(1000)]
    public string? RejectionReason { get; set; }

    public string? ReviewedByAdminId { get; set; }
    public ApplicationUser? ReviewedByAdmin { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<CourseQuestion> QnaQuestions { get; set; } = new List<CourseQuestion>();
}
