using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Instructor.Controllers;

[Area("Instructor")]
[Authorize(Roles = AppRoles.Instructor)]
public class DashboardController : Controller
{
    private readonly IReportService _reports;

    public DashboardController(IReportService reports)
    {
        _reports = reports;
    }

    public async Task<IActionResult> Index(string period = "month")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var report = await _reports.GetInstructorReportAsync(userId, period);
        return View(report);
    }
}
