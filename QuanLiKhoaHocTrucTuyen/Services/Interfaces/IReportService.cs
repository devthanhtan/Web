using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public record PeriodBucketStat(
    string Label,
    decimal GrossRevenue,
    decimal PlatformAmount,
    decimal InstructorAmount,
    int NewEnrollments,
    int NewUsers);

public record CourseEngagementStat(
    int CourseId,
    string Title,
    string InstructorName,
    int EnrollmentCount,
    decimal AvgProgressPercent,
    double AvgRating,
    decimal Revenue);

public class InstructorReportDto
{
    public string Period { get; set; } = "month";
    public int TotalCourses { get; set; }
    public int ApprovedCourses { get; set; }
    public int TotalStudents { get; set; }
    public decimal TotalNetRevenue { get; set; }
    public double AverageRating { get; set; }
    public int UnreadQna { get; set; }
    public List<PeriodBucketStat> Buckets { get; set; } = new();
    public List<CourseEngagementStat> CourseStats { get; set; } = new();
}

public class AdminReportDto
{
    public string Period { get; set; } = "month";
    public int TotalUsers { get; set; }
    public int PendingInstructorRequests { get; set; }
    public int PendingCourses { get; set; }
    public int TotalCourses { get; set; }
    public decimal TotalGrossRevenue { get; set; }
    public decimal TotalPlatformCommission { get; set; }
    public decimal TotalInstructorPayout { get; set; }
    public decimal CurrentCommissionRate { get; set; }
    public List<PeriodBucketStat> Buckets { get; set; } = new();
    public List<CourseEngagementStat> TopCourses { get; set; } = new();
}

public interface IReportService
{
    Task<InstructorReportDto> GetInstructorReportAsync(string instructorId, string period);
    Task<AdminReportDto> GetAdminReportAsync(string period);
    Task<byte[]> ExportAdminReportCsvAsync(string period);
    Task<CommissionConfig> GetLatestCommissionAsync();
    Task UpdateCommissionAsync(decimal ratePercent, string? note, string adminId);
}
