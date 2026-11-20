using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public async Task<IActionResult> Index(bool? hiddenOnly)
    {
        ViewBag.HiddenOnly = hiddenOnly;
        var reviews = await _reviewService.GetAllReviewsForAdminAsync(hiddenOnly);
        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHide(int id, bool? hiddenOnly)
    {
        var res = await _reviewService.ToggleHideReviewAsync(id);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã cập nhật trạng thái hiển thị đánh giá."
            : res.Error;
        return RedirectToAction(nameof(Index), new { hiddenOnly });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool? hiddenOnly)
    {
        var res = await _reviewService.DeleteReviewAsync(id);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã xóa đánh giá vi phạm."
            : res.Error;
        return RedirectToAction(nameof(Index), new { hiddenOnly });
    }
}
