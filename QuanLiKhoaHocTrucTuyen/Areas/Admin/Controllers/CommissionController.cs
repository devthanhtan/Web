using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class CommissionController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IReportService _reports;

    public CommissionController(ApplicationDbContext db, IReportService reports)
    {
        _db = db;
        _reports = reports;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Latest = await _reports.GetLatestCommissionAsync();
        var history = await _db.CommissionConfigs
            .Include(c => c.UpdatedByAdmin)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(20)
            .ToListAsync();
        return View(history);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(decimal platformRatePercent, string? note)
    {
        if (platformRatePercent < 0 || platformRatePercent > 90)
        {
            TempData["Error"] = "Tỷ lệ chiết khấu nền tảng phải nằm trong khoảng 0% - 90%.";
            return RedirectToAction(nameof(Index));
        }

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _reports.UpdateCommissionAsync(platformRatePercent, note, adminId);
        TempData["Success"] = $"Đã cập nhật tỷ lệ hoa hồng nền tảng mới: {platformRatePercent:0.#}% (áp dụng cho các đơn thanh toán từ thời điểm này).";
        return RedirectToAction(nameof(Index));
    }
}
