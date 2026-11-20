using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = AppRoles.Student)]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int courseId, int rating, string? comment, int? returnLessonId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var res = await _reviewService.SaveReviewAsync(userId, courseId, rating, comment);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã lưu đánh giá khóa học của bạn."
            : res.Error;

        return RedirectToAction("Index", "Learn", new { area = "Student", courseId, lessonId = returnLessonId });
    }
}
