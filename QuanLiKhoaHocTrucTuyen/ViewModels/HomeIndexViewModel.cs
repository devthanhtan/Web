using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.ViewModels;

public class CourseCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int StudentCount { get; set; }
}

public class HomeIndexViewModel
{
    public List<Category> Categories { get; set; } = new();
    public List<CourseCardViewModel> FeaturedCourses { get; set; } = new();
    public int TotalCourses { get; set; }
    public int TotalStudents { get; set; }
    public int TotalInstructors { get; set; }
}
