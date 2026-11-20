using System.ComponentModel.DataAnnotations;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Admin.ViewModels;

public class UsersIndexViewModel
{
    public string? Keyword { get; set; }
    public string? Role { get; set; }
    public List<AdminUserSummary> Users { get; set; } = new();
}

public class InstructorRequestsIndexViewModel
{
    public InstructorRequestStatus? StatusFilter { get; set; }
    public List<InstructorRequest> Requests { get; set; } = new();
}

public class RejectRequestInputModel
{
    public int RequestId { get; set; }

    [Required(ErrorMessage = "Bắt buộc nhập lý do từ chối.")]
    [StringLength(1000)]
    public string RejectionReason { get; set; } = string.Empty;
}

public class AdminCoursesIndexViewModel
{
    public CourseStatus? StatusFilter { get; set; }
    public List<Course> Courses { get; set; } = new();
}

public class RejectCourseInputModel
{
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Bắt buộc nhập lý do từ chối khóa học.")]
    [StringLength(1000)]
    public string RejectionReason { get; set; } = string.Empty;
}
