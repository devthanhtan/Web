using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Areas.Student.ViewModels;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = AppRoles.Student)]
public class InstructorApplicationController : Controller
{
    private readonly IInstructorRequestService _requestService;

    public InstructorApplicationController(IInstructorRequestService requestService)
    {
        _requestService = requestService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var latest = await _requestService.GetLatestForUserAsync(userId);
        var vm = new InstructorApplicationViewModel
        {
            LatestRequest = latest,
            IsAlreadyInstructor = User.IsInRole(AppRoles.Instructor),
            Expertise = latest?.Expertise ?? string.Empty,
            ExperienceYears = latest?.ExperienceYears ?? 1,
            Bio = latest?.Bio ?? string.Empty
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(InstructorApplicationViewModel vm)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        vm.LatestRequest = await _requestService.GetLatestForUserAsync(userId);
        vm.IsAlreadyInstructor = User.IsInRole(AppRoles.Instructor);

        if (vm.CvFile == null || vm.CvFile.Length == 0)
        {
            ModelState.AddModelError(nameof(vm.CvFile), "Vui lòng đính kèm tệp CV / hồ sơ năng lực.");
        }

        if (!ModelState.IsValid)
            return View(vm);

        var result = await _requestService.SubmitOrResubmitAsync(
            userId,
            vm.Expertise,
            vm.ExperienceYears,
            vm.Bio,
            vm.CvFile!,
            vm.CertificateFile);

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return View(vm);
        }

        TempData["Success"] = "Đã gửi hồ sơ nâng cấp Giảng viên! Vui lòng chờ Admin thẩm định.";
        return RedirectToAction(nameof(Index));
    }
}
