using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Controllers;

public class CertificateController : Controller
{
    private readonly ICertificateService _certificates;

    public CertificateController(ICertificateService certificates)
    {
        _certificates = certificates;
    }

    public async Task<IActionResult> Verify(string? code)
    {
        ViewBag.SearchCode = code;
        if (string.IsNullOrWhiteSpace(code))
            return View(null);

        var cert = await _certificates.FindByCodeAsync(code);
        return View(cert);
    }

    public async Task<IActionResult> QrImage(string code)
    {
        var cert = await _certificates.FindByCodeAsync(code);
        if (cert == null) return NotFound();

        var bytes = _certificates.GenerateQrPngBytes(cert.QrData);
        return File(bytes, "image/png");
    }

    public async Task<IActionResult> DownloadPdf(string code)
    {
        var cert = await _certificates.FindByCodeAsync(code);
        if (cert == null) return NotFound();

        var bytes = await _certificates.GeneratePdfBytesAsync(cert);
        return File(bytes, "application/pdf", $"{cert.Code}.pdf");
    }
}
