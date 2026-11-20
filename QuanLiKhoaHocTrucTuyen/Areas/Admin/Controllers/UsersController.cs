using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Areas.Admin.ViewModels;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly IUserManagementService _userService;

    public UsersController(IUserManagementService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> Index(string? keyword, string? role)
    {
        var vm = new UsersIndexViewModel
        {
            Keyword = keyword,
            Role = role,
            Users = await _userService.SearchUsersAsync(keyword, role)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id, string? keyword, string? role)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _userService.ToggleLockAsync(id, adminId);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã cập nhật trạng thái khóa tài khoản."
            : res.Error;

        return RedirectToAction(nameof(Index), new { keyword, role });
    }
}
