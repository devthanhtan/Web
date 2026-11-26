namespace QuanLiKhoaHocTrucTuyen.Options;

public class AppFeaturesOptions
{
    public bool UseMockServices { get; set; } = true;
    public long MaxVideoSizeBytes { get; set; } = 104_857_600; // 100 MB
    public long MaxDocumentSizeBytes { get; set; } = 20_971_520; // 20 MB
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "no-reply@eduspark.local";
    public string FromName { get; set; } = "HUIT Learning";
}

public class YouTubeOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string ChannelName { get; set; } = "HUIT Learning";
    public string DefaultDemoVideoId { get; set; } = "PkZNo7MFNFg";
}

public class VnpayOptions
{
    public string TmnCode { get; set; } = "EDUSPARK";
    public string HashSecret { get; set; } = "VNPAY_SANDBOX_SECRET_KEY_2026_DEMO";
    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string ReturnPath { get; set; } = "/Payment/VnpayReturn";
    public string IpnPath { get; set; } = "/Payment/VnpayIpn";
}

public class MomoOptions
{
    public string PartnerCode { get; set; } = "MOMO_SANDBOX_PARTNER";
    public string AccessKey { get; set; } = "MOMO_SANDBOX_ACCESS_KEY";
    public string SecretKey { get; set; } = "MOMO_SANDBOX_SECRET_KEY_2026_DEMO";
    public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
    public string ReturnPath { get; set; } = "/Payment/MomoReturn";
    public string IpnPath { get; set; } = "/Payment/MomoIpn";
}
