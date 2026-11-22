using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly ApplicationDbContext _db;
    private readonly VnpayOptions _vnpay;
    private readonly MomoOptions _momo;
    private readonly AppFeaturesOptions _features;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly INotificationService _notifications;
    private readonly IAppEmailService _email;
    private readonly ILogger<PaymentGatewayService> _logger;

    public PaymentGatewayService(
        ApplicationDbContext db,
        IOptions<VnpayOptions> vnpay,
        IOptions<MomoOptions> momo,
        IOptions<AppFeaturesOptions> features,
        IHttpClientFactory httpClientFactory,
        INotificationService notifications,
        IAppEmailService email,
        ILogger<PaymentGatewayService> logger)
    {
        _db = db;
        _vnpay = vnpay.Value;
        _momo = momo.Value;
        _features = features.Value;
        _httpClientFactory = httpClientFactory;
        _notifications = notifications;
        _email = email;
        _logger = logger;
    }

    private static readonly Dictionary<string, string> VnpayResponseMessages = new()
    {
        ["07"] = "Trừ tiền thành công nhưng giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
        ["09"] = "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
        ["10"] = "Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.",
        ["11"] = "Đã hết hạn chờ thanh toán.",
        ["12"] = "Thẻ/Tài khoản bị khóa.",
        ["13"] = "Nhập sai mật khẩu xác thực giao dịch (OTP).",
        ["24"] = "Khách hàng hủy giao dịch.",
        ["51"] = "Tài khoản không đủ số dư.",
        ["65"] = "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.",
        ["75"] = "Ngân hàng thanh toán đang bảo trì.",
        ["79"] = "Nhập sai mật khẩu thanh toán quá số lần quy định.",
        ["99"] = "Lỗi khác."
    };

    private bool UseRealVnpay => _vnpay.UseRealGateway || !_features.UseMockServices;

    public bool IsMockGateway(PaymentProvider provider) =>
        provider == PaymentProvider.VNPAY ? !UseRealVnpay : _features.UseMockServices;

    // Mock mode signs and verifies its own callbacks, so it can fall back to a demo secret.
    private string VnpayHashSecret =>
        !string.IsNullOrWhiteSpace(_vnpay.HashSecret) ? _vnpay.HashSecret
        : UseRealVnpay ? string.Empty
        : VnpayOptions.MockHashSecret;

    private string? ValidateGatewayConfig(PaymentProvider provider)
    {
        if (provider == PaymentProvider.VNPAY && UseRealVnpay &&
            (string.IsNullOrWhiteSpace(_vnpay.TmnCode) || string.IsNullOrWhiteSpace(_vnpay.HashSecret)))
        {
            return "Cổng VNPAY chưa được cấu hình (thiếu Vnpay:TmnCode hoặc Vnpay:HashSecret).";
        }
        return null;
    }

    public async Task<(bool Success, Order? Order, string? RedirectUrl, string? Error)> CreateOrderAndPaymentUrlAsync(
        string userId,
        PaymentProvider provider,
        string baseUrl,
        string clientIp)
    {
        if (ValidateGatewayConfig(provider) is string configError)
            return (false, null, null, configError);

        var cartItems = await _db.CartItems
            .Include(c => c.Course)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cartItems.Any())
            return (false, null, null, "Giỏ hàng của bạn đang trống.");

        // Remove any already-owned courses just in case (BR-C2 defense-in-depth)
        var ownedCourseIds = await _db.Enrollments
            .Where(e => e.UserId == userId)
            .Select(e => e.CourseId)
            .ToListAsync();

        var validItems = cartItems
            .Where(c => c.Course.Status == CourseStatus.Approved && !ownedCourseIds.Contains(c.CourseId))
            .ToList();

        if (!validItems.Any())
            return (false, null, null, "Tất cả khóa học trong giỏ đã thuộc sở hữu của bạn hoặc không còn khả dụng.");

        var total = validItems.Sum(c => c.Course.Price);
        var currentRate = await GetCurrentCommissionRateAsync();
        var orderCode = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

        var order = new Order
        {
            OrderCode = orderCode,
            UserId = userId,
            TotalAmount = total,
            Status = OrderStatus.PendingPayment,
            PaymentProvider = provider,
            CreatedAt = DateTime.UtcNow,
            Items = validItems.Select(ci =>
            {
                var platformAmt = Math.Round(ci.Course.Price * currentRate / 100m, 2);
                return new OrderItem
                {
                    CourseId = ci.CourseId,
                    UnitPrice = ci.Course.Price,
                    CommissionRatePercent = currentRate,
                    PlatformAmount = platformAmt,
                    InstructorAmount = ci.Course.Price - platformAmt
                };
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Tạo đơn hàng {OrderCode} cho user {UserId}, tổng tiền {Total}, cổng {Provider}",
            order.OrderCode, userId, order.TotalAmount, provider);

        // If total is 0đ (free courses), mark Paid immediately
        if (order.TotalAmount <= 0m)
        {
            await CompleteOrderPaidAsync(order, "FREE_0", "{\"free\":true}");
            return (true, order, $"{baseUrl}/Payment/Result?orderCode={order.OrderCode}", null);
        }

        var url = await BuildGatewayUrlAsync(order, provider, baseUrl, clientIp);
        return (true, order, url, null);
    }

    public async Task<(bool Success, string? RedirectUrl, string? Error)> RetryOrderPaymentAsync(
        int orderId,
        string userId,
        PaymentProvider provider,
        string baseUrl,
        string clientIp)
    {
        if (ValidateGatewayConfig(provider) is string configError)
            return (false, null, configError);

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
            return (false, null, "Không tìm thấy đơn hàng.");
        if (order.Status == OrderStatus.Paid)
            return (false, null, "Đơn hàng này đã thanh toán thành công trước đó.");

        order.Status = OrderStatus.PendingPayment;
        order.PaymentProvider = provider;
        await _db.SaveChangesAsync();

        var url = await BuildGatewayUrlAsync(order, provider, baseUrl, clientIp);
        return (true, url, null);
    }

    private async Task<string> BuildGatewayUrlAsync(Order order, PaymentProvider provider, string baseUrl, string clientIp)
    {
        var mockUrl = $"{baseUrl}/Payment/MockGateway?orderCode={Uri.EscapeDataString(order.OrderCode)}&provider={provider}";

        if (provider == PaymentProvider.VNPAY)
        {
            var now = DateTime.UtcNow.AddHours(7);
            order.PaymentTxnRef = BuildVnpayTxnRef(order.OrderCode, now);
            await _db.SaveChangesAsync();

            return UseRealVnpay ? BuildVnpayUrl(order, baseUrl, clientIp, now) : mockUrl;
        }

        if (_features.UseMockServices)
        {
            return mockUrl;
        }

        return await CreateMomoSandboxUrlAsync(order, baseUrl);
    }

    // vnp_TxnRef must be alphanumeric and unique within a day, so each payment attempt gets a time suffix.
    public static string BuildVnpayTxnRef(string orderCode, DateTime vietnamNow)
    {
        var baseCode = new string(orderCode.Where(char.IsAsciiLetterOrDigit).ToArray());
        return $"{baseCode}{vietnamNow:HHmmss}";
    }

    public static string NormalizeClientIp(string? clientIp)
    {
        if (string.IsNullOrWhiteSpace(clientIp) || !IPAddress.TryParse(clientIp, out var address))
            return "127.0.0.1";
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        return IPAddress.IsLoopback(address) ? "127.0.0.1" : address.ToString();
    }

    private string BuildVnpayUrl(Order order, string baseUrl, string clientIp, DateTime vietnamNow)
    {
        var returnUrl = $"{baseUrl}{_vnpay.ReturnPath}";
        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _vnpay.TmnCode,
            ["vnp_Amount"] = ((long)(order.TotalAmount * 100)).ToString(),
            ["vnp_CreateDate"] = vietnamNow.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = vietnamNow.AddMinutes(_vnpay.ExpireMinutes).ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = NormalizeClientIp(clientIp),
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = $"Thanh toan khoa hoc {order.OrderCode}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = order.PaymentTxnRef ?? order.OrderCode
        };

        var query = BuildEncodedQuery(vnpParams);
        var hash = ComputeHmacSha512(VnpayHashSecret, query);
        _logger.LogInformation("[VNPAY] Tạo URL thanh toán cho đơn {OrderCode}, TxnRef {TxnRef}", order.OrderCode, vnpParams["vnp_TxnRef"]);
        return $"{_vnpay.BaseUrl}?{query}&vnp_SecureHash={hash}";
    }

    private async Task<string> CreateMomoSandboxUrlAsync(Order order, string baseUrl)
    {
        var redirectUrl = $"{baseUrl}{_momo.ReturnPath}";
        var ipnUrl = $"{baseUrl}{_momo.IpnPath}";
        var requestId = Guid.NewGuid().ToString("N");
        var amount = ((long)order.TotalAmount).ToString();
        var orderInfo = $"Thanh toan khoa hoc {order.OrderCode}";
        var extraData = "";
        var requestType = "captureWallet";

        var rawHash = $"accessKey={_momo.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={order.OrderCode}&orderInfo={orderInfo}&partnerCode={_momo.PartnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
        var signature = ComputeHmacSha256(_momo.SecretKey, rawHash);

        var body = new
        {
            partnerCode = _momo.PartnerCode,
            accessKey = _momo.AccessKey,
            requestId,
            amount,
            orderId = order.OrderCode,
            orderInfo,
            redirectUrl,
            ipnUrl,
            extraData,
            requestType,
            signature,
            lang = "vi"
        };

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(_momo.Endpoint, body);
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("payUrl", out var payUrlProp) && payUrlProp.GetString() is string payUrl)
        {
            return payUrl;
        }

        _logger.LogWarning("MoMo sandbox không trả về payUrl: {Response}", json);
        return $"{baseUrl}/Payment/MockGateway?orderCode={Uri.EscapeDataString(order.OrderCode)}&provider=MoMo";
    }

    public string BuildSignedMockSuccessQuery(Order order, PaymentProvider provider, string transactionNo)
    {
        if (provider == PaymentProvider.VNPAY)
        {
            var dict = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Amount"] = ((long)(order.TotalAmount * 100)).ToString(),
                ["vnp_BankCode"] = "NCB",
                ["vnp_OrderInfo"] = $"Thanh toan khoa hoc {order.OrderCode}",
                ["vnp_PayDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
                ["vnp_ResponseCode"] = "00",
                ["vnp_TmnCode"] = _vnpay.TmnCode,
                ["vnp_TransactionNo"] = transactionNo,
                ["vnp_TransactionStatus"] = "00",
                ["vnp_TxnRef"] = order.PaymentTxnRef ?? order.OrderCode
            };
            var query = BuildEncodedQuery(dict);
            var sig = ComputeHmacSha512(VnpayHashSecret, query);
            return $"{query}&vnp_SecureHash={sig}";
        }
        else
        {
            var amount = ((long)order.TotalAmount).ToString();
            var requestId = $"REQ_{order.OrderCode}";
            var orderInfo = $"Thanh toan khoa hoc {order.OrderCode}";
            var orderType = "momo_wallet";
            var payType = "qr";
            var responseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var resultCode = "0";
            var message = "Successful.";
            var extraData = "";

            var rawHash = $"accessKey={_momo.AccessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={order.OrderCode}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={_momo.PartnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transactionNo}";
            var sig = ComputeHmacSha256(_momo.SecretKey, rawHash);

            var dict = new Dictionary<string, string>
            {
                ["partnerCode"] = _momo.PartnerCode,
                ["orderId"] = order.OrderCode,
                ["requestId"] = requestId,
                ["amount"] = amount,
                ["orderInfo"] = orderInfo,
                ["orderType"] = orderType,
                ["transId"] = transactionNo,
                ["resultCode"] = resultCode,
                ["message"] = message,
                ["payType"] = payType,
                ["responseTime"] = responseTime,
                ["extraData"] = extraData,
                ["signature"] = sig
            };
            return BuildEncodedQuery(dict);
        }
    }

    public string BuildSignedMockFailureQuery(Order order, PaymentProvider provider, bool isCancel)
    {
        if (provider == PaymentProvider.VNPAY)
        {
            var code = isCancel ? "24" : "99";
            var dict = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Amount"] = ((long)(order.TotalAmount * 100)).ToString(),
                ["vnp_OrderInfo"] = $"Thanh toan khoa hoc {order.OrderCode}",
                ["vnp_ResponseCode"] = code,
                ["vnp_TmnCode"] = _vnpay.TmnCode,
                ["vnp_TransactionNo"] = "0",
                ["vnp_TransactionStatus"] = "02",
                ["vnp_TxnRef"] = order.PaymentTxnRef ?? order.OrderCode
            };
            var query = BuildEncodedQuery(dict);
            var sig = ComputeHmacSha512(VnpayHashSecret, query);
            return $"{query}&vnp_SecureHash={sig}";
        }
        else
        {
            var amount = ((long)order.TotalAmount).ToString();
            var requestId = $"REQ_{order.OrderCode}";
            var orderInfo = $"Thanh toan khoa hoc {order.OrderCode}";
            var orderType = "momo_wallet";
            var payType = "qr";
            var responseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var resultCode = isCancel ? "1006" : "99";
            var message = isCancel ? "User cancelled" : "Transaction failed";
            var extraData = "";
            var transId = "0";

            var rawHash = $"accessKey={_momo.AccessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={order.OrderCode}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={_momo.PartnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
            var sig = ComputeHmacSha256(_momo.SecretKey, rawHash);

            var dict = new Dictionary<string, string>
            {
                ["partnerCode"] = _momo.PartnerCode,
                ["orderId"] = order.OrderCode,
                ["requestId"] = requestId,
                ["amount"] = amount,
                ["orderInfo"] = orderInfo,
                ["orderType"] = orderType,
                ["transId"] = transId,
                ["resultCode"] = resultCode,
                ["message"] = message,
                ["payType"] = payType,
                ["responseTime"] = responseTime,
                ["extraData"] = extraData,
                ["signature"] = sig
            };
            return BuildEncodedQuery(dict);
        }
    }

    public async Task<PaymentCallbackResult> ProcessVnpayCallbackAsync(IReadOnlyDictionary<string, string> queryParams, string rawPayload, bool updateOrder = true)
    {
        queryParams.TryGetValue("vnp_TxnRef", out var txnRef);
        queryParams.TryGetValue("vnp_SecureHash", out var providedHash);
        queryParams.TryGetValue("vnp_ResponseCode", out var responseCode);
        queryParams.TryGetValue("vnp_TransactionStatus", out var transactionStatus);
        queryParams.TryGetValue("vnp_Amount", out var vnpAmountStr);
        queryParams.TryGetValue("vnp_TransactionNo", out var txnNo);

        txnRef ??= string.Empty;

        var signatureValid = VerifyVnpaySignature(queryParams, VnpayHashSecret, providedHash);
        var order = string.IsNullOrEmpty(txnRef)
            ? null
            : await _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Course)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.PaymentTxnRef == txnRef || o.OrderCode == txnRef);

        // Theo tài liệu VNPAY: kiểm tra checksum trước mọi xử lý khác (RspCode 97)
        if (!signatureValid)
        {
            _logger.LogWarning("[VNPAY] Chữ ký HMAC không hợp lệ cho TxnRef {TxnRef}", txnRef);
            if (order != null && updateOrder)
                await RecordTransactionAsync(order.Id, PaymentProvider.VNPAY, txnNo, order.TotalAmount, TransactionStatus.Failed, false, rawPayload);
            return new PaymentCallbackResult(false, false, false, order?.OrderCode ?? txnRef, "Chữ ký giao dịch không hợp lệ.", PaymentCallbackStatus.InvalidSignature);
        }

        if (order == null)
        {
            _logger.LogWarning("[VNPAY] Không tìm thấy đơn hàng với TxnRef {TxnRef}", txnRef);
            return new PaymentCallbackResult(false, true, false, txnRef, "Không tìm thấy đơn hàng.", PaymentCallbackStatus.OrderNotFound);
        }

        var orderCode = order.OrderCode;

        // VNPAY trả về số tiền đã nhân 100 (RspCode 04 nếu lệch)
        if (!long.TryParse(vnpAmountStr, out var vnpAmount) || vnpAmount != (long)(order.TotalAmount * 100))
        {
            _logger.LogWarning("[VNPAY] Sai lệch số tiền đơn {OrderCode}: nhận {Received}, mong đợi {Expected}",
                orderCode, vnpAmountStr, order.TotalAmount * 100);
            if (updateOrder)
                await RecordTransactionAsync(order.Id, PaymentProvider.VNPAY, txnNo, order.TotalAmount, TransactionStatus.Failed, true, rawPayload);
            return new PaymentCallbackResult(false, true, false, orderCode, "Số tiền giao dịch không khớp.", PaymentCallbackStatus.InvalidAmount);
        }

        // BR-C4: Idempotent check (RspCode 02)
        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation("[VNPAY] Đơn hàng {OrderCode} đã xử lý Paid trước đó (idempotent).", orderCode);
            return new PaymentCallbackResult(true, true, true, orderCode, "Giao dịch đã được ghi nhận trước đó.", PaymentCallbackStatus.AlreadyConfirmed);
        }

        var isPaid = responseCode == "00" && transactionStatus == "00";
        var failureMessage = $"Giao dịch VNPAY không thành công: {DescribeVnpayResponseCode(responseCode)} Khóa học vẫn còn trong giỏ hàng.";

        if (!updateOrder)
        {
            return isPaid
                ? new PaymentCallbackResult(false, true, false, orderCode,
                    "VNPAY đã xác nhận thanh toán. Hệ thống đang chờ IPN để kích hoạt khóa học, vui lòng tải lại trang sau ít phút.",
                    PaymentCallbackStatus.Pending)
                : new PaymentCallbackResult(false, true, false, orderCode, failureMessage, PaymentCallbackStatus.Failed);
        }

        if (isPaid)
        {
            await CompleteOrderPaidAsync(order, txnNo ?? "VNPAY_OK", rawPayload);
            return new PaymentCallbackResult(true, true, false, orderCode, "Thanh toán VNPAY thành công! Khóa học đã được kích hoạt.", PaymentCallbackStatus.Success);
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return new PaymentCallbackResult(false, true, true, orderCode, failureMessage, PaymentCallbackStatus.AlreadyConfirmed);
        }

        _logger.LogInformation("[VNPAY] Đơn {OrderCode} thất bại: ResponseCode {ResponseCode}, TransactionStatus {TransactionStatus}",
            orderCode, responseCode, transactionStatus);
        await MarkOrderCancelledAsync(order, PaymentProvider.VNPAY, txnNo, rawPayload);
        return new PaymentCallbackResult(false, true, false, orderCode, failureMessage, PaymentCallbackStatus.Failed);
    }

    public static string DescribeVnpayResponseCode(string? responseCode) =>
        responseCode != null && VnpayResponseMessages.TryGetValue(responseCode, out var message)
            ? message
            : $"Mã lỗi {responseCode ?? "không xác định"}.";

    public async Task<PaymentCallbackResult> ProcessMomoCallbackAsync(IReadOnlyDictionary<string, string> queryParams, string rawPayload)
    {
        queryParams.TryGetValue("orderId", out var orderCode);
        queryParams.TryGetValue("signature", out var providedHash);
        queryParams.TryGetValue("resultCode", out var resultCode);
        queryParams.TryGetValue("amount", out var amountStr);
        queryParams.TryGetValue("transId", out var txnNo);

        orderCode ??= string.Empty;

        var signatureValid = VerifyMomoSignature(queryParams, _momo.AccessKey, _momo.SecretKey, providedHash);
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Course)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        if (order == null)
        {
            _logger.LogWarning("[MoMo] Không tìm thấy đơn hàng {OrderCode}", orderCode);
            return new PaymentCallbackResult(false, signatureValid, false, orderCode, "Không tìm thấy đơn hàng.");
        }

        if (!signatureValid)
        {
            _logger.LogWarning("[MoMo] Chữ ký HMAC không hợp lệ cho đơn {OrderCode}", orderCode);
            await RecordTransactionAsync(order.Id, PaymentProvider.MoMo, txnNo, order.TotalAmount, TransactionStatus.Failed, false, rawPayload);
            return new PaymentCallbackResult(false, false, false, orderCode, "Chữ ký MoMo không hợp lệ.");
        }

        if (!long.TryParse(amountStr, out var momoAmount) || momoAmount != (long)order.TotalAmount)
        {
            _logger.LogWarning("[MoMo] Sai lệch số tiền đơn {OrderCode}", orderCode);
            await RecordTransactionAsync(order.Id, PaymentProvider.MoMo, txnNo, order.TotalAmount, TransactionStatus.Failed, true, rawPayload);
            return new PaymentCallbackResult(false, true, false, orderCode, "Số tiền giao dịch không khớp.");
        }

        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation("[MoMo] Đơn hàng {OrderCode} đã xử lý Paid trước đó (idempotent).", orderCode);
            return new PaymentCallbackResult(true, true, true, orderCode, "Giao dịch đã được ghi nhận trước đó.");
        }

        if (resultCode == "0")
        {
            await CompleteOrderPaidAsync(order, txnNo ?? "MOMO_OK", rawPayload);
            return new PaymentCallbackResult(true, true, false, orderCode, "Thanh toán MoMo thành công! Khóa học đã được kích hoạt.");
        }

        await MarkOrderCancelledAsync(order, PaymentProvider.MoMo, txnNo, rawPayload);
        return new PaymentCallbackResult(false, true, false, orderCode, "Giao dịch MoMo bị hủy hoặc thất bại. Khóa học vẫn còn trong giỏ hàng.");
    }

    public static bool VerifyVnpaySignature(IReadOnlyDictionary<string, string> queryParams, string hashSecret, string? providedHash)
    {
        if (string.IsNullOrWhiteSpace(providedHash) || string.IsNullOrWhiteSpace(hashSecret))
            return false;

        var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in queryParams)
        {
            if (kv.Key.StartsWith("vnp_", StringComparison.Ordinal) &&
                kv.Key != "vnp_SecureHash" &&
                kv.Key != "vnp_SecureHashType" &&
                !string.IsNullOrEmpty(kv.Value))
            {
                sorted[kv.Key] = kv.Value;
            }
        }

        var rawData = BuildEncodedQuery(sorted);
        var expected = ComputeHmacSha512(hashSecret, rawData);
        return ConstantTimeEqualsHex(expected, providedHash);
    }

    public static bool VerifyMomoSignature(IReadOnlyDictionary<string, string> p, string accessKey, string secretKey, string? providedHash)
    {
        if (string.IsNullOrWhiteSpace(providedHash) || string.IsNullOrWhiteSpace(secretKey))
            return false;

        string Get(string key) => p.TryGetValue(key, out var val) ? val : string.Empty;

        var rawHash =
            $"accessKey={accessKey}" +
            $"&amount={Get("amount")}" +
            $"&extraData={Get("extraData")}" +
            $"&message={Get("message")}" +
            $"&orderId={Get("orderId")}" +
            $"&orderInfo={Get("orderInfo")}" +
            $"&orderType={Get("orderType")}" +
            $"&partnerCode={Get("partnerCode")}" +
            $"&payType={Get("payType")}" +
            $"&requestId={Get("requestId")}" +
            $"&responseTime={Get("responseTime")}" +
            $"&resultCode={Get("resultCode")}" +
            $"&transId={Get("transId")}";

        var expected = ComputeHmacSha256(secretKey, rawHash);
        return ConstantTimeEqualsHex(expected, providedHash);
    }

    private async Task CompleteOrderPaidAsync(Order order, string transactionNo, string rawPayload)
    {
        var currentRate = await GetCurrentCommissionRateAsync();
        order.Status = OrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            item.CommissionRatePercent = currentRate;
            item.PlatformAmount = Math.Round(item.UnitPrice * currentRate / 100m, 2);
            item.InstructorAmount = item.UnitPrice - item.PlatformAmount;

            var enrolled = await _db.Enrollments.AnyAsync(e => e.UserId == order.UserId && e.CourseId == item.CourseId);
            if (!enrolled)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    UserId = order.UserId,
                    CourseId = item.CourseId,
                    OrderId = order.Id,
                    EnrolledAt = DateTime.UtcNow,
                    ProgressPercent = 0m
                });
            }
        }

        // Remove paid courses from cart
        var purchasedIds = order.Items.Select(i => i.CourseId).ToList();
        var cartItems = await _db.CartItems
            .Where(c => c.UserId == order.UserId && purchasedIds.Contains(c.CourseId))
            .ToListAsync();
        if (cartItems.Any())
        {
            _db.CartItems.RemoveRange(cartItems);
        }

        _db.Transactions.Add(new Transaction
        {
            OrderId = order.Id,
            Provider = order.PaymentProvider,
            TransactionNo = transactionNo,
            Amount = order.TotalAmount,
            Status = TransactionStatus.Success,
            SignatureValid = true,
            RawPayload = rawPayload,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Đơn hàng {OrderCode} thanh toán thành công (Mã GD: {TxnNo}). Đã kích hoạt {Count} khóa học.",
            order.OrderCode, transactionNo, order.Items.Count);

        await _notifications.CreateAsync(
            order.UserId,
            "Thanh toán thành công",
            $"Đơn hàng {order.OrderCode} ({order.TotalAmount:N0} đ) đã hoàn tất. Bạn có thể vào học ngay.",
            "/Student/Dashboard");

        if (order.User?.Email != null)
        {
            await _email.SendEmailAsync(
                order.User.Email,
                $"[EduSpark] Xác nhận thanh toán đơn hàng {order.OrderCode}",
                $"<p>Cảm ơn bạn đã mua khóa học tại EduSpark. Mã đơn: <strong>{order.OrderCode}</strong>, Tổng tiền: <strong>{order.TotalAmount:N0} đ</strong>. Quyền truy cập khóa học đã được mở ngay lập tức.</p>");
        }
    }

    private async Task MarkOrderCancelledAsync(Order order, PaymentProvider provider, string? txnNo, string rawPayload)
    {
        order.Status = OrderStatus.Cancelled;
        _db.Transactions.Add(new Transaction
        {
            OrderId = order.Id,
            Provider = provider,
            TransactionNo = txnNo,
            Amount = order.TotalAmount,
            Status = TransactionStatus.Failed,
            SignatureValid = true,
            RawPayload = rawPayload,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        _logger.LogInformation("Đơn hàng {OrderCode} chuyển sang Cancelled; giữ nguyên giỏ hàng.", order.OrderCode);
    }

    private async Task RecordTransactionAsync(int orderId, PaymentProvider provider, string? txnNo, decimal amount, TransactionStatus status, bool signatureValid, string rawPayload)
    {
        _db.Transactions.Add(new Transaction
        {
            OrderId = orderId,
            Provider = provider,
            TransactionNo = txnNo,
            Amount = amount,
            Status = status,
            SignatureValid = signatureValid,
            RawPayload = rawPayload,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task<decimal> GetCurrentCommissionRateAsync()
    {
        var cfg = await _db.CommissionConfigs.OrderByDescending(c => c.UpdatedAt).FirstOrDefaultAsync();
        return cfg?.PlatformRatePercent ?? 20.00m;
    }

    private static string BuildEncodedQuery(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join("&", parameters.Select(kv =>
            $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
    }

    public static string ComputeHmacSha512(string key, string inputData)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static string ComputeHmacSha256(string key, string inputData)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static bool ConstantTimeEqualsHex(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a.Trim().ToLowerInvariant());
        var bytesB = Encoding.UTF8.GetBytes(b.Trim().ToLowerInvariant());
        return bytesA.Length == bytesB.Length && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
