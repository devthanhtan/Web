namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public record AdminUserSummary(
    string Id,
    string FullName,
    string Email,
    bool EmailConfirmed,
    bool IsLocked,
    DateTime CreatedAt,
    List<string> Roles);

public interface IUserManagementService
{
    Task<List<AdminUserSummary>> SearchUsersAsync(string? keyword, string? role);
    Task<(bool Success, string? Error)> ToggleLockAsync(string targetUserId, string currentAdminId);
}
