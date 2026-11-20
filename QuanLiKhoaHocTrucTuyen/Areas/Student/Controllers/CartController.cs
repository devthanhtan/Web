using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = AppRoles.Student)]
public class CartController : Controller
{
    private readonly ICartService _cart;
    private readonly IPaymentGatewayService _payment;

    public CartController(ICartService cart, IPaymentGatewayService payment)
    {
        _cart = cart;
        _payment = payment;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var items = await _cart.GetCartItemsAsync(CurrentUserId);
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int courseId, string? returnUrl)
    {
        var res = await _cart.AddToCartAsync(CurrentUserId, courseId);
        TempData[res.Success ? "Success" : "Error"] = res.Success
            ? "Đã thêm khóa học vào giỏ hàng."
            : res.Error;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int courseId)
    {
        await _cart.RemoveFromCartAsync(CurrentUserId, courseId);
        TempData["Success"] = "Đã xóa khóa học khỏi giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(PaymentProvider provider)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var (success, _, redirectUrl, error) = await _payment.CreateOrderAndPaymentUrlAsync(CurrentUserId, provider, baseUrl, clientIp);

        if (!success || string.IsNullOrEmpty(redirectUrl))
        {
            TempData["Error"] = error ?? "Không thể khởi tạo thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        return Redirect(redirectUrl);
    }
}
