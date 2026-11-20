using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = AppRoles.Student)]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGatewayService _payment;

    public OrdersController(ApplicationDbContext db, IPaymentGatewayService payment)
    {
        _db = db;
        _payment = payment;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var orders = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Course)
            .Include(o => o.Transactions)
            .AsSplitQuery()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int orderId, PaymentProvider provider)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, redirectUrl, error) = await _payment.RetryOrderPaymentAsync(orderId, userId, provider, baseUrl, clientIp);
        if (!success || string.IsNullOrEmpty(redirectUrl))
        {
            TempData["Error"] = error ?? "Không thể thanh toán lại đơn hàng.";
            return RedirectToAction(nameof(Index));
        }

        return Redirect(redirectUrl);
    }
}
