using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Areas.Student.ViewModels;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.Controllers;

[Area("Student")]
[Authorize]
public class ProfileController : Controller
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorageService _fileStorage;

    public ProfileController(UserManager<ApplicationUser> userManager, IFileStorageService fileStorage)
    {
        _userManager = userManager;
        _fileStorage = fileStorage;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var vm = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Headline = user.Headline,
            Bio = user.Bio,
            CurrentAvatarUrl = user.AvatarUrl
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        vm.Email = user.Email ?? string.Empty;
        vm.CurrentAvatarUrl = user.AvatarUrl;

        if (!ModelState.IsValid)
            return View(vm);

        if (vm.AvatarFile != null && vm.AvatarFile.Length > 0)
        {
            var saveRes = await _fileStorage.SaveFileAsync(vm.AvatarFile, "avatars", ImageExtensions, 5 * 1024 * 1024);
            if (!saveRes.Success)
            {
                ModelState.AddModelError(nameof(vm.AvatarFile), saveRes.Error ?? "Lỗi tải ảnh đại diện.");
                return View(vm);
            }

            _fileStorage.DeleteRelativeFile(user.AvatarUrl);
            user.AvatarUrl = saveRes.RelativePath;
        }

        user.FullName = vm.FullName.Trim();
        user.PhoneNumber = vm.PhoneNumber?.Trim();
        user.Headline = vm.Headline?.Trim();
        user.Bio = vm.Bio?.Trim();

        var updateRes = await _userManager.UpdateAsync(user);
        if (!updateRes.Succeeded)
        {
            TempData["Error"] = "Không thể cập nhật hồ sơ.";
            return View(vm);
        }

        TempData["Success"] = "Đã cập nhật hồ sơ cá nhân thành công.";
        return RedirectToAction(nameof(Index));
    }
}
