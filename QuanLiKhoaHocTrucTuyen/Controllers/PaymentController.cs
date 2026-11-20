using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;
using QuanLiKhoaHocTrucTuyen.ViewModels;

namespace QuanLiKhoaHocTrucTuyen.Controllers;

public class PaymentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGatewayService _paymentService;

    public PaymentController(ApplicationDbContext db, IPaymentGatewayService paymentService)
    {
        _db = db;
        _paymentService = paymentService;
    }

    [Authorize]
    public async Task<IActionResult> MockGateway(string orderCode, PaymentProvider provider)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Course)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        if (order == null) return NotFound();

        return View(new MockPaymentViewModel { Order = order, Provider = provider });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulateMockCallback(string orderCode, PaymentProvider provider, string outcome)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        if (order == null) return NotFound();

        string query = outcome switch
        {
            "success" => _paymentService.BuildSignedMockSuccessQuery(order, provider, $"MOCKTXN{DateTime.UtcNow:HHmmss}"),
            "cancel" => _paymentService.BuildSignedMockFailureQuery(order, provider, true),
            _ => _paymentService.BuildSignedMockFailureQuery(order, provider, false)
        };

        var targetAction = provider == PaymentProvider.VNPAY ? nameof(VnpayReturn) : nameof(MomoReturn);
        return Redirect($"/Payment/{targetAction}?{query}");
    }

    [HttpGet]
    public async Task<IActionResult> VnpayReturn()
    {
        var dict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var result = await _paymentService.ProcessVnpayCallbackAsync(dict, Request.QueryString.Value ?? "");
        return View("Result", result);
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> VnpayIpn()
    {
        var dict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var result = await _paymentService.ProcessVnpayCallbackAsync(dict, Request.QueryString.Value ?? "");
        if (!result.SignatureValid)
            return Json(new { RspCode = "97", Message = "Invalid signature" });
        return Json(new { RspCode = "00", Message = result.AlreadyProcessed ? "Order already confirmed" : "Confirm Success" });
    }

    [HttpGet]
    public async Task<IActionResult> MomoReturn()
    {
        var dict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var result = await _paymentService.ProcessMomoCallbackAsync(dict, Request.QueryString.Value ?? "");
        return View("Result", result);
    }

    [HttpPost]
    public async Task<IActionResult> MomoIpn()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var dict = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(body))
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ToString();
            }
        }
        else
        {
            foreach (var q in Request.Query) dict[q.Key] = q.Value.ToString();
        }

        var result = await _paymentService.ProcessMomoCallbackAsync(dict, body);
        return result.SignatureValid ? NoContent() : BadRequest(new { message = "Invalid signature" });
    }

    public async Task<IActionResult> Result(string orderCode)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        if (order == null) return NotFound();

        var isPaid = order.Status == OrderStatus.Paid;
        return View(new PaymentCallbackResult(isPaid, true, false, orderCode,
            isPaid ? "Thanh toán thành công! Khóa học đã được mở khóa." : "Đơn hàng chưa hoàn tất thanh toán."));
    }
}
