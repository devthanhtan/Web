using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhoaHocTrucTuyen.Data;

namespace QuanLiKhoaHocTrucTuyen.ViewComponents;

public class CartCountViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;

    public CartCountViewComponent(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var count = string.IsNullOrEmpty(userId)
            ? 0
            : await _db.CartItems.CountAsync(c => c.UserId == userId);

        return View(count);
    }
}
