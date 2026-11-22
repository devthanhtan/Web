using System.Net;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Tests;

public class VnpayIntegrationTests
{
    private const string Secret = "TEST_VNP_SECRET";
    private const string TxnRef = "ORDTEST01103000";

    private static async Task<(ApplicationDbContext Db, Order Order)> SeedOrderAsync(OrderStatus status = OrderStatus.PendingPayment)
    {
        var db = TestHelpers.CreateInMemoryDb();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1@test.vn", Email = "u1@test.vn", FullName = "Học viên A" });
        db.Categories.Add(new Category { Id = 1, Name = "Web", Slug = "web" });
        db.Courses.Add(new Course { Id = 10, Title = "Course A", Slug = "course-a", ShortDescription = "s", Description = "d", Price = 500000m, CategoryId = 1, InstructorId = "u1", Status = CourseStatus.Approved });
        var order = new Order
        {
            Id = 1,
            OrderCode = "ORD-TEST-01",
            PaymentTxnRef = TxnRef,
            UserId = "u1",
            TotalAmount = 500000m,
            Status = status,
            PaymentProvider = PaymentProvider.VNPAY,
            Items = [new OrderItem { CourseId = 10, UnitPrice = 500000m, CommissionRatePercent = 20m, PlatformAmount = 100000m, InstructorAmount = 400000m }]
        };
        db.Orders.Add(order);
        db.CartItems.Add(new CartItem { UserId = "u1", CourseId = 10 });
        await db.SaveChangesAsync();
        return (db, order);
    }

    private static Dictionary<string, string> SignedCallback(string txnRef = TxnRef, string amount = "50000000", string responseCode = "00", string transactionStatus = "00")
    {
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = amount,
            ["vnp_BankCode"] = "NCB",
            ["vnp_OrderInfo"] = "Thanh toan khoa hoc ORD-TEST-01",
            ["vnp_ResponseCode"] = responseCode,
            ["vnp_TmnCode"] = "TESTTMN",
            ["vnp_TransactionNo"] = "14226112",
            ["vnp_TransactionStatus"] = transactionStatus,
            ["vnp_TxnRef"] = txnRef
        };
        var raw = string.Join("&", data.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
        return new Dictionary<string, string>(data) { ["vnp_SecureHash"] = PaymentGatewayService.ComputeHmacSha512(Secret, raw) };
    }

    [Fact]
    public async Task Callback_UnknownTxnRef_ReturnsOrderNotFound()
    {
        var (db, _) = await SeedOrderAsync();
        using var _db = db;
        var svc = TestHelpers.CreatePaymentService(db, Secret);

        var res = await svc.ProcessVnpayCallbackAsync(SignedCallback(txnRef: "UNKNOWN123"), "raw");

        Assert.Equal(PaymentCallbackStatus.OrderNotFound, res.Status);
    }

    [Fact]
    public async Task Callback_AmountMismatch_ReturnsInvalidAmount_AndDoesNotEnroll()
    {
        var (db, _) = await SeedOrderAsync();
        using var _db = db;
        var svc = TestHelpers.CreatePaymentService(db, Secret);

        var res = await svc.ProcessVnpayCallbackAsync(SignedCallback(amount: "100"), "raw");

        Assert.Equal(PaymentCallbackStatus.InvalidAmount, res.Status);
        Assert.Equal(0, await db.Enrollments.CountAsync());
    }

    [Fact]
    public async Task Callback_Success_LooksUpByPaymentTxnRef_ThenReplayIsAlreadyConfirmed()
    {
        var (db, _) = await SeedOrderAsync();
        using var _db = db;
        var svc = TestHelpers.CreatePaymentService(db, Secret);

        var first = await svc.ProcessVnpayCallbackAsync(SignedCallback(), "raw");
        var replay = await svc.ProcessVnpayCallbackAsync(SignedCallback(), "raw");

        Assert.Equal(PaymentCallbackStatus.Success, first.Status);
        Assert.Equal("ORD-TEST-01", first.OrderCode);
        Assert.Equal(PaymentCallbackStatus.AlreadyConfirmed, replay.Status);
        Assert.Equal(1, await db.Enrollments.CountAsync());
    }

