using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services;

namespace QuanLiKhoaHocTrucTuyen.Tests;

public class PaymentSignatureAndIdempotencyTests
{
    [Fact]
    public void VnpaySignature_Valid_ReturnsTrue_AndTampered_ReturnsFalse()
    {
        const string secret = "VNPAY_TEST_SECRET";
        var raw = "vnp_Amount=79900000&vnp_ResponseCode=00&vnp_TxnRef=ORD001";
        var validHash = PaymentGatewayService.ComputeHmacSha512(secret, raw);

        var validDict = new Dictionary<string, string>
        {
            ["vnp_Amount"] = "79900000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TxnRef"] = "ORD001",
            ["vnp_SecureHash"] = validHash
        };
        Assert.True(PaymentGatewayService.VerifyVnpaySignature(validDict, secret, validHash));

        // Tamper amount
        validDict["vnp_Amount"] = "100";
        Assert.False(PaymentGatewayService.VerifyVnpaySignature(validDict, secret, validHash));
    }

    [Fact]
    public void MomoSignature_Valid_ReturnsTrue_AndTampered_ReturnsFalse()
    {
        const string accessKey = "ACC";
        const string secret = "MOMO_SECRET";
        var rawHash = "accessKey=ACC&amount=500000&extraData=&message=Successful.&orderId=ORD002&orderInfo=Test&orderType=momo_wallet&partnerCode=MOMO&payType=qr&requestId=REQ1&responseTime=12345&resultCode=0&transId=TX99";
        var validSig = PaymentGatewayService.ComputeHmacSha256(secret, rawHash);

        var dict = new Dictionary<string, string>
        {
            ["partnerCode"] = "MOMO",
            ["orderId"] = "ORD002",
            ["requestId"] = "REQ1",
            ["amount"] = "500000",
            ["orderInfo"] = "Test",
            ["orderType"] = "momo_wallet",
            ["transId"] = "TX99",
            ["resultCode"] = "0",
            ["message"] = "Successful.",
            ["payType"] = "qr",
            ["responseTime"] = "12345",
            ["extraData"] = "",
            ["signature"] = validSig
        };

        Assert.True(PaymentGatewayService.VerifyMomoSignature(dict, accessKey, secret, validSig));

        dict["amount"] = "1";
        Assert.False(PaymentGatewayService.VerifyMomoSignature(dict, accessKey, secret, validSig));
    }

    [Fact]
    public async Task ProcessVnpayCallback_RejectsInvalidSignature_AndHandlesPaidIdempotently()
    {
        using var db = TestHelpers.CreateInMemoryDb();
        var user = new ApplicationUser { Id = "u1", UserName = "u1@test.vn", Email = "u1@test.vn", FullName = "Học viên A" };
        var cat = new Category { Id = 1, Name = "Web", Slug = "web" };
        var course = new Course { Id = 10, Title = "Course A", Slug = "course-a", ShortDescription = "s", Description = "d", Price = 500000m, CategoryId = 1, InstructorId = "u1", Status = CourseStatus.Approved };
        var order = new Order
        {
            Id = 1,
            OrderCode = "ORD-TEST-01",
            UserId = "u1",
            TotalAmount = 500000m,
            Status = OrderStatus.PendingPayment,
            PaymentProvider = PaymentProvider.VNPAY,
            Items = [new OrderItem { CourseId = 10, UnitPrice = 500000m, CommissionRatePercent = 20m, PlatformAmount = 100000m, InstructorAmount = 400000m }]
        };
        db.Users.Add(user);
        db.Categories.Add(cat);
        db.Courses.Add(course);
        db.Orders.Add(order);
        db.CartItems.Add(new CartItem { UserId = "u1", CourseId = 10 });
        await db.SaveChangesAsync();

        var svc = TestHelpers.CreatePaymentService(db, vnpSecret: "TEST_VNP_SECRET");

        // 1. Invalid signature must be rejected
        var badRes = await svc.ProcessVnpayCallbackAsync(new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = "ORD-TEST-01",
            ["vnp_Amount"] = "50000000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_SecureHash"] = "deadbeef"
        }, "bad");
        Assert.False(badRes.IsSuccess);
        Assert.False(badRes.SignatureValid);
        Assert.Equal(0, await db.Enrollments.CountAsync());

        // 2. Valid signed success query activates enrollment and clears cart
        var signedQuery = svc.BuildSignedMockSuccessQuery(order, PaymentProvider.VNPAY, "VNP999");
        var parsed = System.Web.HttpUtility.ParseQueryString(signedQuery);
        var dict = parsed.AllKeys.Where(k => k != null).ToDictionary(k => k!, k => parsed[k]!);

        var okRes = await svc.ProcessVnpayCallbackAsync(dict, signedQuery);
        Assert.True(okRes.IsSuccess);
        Assert.False(okRes.AlreadyProcessed);
        Assert.Equal(OrderStatus.Paid, (await db.Orders.FirstAsync()).Status);
        Assert.Equal(1, await db.Enrollments.CountAsync());
        Assert.Equal(0, await db.CartItems.CountAsync());

        // 3. Replaying same callback must be idempotent (no duplicate enrollments)
        var replayRes = await svc.ProcessVnpayCallbackAsync(dict, signedQuery);
        Assert.True(replayRes.IsSuccess);
        Assert.True(replayRes.AlreadyProcessed);
        Assert.Equal(1, await db.Enrollments.CountAsync());
    }
}
