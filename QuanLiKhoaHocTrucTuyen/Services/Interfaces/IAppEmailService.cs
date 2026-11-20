using Microsoft.AspNetCore.Identity.UI.Services;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface IAppEmailService : IEmailSender
{
    Task<string?> GetLatestMockConfirmationUrlAsync(string email);
}