    [Fact]
    public async Task Callback_ResponseCode00ButTransactionStatusFailed_IsNotPaid()
    {
        var (db, _) = await SeedOrderAsync();
        using var _db = db;
        var svc = TestHelpers.CreatePaymentService(db, Secret);

        var res = await svc.ProcessVnpayCallbackAsync(SignedCallback(responseCode: "00", transactionStatus: "02"), "raw");

        Assert.Equal(PaymentCallbackStatus.Failed, res.Status);
        Assert.Equal(OrderStatus.Cancelled, (await db.Orders.FirstAsync()).Status);
        Assert.Equal(0, await db.Enrollments.CountAsync());
    }

    [Fact]
    public async Task Callback_CancelledByUser_ShowsReason_AndReplayIsAlreadyConfirmed()
    {
        var (db, _) = await SeedOrderAsync();
        using var _db = db;
        var svc = TestHelpers.CreatePaymentService(db, Secret);

        var first = await svc.ProcessVnpayCallbackAsync(SignedCallback(responseCode: "24", transactionStatus: "02"), "raw");
        var replay = await svc.ProcessVnpayCallbackAsync(SignedCallback(responseCode: "24", transactionStatus: "02"), "raw");

        Assert.Equal(PaymentCallbackStatus.Failed, first.Status);
        Assert.Contains("hủy giao dịch", first.Message);
        Assert.Equal(PaymentCallbackStatus.AlreadyConfirmed, replay.Status);
        Assert.Equal(1, await db.Transactions.CountAsync());
        Assert.Equal(1, await db.CartItems.CountAsync());
    }

    [Fact]
    public async Task Callback_WithoutUpdateOrder_DoesNotTouchDatabase()
    {
        var (db, _) = await SeedOrderAsync();
        using var _db = db;
        var svc = TestHelpers.CreatePaymentService(db, Secret);

        var res = await svc.ProcessVnpayCallbackAsync(SignedCallback(), "raw", updateOrder: false);

        Assert.Equal(PaymentCallbackStatus.Pending, res.Status);
        Assert.Equal(OrderStatus.PendingPayment, (await db.Orders.FirstAsync()).Status);
        Assert.Equal(0, await db.Transactions.CountAsync());
    }

    [Fact]
    public async Task Checkout_RealGateway_BuildsSignedUrlWithExpireDateAndStoresTxnRef()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1@test.vn", Email = "u1@test.vn", FullName = "Học viên A" });
        db.Categories.Add(new Category { Id = 1, Name = "Web", Slug = "web" });
        db.Courses.Add(new Course { Id = 10, Title = "Course A", Slug = "course-a", ShortDescription = "s", Description = "d", Price = 500000m, CategoryId = 1, InstructorId = "u2", Status = CourseStatus.Approved });
        db.CartItems.Add(new CartItem { UserId = "u1", CourseId = 10 });
        await db.SaveChangesAsync();

        var svc = TestHelpers.CreatePaymentService(db, vnpayOptions: new VnpayOptions { TmnCode = "TESTTMN", HashSecret = Secret, UseRealGateway = true });

        var (success, order, url, error) = await svc.CreateOrderAndPaymentUrlAsync("u1", PaymentProvider.VNPAY, "https://localhost:7119", "::1");

        Assert.True(success, error);
        Assert.StartsWith("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?", url);
        var query = System.Web.HttpUtility.ParseQueryString(new Uri(url!).Query);
        var dict = query.AllKeys.Where(k => k != null).ToDictionary(k => k!, k => query[k]!);

