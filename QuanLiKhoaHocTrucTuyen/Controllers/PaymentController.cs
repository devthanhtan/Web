using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;
using QuanLiKhoaHocTrucTuyen.ViewModels;

namespace QuanLiKhoaHocTrucTuyen.Controllers;

public class PaymentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGatewayService _paymentService;
    private readonly VnpayOptions _vnpay;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        ApplicationDbContext db,
        IPaymentGatewayService paymentService,
        IOptions<VnpayOptions> vnpay,
        ILogger<PaymentController> logger)
    {
        _db = db;
        _paymentService = paymentService;
        _vnpay = vnpay.Value;
        _logger = logger;
    }

    [Authorize]
    public async Task<IActionResult> MockGateway(string orderCode, PaymentProvider provider)
    {
        if (!_paymentService.IsMockGateway(provider)) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Course)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId);
        if (order == null) return NotFound();

        return View(new MockPaymentViewModel { Order = order, Provider = provider });
    }

    // Signs callbacks with the gateway secret, so it must never be reachable once a real gateway is configured.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulateMockCallback(string orderCode, PaymentProvider provider, string outcome)
    {
        if (!_paymentService.IsMockGateway(provider)) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId);
        if (order == null) return NotFound();

        string query = outcome switch
        {
            "success" => _paymentService.BuildSignedMockSuccessQuery(order, provider, $"MOCKTXN{DateTime.UtcNow:HHmmss}"),
            "cancel" => _paymentService.BuildSignedMockFailureQuery(order, provider, true),
            _ => _paymentService.BuildSignedMockFailureQuery(order, provider, false)
        };

        if (provider == PaymentProvider.VNPAY)
        {
            // Simulate VNPAY's server-to-server IPN before redirecting the browser to ReturnUrl.
            var parsed = System.Web.HttpUtility.ParseQueryString(query);
            var ipnParams = parsed.AllKeys.Where(k => k != null).ToDictionary(k => k!, k => parsed[k] ?? string.Empty);
            await _paymentService.ProcessVnpayCallbackAsync(ipnParams, query);
        }

        var targetAction = provider == PaymentProvider.VNPAY ? nameof(VnpayReturn) : nameof(MomoReturn);
        return Redirect($"/Payment/{targetAction}?{query}");
    }

    [HttpGet]
    public async Task<IActionResult> VnpayReturn()
    {
        var dict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var result = await _paymentService.ProcessVnpayCallbackAsync(dict, Request.QueryString.Value ?? "", _vnpay.UpdateOrderOnReturn);
        if (result.IsSuccess && result.AlreadyProcessed)
            result = result with { Message = "Thanh toán VNPAY thành công! Khóa học đã được kích hoạt." };
        return View("Result", result);
    }

    // VNPAY server-to-server callback. It retries (10 times, every 5 minutes) unless RspCode is 00 or 02.
    [HttpGet]
    public async Task<IActionResult> VnpayIpn()
    {
        if (!Request.Query.Any())
            return VnpayIpnResponse("99", "Input data required");

        try
        {
            var dict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
            var result = await _paymentService.ProcessVnpayCallbackAsync(dict, Request.QueryString.Value ?? "");
            return result.Status switch
            {
                PaymentCallbackStatus.InvalidSignature => VnpayIpnResponse("97", "Invalid signature"),
                PaymentCallbackStatus.OrderNotFound => VnpayIpnResponse("01", "Order not found"),
                PaymentCallbackStatus.InvalidAmount => VnpayIpnResponse("04", "Invalid amount"),
                PaymentCallbackStatus.AlreadyConfirmed => VnpayIpnResponse("02", "Order already confirmed"),
                _ => VnpayIpnResponse("00", "Confirm Success")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VNPAY] Lỗi xử lý IPN {Query}", Request.QueryString.Value);
            return VnpayIpnResponse("99", "Unknown error");
        }
    }

    // VNPAY expects the exact keys "RspCode" and "Message" (no camelCase).
    private JsonResult VnpayIpnResponse(string rspCode, string message) =>
        Json(new { RspCode = rspCode, Message = message }, VnpayJsonOptions);

    private static readonly JsonSerializerOptions VnpayJsonOptions = new() { PropertyNamingPolicy = null };

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
        var status = isPaid ? PaymentCallbackStatus.Success
            : order.Status == OrderStatus.PendingPayment ? PaymentCallbackStatus.Pending
            : PaymentCallbackStatus.Failed;
        return View(new PaymentCallbackResult(isPaid, true, false, orderCode,
            isPaid ? "Thanh toán thành công! Khóa học đã được mở khóa." : "Đơn hàng chưa hoàn tất thanh toán.", status));
    }
}
