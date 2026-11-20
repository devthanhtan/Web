using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.ViewModels;

public class CourseCatalogViewModel
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinRating { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public List<Category> Categories { get; set; } = new();
    public List<CourseCardViewModel> Courses { get; set; } = new();
}

public class CourseDetailViewModel
{
    public Course Course { get; set; } = null!;
    public bool IsOwned { get; set; }
    public bool IsInCart { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int TotalLessons { get; set; }
    public int TotalDurationMinutes { get; set; }
    public List<Review> VisibleReviews { get; set; } = new();
}

public class MockPaymentViewModel
{
    public Order Order { get; set; } = null!;
    public PaymentProvider Provider { get; set; }
}
