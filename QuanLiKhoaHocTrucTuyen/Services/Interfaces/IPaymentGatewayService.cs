using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public record PaymentCallbackResult(
    bool IsSuccess,
    bool SignatureValid,
    bool AlreadyProcessed,
    string OrderCode,
    string Message);

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

    Task<PaymentCallbackResult> ProcessVnpayCallbackAsync(IReadOnlyDictionary<string, string> queryParams, string rawPayload);
    Task<PaymentCallbackResult> ProcessMomoCallbackAsync(IReadOnlyDictionary<string, string> queryParams, string rawPayload);

    string BuildSignedMockSuccessQuery(Order order, PaymentProvider provider, string transactionNo);
    string BuildSignedMockFailureQuery(Order order, PaymentProvider provider, bool isCancel);
}
