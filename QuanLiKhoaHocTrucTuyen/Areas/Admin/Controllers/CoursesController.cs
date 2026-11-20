using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Areas.Admin.ViewModels;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class CoursesController : Controller
{
    private readonly ICourseManagementService _courseService;

    public CoursesController(ICourseManagementService courseService)
    {
        _courseService = courseService;
    }

    public async Task<IActionResult> Index(CourseStatus? status)
    {
        var vm = new AdminCoursesIndexViewModel
        {
            StatusFilter = status,
            Courses = await _courseService.GetCoursesForAdminReviewAsync(status)
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var course = await _courseService.GetCourseWithCurriculumAsync(id);
        if (course == null || course.Status == CourseStatus.Draft)
            return NotFound();

        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _courseService.ApproveCourseAsync(id, adminId);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã phê duyệt khóa học hiển thị công khai."
            : res.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectCourseInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Bắt buộc nhập lý do từ chối khóa học.";
            return RedirectToAction(nameof(Details), new { id = model.CourseId });
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _courseService.RejectCourseAsync(model.CourseId, adminId, model.RejectionReason);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã từ chối khóa học và thông báo lý do cho Giảng viên."
            : res.Error;
        return RedirectToAction(nameof(Index));
    }
}
