using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : Controller
{
    private readonly IReportService _reports;

    public DashboardController(IReportService reports)
    {
        _reports = reports;
    }

    public async Task<IActionResult> Index(string period = "month")
    {
        var report = await _reports.GetAdminReportAsync(period);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(string period = "month")
    {
        var bytes = await _reports.ExportAdminReportCsvAsync(period);
        return File(bytes, "text/csv; charset=utf-8", $"BaoCao_EduSpark_{period}_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
