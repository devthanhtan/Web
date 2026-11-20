using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CommissionConfig> GetLatestCommissionAsync()
    {
        return await _db.CommissionConfigs
            .Include(c => c.UpdatedByAdmin)
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync()
            ?? new CommissionConfig { PlatformRatePercent = 20m, UpdatedAt = DateTime.UtcNow };
    }

    public async Task UpdateCommissionAsync(decimal ratePercent, string? note, string adminId)
    {
        _db.CommissionConfigs.Add(new CommissionConfig
        {
            PlatformRatePercent = ratePercent,
            Note = note?.Trim(),
            UpdatedByAdminId = adminId,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<InstructorReportDto> GetInstructorReportAsync(string instructorId, string period)
    {
        period = NormalizePeriod(period);

        var courses = await _db.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Reviews)
            .Include(c => c.OrderItems).ThenInclude(oi => oi.Order)
            .AsSplitQuery()
            .Where(c => c.InstructorId == instructorId)
            .ToListAsync();

        var paidItems = courses.SelectMany(c => c.OrderItems)
            .Where(oi => oi.Order.Status == OrderStatus.Paid)
            .ToList();

        var enrollments = courses.SelectMany(c => c.Enrollments).ToList();
        var visibleReviews = courses.SelectMany(c => c.Reviews).Where(r => !r.IsHidden).ToList();

        var buckets = BuildBuckets(period, paidItems, enrollments, []);

        return new InstructorReportDto
        {
            Period = period,
            TotalCourses = courses.Count,
            ApprovedCourses = courses.Count(c => c.Status == CourseStatus.Approved),
            TotalStudents = enrollments.Count,
            TotalNetRevenue = paidItems.Sum(i => i.InstructorAmount),
            AverageRating = visibleReviews.Any() ? Math.Round(visibleReviews.Average(r => r.Rating), 1) : 0,
            UnreadQna = await _db.CourseQuestions.CountAsync(q => q.Course.InstructorId == instructorId && q.AnswerText == null),
            Buckets = buckets,
            CourseStats = courses.Select(c =>
            {
                var cReviews = c.Reviews.Where(r => !r.IsHidden).ToList();
                var cPaid = c.OrderItems.Where(oi => oi.Order.Status == OrderStatus.Paid).ToList();
                return new CourseEngagementStat(
                    c.Id,
                    c.Title,
                    "",
                    c.Enrollments.Count,
                    c.Enrollments.Any() ? Math.Round(c.Enrollments.Average(e => e.ProgressPercent), 1) : 0m,
                    cReviews.Any() ? Math.Round(cReviews.Average(r => r.Rating), 1) : 0,
                    cPaid.Sum(i => i.InstructorAmount));
            }).OrderByDescending(x => x.Revenue).ToList()
        };
    }

    public async Task<AdminReportDto> GetAdminReportAsync(string period)
    {
        period = NormalizePeriod(period);

        var paidItems = await _db.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Course)
            .Where(oi => oi.Order.Status == OrderStatus.Paid)
            .ToListAsync();

        var enrollments = await _db.Enrollments.ToListAsync();
        var users = await _db.Users.ToListAsync();

        var courses = await _db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Enrollments)
            .Include(c => c.Reviews)
            .Include(c => c.OrderItems).ThenInclude(oi => oi.Order)
            .AsSplitQuery()
            .ToListAsync();

        var latestCommission = await GetLatestCommissionAsync();

        return new AdminReportDto
        {
            Period = period,
            TotalUsers = users.Count,
            PendingInstructorRequests = await _db.InstructorRequests.CountAsync(r => r.Status == InstructorRequestStatus.Pending),
            PendingCourses = courses.Count(c => c.Status == CourseStatus.PendingReview),
            TotalCourses = courses.Count,
            TotalGrossRevenue = paidItems.Sum(i => i.UnitPrice),
            TotalPlatformCommission = paidItems.Sum(i => i.PlatformAmount),
            TotalInstructorPayout = paidItems.Sum(i => i.InstructorAmount),
            CurrentCommissionRate = latestCommission.PlatformRatePercent,
            Buckets = BuildBuckets(period, paidItems, enrollments, users),
            TopCourses = courses
                .Where(c => c.Status == CourseStatus.Approved)
                .Select(c =>
                {
                    var visibleReviews = c.Reviews.Where(r => !r.IsHidden).ToList();
                    var cPaid = c.OrderItems.Where(oi => oi.Order.Status == OrderStatus.Paid).ToList();
                    return new CourseEngagementStat(
                        c.Id,
                        c.Title,
                        c.Instructor.FullName,
                        c.Enrollments.Count,
                        c.Enrollments.Any() ? Math.Round(c.Enrollments.Average(e => e.ProgressPercent), 1) : 0m,
                        visibleReviews.Any() ? Math.Round(visibleReviews.Average(r => r.Rating), 1) : 0,
                        cPaid.Sum(i => i.UnitPrice));
                })
                .OrderByDescending(x => x.EnrollmentCount)
                .ThenByDescending(x => x.AvgProgressPercent)
                .Take(10)
                .ToList()
        };
    }

    public async Task<byte[]> ExportAdminReportCsvAsync(string period)
    {
        var report = await GetAdminReportAsync(period);
        var sb = new StringBuilder();
        sb.AppendLine("Ky,DoanhThuTong,HoaHongNenTang,ThuNhapGiangVien,DangKyMoi,TaiKhoanMoi");
        foreach (var b in report.Buckets)
        {
            sb.AppendLine($"\"{b.Label}\",{b.GrossRevenue.ToString("0.00", CultureInfo.InvariantCulture)},{b.PlatformAmount.ToString("0.00", CultureInfo.InvariantCulture)},{b.InstructorAmount.ToString("0.00", CultureInfo.InvariantCulture)},{b.NewEnrollments},{b.NewUsers}");
        }
        sb.AppendLine();
        sb.AppendLine("KhoaHoc,GiangVien,HocVien,TienDoTB(%),DanhGiaTB,DoanhThu");
        foreach (var c in report.TopCourses)
        {
            sb.AppendLine($"\"{c.Title.Replace("\"", "\"\"")}\",\"{c.InstructorName.Replace("\"", "\"\"")}\",{c.EnrollmentCount},{c.AvgProgressPercent.ToString("0.0", CultureInfo.InvariantCulture)},{c.AvgRating.ToString("0.0", CultureInfo.InvariantCulture)},{c.Revenue.ToString("0.00", CultureInfo.InvariantCulture)}");
        }

        // UTF-8 BOM for clean Vietnamese display in Microsoft Excel
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        return preamble.Concat(body).ToArray();
    }

    private static string NormalizePeriod(string? period) =>
        string.Equals(period, "week", StringComparison.OrdinalIgnoreCase) ? "week" : "month";

    private static List<PeriodBucketStat> BuildBuckets(
        string period,
        List<OrderItem> paidItems,
        List<Enrollment> enrollments,
        List<ApplicationUser> users)
    {
        var now = DateTime.UtcNow;
        var results = new List<PeriodBucketStat>();

        if (period == "week")
        {
            // Last 8 weeks
            var currentWeekStart = StartOfWeek(now.Date);
            for (int i = 7; i >= 0; i--)
            {
                var start = currentWeekStart.AddDays(-7 * i);
                var end = start.AddDays(7);

                var itemsInPeriod = paidItems.Where(x => (x.Order.PaidAt ?? x.Order.CreatedAt) >= start && (x.Order.PaidAt ?? x.Order.CreatedAt) < end).ToList();
                var enrollsInPeriod = enrollments.Count(e => e.EnrolledAt >= start && e.EnrolledAt < end);
                var usersInPeriod = users.Count(u => u.CreatedAt >= start && u.CreatedAt < end);

                results.Add(new PeriodBucketStat(
                    $"{start:dd/MM} - {end.AddDays(-1):dd/MM}",
                    itemsInPeriod.Sum(x => x.UnitPrice),
                    itemsInPeriod.Sum(x => x.PlatformAmount),
                    itemsInPeriod.Sum(x => x.InstructorAmount),
                    enrollsInPeriod,
                    usersInPeriod));
            }
        }
        else
        {
            // Last 6 months
            var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            for (int i = 5; i >= 0; i--)
            {
                var start = currentMonthStart.AddMonths(-i);
                var end = start.AddMonths(1);

                var itemsInPeriod = paidItems.Where(x => (x.Order.PaidAt ?? x.Order.CreatedAt) >= start && (x.Order.PaidAt ?? x.Order.CreatedAt) < end).ToList();
                var enrollsInPeriod = enrollments.Count(e => e.EnrolledAt >= start && e.EnrolledAt < end);
                var usersInPeriod = users.Count(u => u.CreatedAt >= start && u.CreatedAt < end);

                results.Add(new PeriodBucketStat(
                    $"T{start:MM/yyyy}",
                    itemsInPeriod.Sum(x => x.UnitPrice),
                    itemsInPeriod.Sum(x => x.PlatformAmount),
                    itemsInPeriod.Sum(x => x.InstructorAmount),
                    enrollsInPeriod,
                    usersInPeriod));
            }
        }

        return results;
    }

    private static DateTime StartOfWeek(DateTime dt)
    {
        int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
        return DateTime.SpecifyKind(dt.AddDays(-1 * diff).Date, DateTimeKind.Utc);
    }
}
