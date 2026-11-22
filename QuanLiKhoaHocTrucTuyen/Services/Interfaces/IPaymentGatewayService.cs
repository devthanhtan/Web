using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public enum PaymentCallbackStatus
{
    Success,
    Failed,
    AlreadyConfirmed,
    OrderNotFound,
    InvalidAmount,
    InvalidSignature,
    Pending
}

public record PaymentCallbackResult(
    bool IsSuccess,
    bool SignatureValid,
    bool AlreadyProcessed,
    string OrderCode,
    string Message,
    PaymentCallbackStatus Status = PaymentCallbackStatus.Success);

public interface IPaymentGatewayService
{
    Task<(bool Success, Order? Order, string? RedirectUrl, string? Error)> CreateOrderAndPaymentUrlAsync(
        string userId,
        PaymentProvider provider,
        string baseUrl,
        string clientIp);

    Task<(bool Success, string? RedirectUrl, string? Error)> RetryOrderPaymentAsync(
        int orderId,
        string userId,
        PaymentProvider provider,
        string baseUrl,
        string clientIp);

    /// <param name="updateOrder">false: chỉ xác thực chữ ký và đọc trạng thái đơn (ReturnUrl), không ghi DB.</param>
    Task<PaymentCallbackResult> ProcessVnpayCallbackAsync(IReadOnlyDictionary<string, string> queryParams, string rawPayload, bool updateOrder = true);
    Task<PaymentCallbackResult> ProcessMomoCallbackAsync(IReadOnlyDictionary<string, string> queryParams, string rawPayload);

    /// <summary>true khi cổng của provider đang chạy giả lập (chỉ khi đó mới được phép ký callback mock).</summary>
    bool IsMockGateway(PaymentProvider provider);

    string BuildSignedMockSuccessQuery(Order order, PaymentProvider provider, string transactionNo);
    string BuildSignedMockFailureQuery(Order order, PaymentProvider provider, bool isCancel);
}
