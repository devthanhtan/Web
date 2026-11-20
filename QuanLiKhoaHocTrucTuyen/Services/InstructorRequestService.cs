using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class InstructorRequestService : IInstructorRequestService
{
    private static readonly string[] AllowedExtensions = [".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg", ".txt"];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorageService _fileStorage;
    private readonly IAppEmailService _email;
    private readonly INotificationService _notifications;
    private readonly AppFeaturesOptions _features;

    public InstructorRequestService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileStorageService fileStorage,
        IAppEmailService email,
        INotificationService notifications,
        IOptions<AppFeaturesOptions> features)
    {
        _db = db;
        _userManager = userManager;
        _fileStorage = fileStorage;
        _email = email;
        _notifications = notifications;
        _features = features.Value;
    }

    public async Task<InstructorRequest?> GetLatestForUserAsync(string userId)
    {
        return await _db.InstructorRequests
            .Include(r => r.User)
            .Include(r => r.ReviewedByAdmin)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.SubmittedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<InstructorRequest>> GetAllAsync(InstructorRequestStatus? statusFilter = null)
    {
        var query = _db.InstructorRequests
            .Include(r => r.User)
            .Include(r => r.ReviewedByAdmin)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(r => r.Status == statusFilter.Value);
        }

        return await query.OrderBy(r => r.Status).ThenByDescending(r => r.SubmittedAt).ToListAsync();
    }

    public async Task<InstructorRequest?> GetByIdAsync(int id)
    {
        return await _db.InstructorRequests
            .Include(r => r.User)
            .Include(r => r.ReviewedByAdmin)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<(bool Success, string? Error)> SubmitOrResubmitAsync(
        string userId,
        string expertise,
        int experienceYears,
        string bio,
        IFormFile cvFile,
        IFormFile? certificateFile)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "Không tìm thấy tài khoản.");

        if (await _userManager.IsInRoleAsync(user, AppRoles.Instructor))
            return (false, "Tài khoản của bạn đã có quyền Giảng viên.");

        var existingPending = await _db.InstructorRequests
            .AnyAsync(r => r.UserId == userId && r.Status == InstructorRequestStatus.Pending);
        if (existingPending)
            return (false, "Bạn đã có một yêu cầu đang chờ Admin thẩm định.");

        var cvResult = await _fileStorage.SaveFileAsync(cvFile, "instructor-requests", AllowedExtensions, _features.MaxDocumentSizeBytes);
        if (!cvResult.Success || cvResult.RelativePath == null)
            return (false, cvResult.Error ?? "Không thể tải lên tệp CV.");

        string? certPath = null;
        if (certificateFile != null && certificateFile.Length > 0)
        {
            var certResult = await _fileStorage.SaveFileAsync(certificateFile, "instructor-requests", AllowedExtensions, _features.MaxDocumentSizeBytes);
            if (!certResult.Success)
            {
                _fileStorage.DeleteRelativeFile(cvResult.RelativePath);
                return (false, certResult.Error ?? "Không thể tải lên tệp chứng chỉ.");
            }
            certPath = certResult.RelativePath;
        }

        var request = new InstructorRequest
        {
            UserId = userId,
            Expertise = expertise.Trim(),
            ExperienceYears = experienceYears,
            Bio = bio.Trim(),
            CvFilePath = cvResult.RelativePath,
            CertificateFilePath = certPath,
            Status = InstructorRequestStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        _db.InstructorRequests.Add(request);
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ApproveAsync(int requestId, string adminId)
    {
        var request = await _db.InstructorRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null) return (false, "Không tìm thấy yêu cầu nâng cấp.");
        if (request.Status != InstructorRequestStatus.Pending)
            return (false, "Yêu cầu này đã được xử lý trước đó.");

        if (!await _userManager.IsInRoleAsync(request.User, AppRoles.Instructor))
        {
            var roleRes = await _userManager.AddToRoleAsync(request.User, AppRoles.Instructor);
            if (!roleRes.Succeeded)
                return (false, "Không thể cấp quyền Giảng viên.");
        }

        request.Status = InstructorRequestStatus.Approved;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByAdminId = adminId;
        request.RejectionReason = null;
        await _db.SaveChangesAsync();

        await _notifications.CreateAsync(
            request.UserId,
            "Yêu cầu trở thành Giảng viên đã được duyệt",
            "Chúc mừng! Bạn đã được cấp quyền Giảng viên và có thể tạo khóa học ngay bây giờ.",
            "/Instructor/Dashboard");

        if (!string.IsNullOrWhiteSpace(request.User.Email))
        {
            await _email.SendEmailAsync(
                request.User.Email,
                "[EduSpark] Chúc mừng! Hồ sơ Giảng viên đã được phê duyệt",
                $"<p>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(request.User.FullName)}</strong>,</p><p>Hồ sơ nâng cấp Giảng viên của bạn đã được Admin phê duyệt. Bạn vui lòng đăng xuất và đăng nhập lại (hoặc truy cập Kênh Giảng Viên) để bắt đầu xuất bản khóa học.</p>");
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RejectAsync(int requestId, string adminId, string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return (false, "Vui lòng nhập lý do từ chối.");

        var request = await _db.InstructorRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null) return (false, "Không tìm thấy yêu cầu nâng cấp.");
        if (request.Status != InstructorRequestStatus.Pending)
            return (false, "Yêu cầu này đã được xử lý trước đó.");

        request.Status = InstructorRequestStatus.Rejected;
        request.RejectionReason = rejectionReason.Trim();
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByAdminId = adminId;
        await _db.SaveChangesAsync();

        await _notifications.CreateAsync(
            request.UserId,
            "Hồ sơ Giảng viên cần bổ sung",
            $"Yêu cầu bị từ chối với lý do: {request.RejectionReason}. Bạn có thể bổ sung hồ sơ và gửi lại.",
            "/Student/InstructorApplication");

        if (!string.IsNullOrWhiteSpace(request.User.Email))
        {
            await _email.SendEmailAsync(
                request.User.Email,
                "[EduSpark] Thông báo kết quả thẩm định hồ sơ Giảng viên",
                $"<p>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(request.User.FullName)}</strong>,</p><p>Hồ sơ Giảng viên của bạn chưa đạt yêu cầu với lý do:</p><blockquote>{System.Net.WebUtility.HtmlEncode(request.RejectionReason)}</blockquote><p>Bạn có thể bổ sung hồ sơ và gửi xét duyệt lại trong mục Hồ sơ Học viên.</p>");
        }

        return (true, null);
    }
}
