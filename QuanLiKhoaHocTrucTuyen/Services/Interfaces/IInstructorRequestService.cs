using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface IInstructorRequestService
{
    Task<InstructorRequest?> GetLatestForUserAsync(string userId);
    Task<List<InstructorRequest>> GetAllAsync(InstructorRequestStatus? statusFilter = null);
    Task<InstructorRequest?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> SubmitOrResubmitAsync(
        string userId,
        string expertise,
        int experienceYears,
        string bio,
        IFormFile cvFile,
        IFormFile? certificateFile);

    Task<(bool Success, string? Error)> ApproveAsync(int requestId, string adminId);
    Task<(bool Success, string? Error)> RejectAsync(int requestId, string adminId, string rejectionReason);
}
