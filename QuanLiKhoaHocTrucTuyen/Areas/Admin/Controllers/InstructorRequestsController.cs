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
public class InstructorRequestsController : Controller
{
    private readonly IInstructorRequestService _requestService;

    public InstructorRequestsController(IInstructorRequestService requestService)
    {
        _requestService = requestService;
    }

    public async Task<IActionResult> Index(InstructorRequestStatus? status)
    {
        var vm = new InstructorRequestsIndexViewModel
        {
            StatusFilter = status,
            Requests = await _requestService.GetAllAsync(status)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _requestService.ApproveAsync(id, adminId);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã phê duyệt hồ sơ và cấp quyền Giảng viên."
            : res.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectRequestInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Bắt buộc nhập lý do từ chối hồ sơ.";
            return RedirectToAction(nameof(Index));
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _requestService.RejectAsync(model.RequestId, adminId, model.RejectionReason);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã từ chối hồ sơ và gửi thông báo lý do cho người dùng."
            : res.Error;
        return RedirectToAction(nameof(Index));
    }
}
