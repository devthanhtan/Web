using QuanLiKhoaHocTrucTuyen.Options;

namespace QuanLiKhoaHocTrucTuyen.Tests;

public class DotEnvConfigurationTests
{
    [Fact]
    public void Parse_MapsVnpayEmailKeys_AndEnablesRealGateway()
    {
        var result = DotEnvConfiguration.Parse(
        [
            "vnp_TmnCode=TESTTMN1",
            "vnp_HashSecret=SECRET123",
            "vnp_Url=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
        ]);

        Assert.Equal("TESTTMN1", result["Vnpay:TmnCode"]);
        Assert.Equal("SECRET123", result["Vnpay:HashSecret"]);
        Assert.Equal("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html", result["Vnpay:BaseUrl"]);
        Assert.Equal("true", result["Vnpay:UseRealGateway"]);
    }

    [Fact]
    public void Parse_HandlesCommentsQuotesExportAndSectionKeys()
    {
        var result = DotEnvConfiguration.Parse(
        [
            "# comment",
            "",
            "export Smtp__Password=\"p@ss=word\"",
            "Vnpay__UseRealGateway=false",
            "vnp_TmnCode='TESTTMN1'",
            "vnp_HashSecret=SECRET123",
            "not a pair"
        ]);

        Assert.Equal("p@ss=word", result["Smtp:Password"]);
        Assert.Equal("TESTTMN1", result["Vnpay:TmnCode"]);
        Assert.Equal("false", result["Vnpay:UseRealGateway"]);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Parse_WithoutSecret_DoesNotEnableRealGateway()
    {
        var result = DotEnvConfiguration.Parse(["vnp_TmnCode=TESTTMN1"]);

        Assert.False(result.ContainsKey("Vnpay:UseRealGateway"));
    }
}
