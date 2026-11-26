using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using QuanLiKhoaHocTrucTuyen.Options;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class MailKitEmailSender : IAppEmailService
{
    private readonly SmtpOptions _smtp;
    private readonly AppFeaturesOptions _features;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(
        IOptions<SmtpOptions> smtp,
        IOptions<AppFeaturesOptions> features,
        IWebHostEnvironment env,
        ILogger<MailKitEmailSender> logger)
    {
        _smtp = smtp.Value;
        _features = features.Value;
        _env = env;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var senderName = string.IsNullOrWhiteSpace(_smtp.FromName) ? "HUIT Learning" : _smtp.FromName;

        if (_features.UseMockServices || string.IsNullOrWhiteSpace(_smtp.Host))
        {
            var mockDir = Path.Combine(_env.ContentRootPath, "App_Data", "MockEmails");
            Directory.CreateDirectory(mockDir);
            var safeEmail = Regex.Replace(email, @"[^a-zA-Z0-9._-]", "_");
            var filePath = Path.Combine(mockDir, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeEmail}.html");
            var content = $"<!-- From: {senderName} | To: {email} | Subject: {subject} -->\n{htmlMessage}";
            await File.WriteAllTextAsync(filePath, content);
            _logger.LogInformation("[MockEmail - HUIT Learning] Đã lưu email tới {Email} với tiêu đề '{Subject}' tại {FilePath}", email, subject, filePath);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, _smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocket = _smtp.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(_smtp.Host, _smtp.Port, secureSocket);
        if (!string.IsNullOrWhiteSpace(_smtp.UserName))
        {
            await client.AuthenticateAsync(_smtp.UserName, _smtp.Password);
        }
        await client.SendAsync(new SmtpSendRequest(message));
        await client.DisconnectAsync(true);
        _logger.LogInformation("Đã gửi email tới {Email} với tiêu đề '{Subject}'", email, subject);
    }

    public async Task<string?> GetLatestMockConfirmationUrlAsync(string email)
    {
        var mockDir = Path.Combine(_env.ContentRootPath, "App_Data", "MockEmails");
        if (!Directory.Exists(mockDir)) return null;

        var safeEmail = Regex.Replace(email, @"[^a-zA-Z0-9._-]", "_");
        var latestFile = Directory.GetFiles(mockDir, $"*_{safeEmail}.html")
            .OrderByDescending(f => f)
            .FirstOrDefault();
        if (latestFile == null) return null;

        var html = await File.ReadAllTextAsync(latestFile);
        var match = Regex.Match(html, @"href=[""']([^""']*ConfirmEmail[^""']*)[""']", RegexOptions.IgnoreCase);
        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }
}
