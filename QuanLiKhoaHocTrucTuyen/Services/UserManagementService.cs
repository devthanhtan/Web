using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<List<AdminUserSummary>> SearchUsersAsync(string? keyword, string? role)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var results = new List<AdminUserSummary>();

        foreach (var u in users)
        {
            var roles = (await _userManager.GetRolesAsync(u)).ToList();
            if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role))
                continue;

            var isLocked = u.IsLockedByAdmin || (u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow);
            results.Add(new AdminUserSummary(
                u.Id,
                u.FullName,
                u.Email ?? "",
                u.EmailConfirmed,
                isLocked,
                u.CreatedAt,
                roles));
        }

        return results;
    }

    public async Task<(bool Success, string? Error)> ToggleLockAsync(string targetUserId, string currentAdminId)
    {
        if (targetUserId == currentAdminId)
            return (false, "Không thể tự khóa tài khoản Quản trị viên đang đăng nhập.");

        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user == null)
            return (false, "Không tìm thấy người dùng.");

        var currentlyLocked = user.IsLockedByAdmin || (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow);

        if (currentlyLocked)
        {
            user.IsLockedByAdmin = false;
            await _userManager.SetLockoutEndDateAsync(user, null);
        }
        else
        {
            user.IsLockedByAdmin = true;
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            await _userManager.UpdateSecurityStampAsync(user);
        }

        await _userManager.UpdateAsync(user);
        return (true, null);
    }
}
