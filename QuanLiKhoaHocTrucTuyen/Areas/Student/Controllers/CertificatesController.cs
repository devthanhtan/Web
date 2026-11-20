using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = AppRoles.Student)]
public class CertificatesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICertificateService _certificates;

    public CertificatesController(ApplicationDbContext db, ICertificateService certificates)
    {
        _db = db;
        _certificates = certificates;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var certs = await _db.Certificates
            .Include(c => c.Course).ThenInclude(co => co.Instructor)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

        return View(certs);
    }

    public async Task<IActionResult> DownloadPdf(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cert = await _db.Certificates
            .Include(c => c.Course).ThenInclude(co => co.Instructor)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (cert == null) return NotFound();

        var bytes = await _certificates.GeneratePdfBytesAsync(cert);
        return File(bytes, "application/pdf", $"{cert.Code}.pdf");
    }
}