        Assert.Equal("50000000", dict["vnp_Amount"]);
        Assert.Equal("127.0.0.1", dict["vnp_IpAddr"]);
        Assert.Equal("https://localhost:7119/Payment/VnpayReturn", dict["vnp_ReturnUrl"]);
        Assert.Equal(order!.PaymentTxnRef, dict["vnp_TxnRef"]);
        Assert.Matches("^[A-Za-z0-9]+$", dict["vnp_TxnRef"]);
        var create = DateTime.ParseExact(dict["vnp_CreateDate"], "yyyyMMddHHmmss", null);
        var expire = DateTime.ParseExact(dict["vnp_ExpireDate"], "yyyyMMddHHmmss", null);
        Assert.Equal(TimeSpan.FromMinutes(15), expire - create);
        Assert.True(PaymentGatewayService.VerifyVnpaySignature(dict, Secret, dict["vnp_SecureHash"]));
    }

    [Fact]
    public async Task Checkout_RealGatewayWithoutSecret_ReturnsConfigError()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        var svc = TestHelpers.CreatePaymentService(db, vnpayOptions: new VnpayOptions { TmnCode = "TESTTMN", HashSecret = "", UseRealGateway = true });

        var (success, _, _, error) = await svc.CreateOrderAndPaymentUrlAsync("u1", PaymentProvider.VNPAY, "https://localhost", "127.0.0.1");

        Assert.False(success);
        Assert.Contains("chưa được cấu hình", error);
    }

    [Fact]
    public async Task MockMode_WithEmptySecret_SignsAndVerifiesWithDemoSecret()
    {
        var (db, order) = await SeedOrderAsync();
        using var _db = db;
        var svc = TestHelpers.CreatePaymentService(db, vnpayOptions: new VnpayOptions { TmnCode = "TESTTMN", HashSecret = "" });

        var query = svc.BuildSignedMockSuccessQuery(order, PaymentProvider.VNPAY, "MOCK1");
        var parsed = System.Web.HttpUtility.ParseQueryString(query);
        var dict = parsed.AllKeys.Where(k => k != null).ToDictionary(k => k!, k => parsed[k]!);
        var res = await svc.ProcessVnpayCallbackAsync(dict, query);

        Assert.Equal(TxnRef, dict["vnp_TxnRef"]);
        Assert.Equal(PaymentCallbackStatus.Success, res.Status);
    }

    [Fact]
    public void IsMockGateway_IsFalseForVnpay_WhenRealGatewayEnabled()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        var mock = TestHelpers.CreatePaymentService(db, vnpayOptions: new VnpayOptions { TmnCode = "TESTTMN", HashSecret = Secret });
        var real = TestHelpers.CreatePaymentService(db, vnpayOptions: new VnpayOptions { TmnCode = "TESTTMN", HashSecret = Secret, UseRealGateway = true });

        Assert.True(mock.IsMockGateway(PaymentProvider.VNPAY));
        Assert.False(real.IsMockGateway(PaymentProvider.VNPAY));
        Assert.True(real.IsMockGateway(PaymentProvider.MoMo));
    }

    [Fact]
    public void TxnRef_IsAlphanumeric_AndDiffersPerAttempt()
    {
        var first = PaymentGatewayService.BuildVnpayTxnRef("ORD-SEED-2026001", new DateTime(2026, 11, 22, 10, 30, 0));
        var retry = PaymentGatewayService.BuildVnpayTxnRef("ORD-SEED-2026001", new DateTime(2026, 11, 22, 10, 45, 12));

        Assert.Equal("ORDSEED2026001103000", first);
        Assert.NotEqual(first, retry);
    }

    [Theory]
    [InlineData("::1", "127.0.0.1")]
    [InlineData("::ffff:13.160.92.202", "13.160.92.202")]
    [InlineData("13.160.92.202", "13.160.92.202")]
    [InlineData(null, "127.0.0.1")]
    public void NormalizeClientIp_HandlesLoopbackAndMappedAddresses(string? input, string expected)
    {
        Assert.Equal(expected, PaymentGatewayService.NormalizeClientIp(input));
    }
}
