using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace QuanLiKhoaHocTrucTuyen.Models;

public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [StringLength(200)]
    public string? Headline { get; set; }

    [StringLength(2000)]
    public string? Bio { get; set; }

    public bool IsLockedByAdmin { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InstructorRequest> InstructorRequests { get; set; } = new List<InstructorRequest>();
    public ICollection<Course> CoursesTeaching { get; set; } = new List<Course>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public ICollection<CourseQuestion> AskedQuestions { get; set; } = new List<CourseQuestion>();
}
