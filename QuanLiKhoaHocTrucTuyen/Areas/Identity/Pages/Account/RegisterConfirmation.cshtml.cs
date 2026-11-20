using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAppEmailService _emailService;
    private readonly AppFeaturesOptions _features;

    public RegisterConfirmationModel(
        UserManager<ApplicationUser> userManager,
        IAppEmailService emailService,
        IOptions<AppFeaturesOptions> features)
    {
        _userManager = userManager;
        _emailService = emailService;
        _features = features.Value;
    }

    public string Email { get; set; } = string.Empty;
    public bool DisplayConfirmAccountLink { get; set; }
    public string? EmailConfirmationUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string? email, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToPage("/Index");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound($"Không tìm thấy tài khoản với email '{email}'.");

        Email = email;
        DisplayConfirmAccountLink = _features.UseMockServices;
        if (DisplayConfirmAccountLink)
        {
            EmailConfirmationUrl = await _emailService.GetLatestMockConfirmationUrlAsync(email);
        }

        return Page();
    }
}
