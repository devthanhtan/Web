using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class CertificateService : ICertificateService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CertificateService(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<Certificate> IssueOrGetCertificateAsync(string userId, int courseId, int quizAttemptId, string baseUrl)
    {
        var existing = await _db.Certificates
            .Include(c => c.Course).ThenInclude(co => co.Instructor)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);

        if (existing != null)
            return existing;

        var user = await _db.Users.FirstAsync(u => u.Id == userId);
        var course = await _db.Courses.Include(c => c.Instructor).FirstAsync(c => c.Id == courseId);

        var code = $"CERT-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var verifyUrl = $"{baseUrl.TrimEnd('/')}/Certificate/Verify?code={code}";

        var cert = new Certificate
        {
            Code = code,
            UserId = userId,
            CourseId = courseId,
            QuizAttemptId = quizAttemptId,
            HolderName = user.FullName,
            CourseTitle = course.Title,
            IssuedAt = DateTime.UtcNow,
            QrData = verifyUrl
        };

        var pdfBytes = BuildPdf(cert, course.Instructor.FullName);
        var certDir = Path.Combine(_env.WebRootPath, "uploads", "certificates");
        Directory.CreateDirectory(certDir);
        var pdfFileName = $"{code}.pdf";
        await File.WriteAllBytesAsync(Path.Combine(certDir, pdfFileName), pdfBytes);
        cert.PdfPath = $"/uploads/certificates/{pdfFileName}";

        _db.Certificates.Add(cert);
        await _db.SaveChangesAsync();
        return cert;
    }

    public async Task<Certificate?> FindByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return await _db.Certificates
            .Include(c => c.Course).ThenInclude(co => co.Instructor)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Code == code.Trim());
    }

    public async Task<byte[]> GeneratePdfBytesAsync(Certificate certificate)
    {
        if (!string.IsNullOrEmpty(certificate.PdfPath))
        {
            var physical = Path.Combine(_env.WebRootPath, certificate.PdfPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(physical))
                return await File.ReadAllBytesAsync(physical);
        }

        var instructorName = certificate.Course?.Instructor?.FullName ?? "Giảng viên HUIT Learning";
        return BuildPdf(certificate, instructorName);
    }

    public byte[] GenerateQrPngBytes(string qrData)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(8);
    }

    private byte[] BuildPdf(Certificate cert, string instructorName)
    {
        var qrBytes = GenerateQrPngBytes(cert.QrData);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.PageColor(Colors.White);

                page.Content().Border(4).BorderColor("#0054A6").Padding(24).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("HUIT LEARNING · NỀN TẢNG HỌC TẬP SỐ").FontSize(14).Bold().FontColor("#0054A6");
                            left.Item().Text("CHỨNG NHẬN HOÀN THÀNH KHÓA HỌC").FontSize(24).Bold().FontColor("#0F172A");
                        });
                        row.ConstantItem(96).AlignRight().Image(qrBytes);
                    });

                    col.Item().PaddingTop(14).LineHorizontal(1.5f).LineColor("#BFDBFE");

                    col.Item().PaddingTop(14).Text("Chứng nhận này trân trọng trao cho:").FontSize(13).FontColor("#64748B");
                    col.Item().Text(cert.HolderName).FontSize(28).Bold().FontColor("#EA580C");

                    col.Item().PaddingTop(6).Text("Đã hoàn thành xuất sắc toàn bộ nội dung và vượt qua bài thi cuối khóa của:").FontSize(13).FontColor("#334155");
                    col.Item().Text(cert.CourseTitle).FontSize(20).Bold().FontColor("#0F172A");

                    col.Item().PaddingTop(24).Row(footer =>
                    {
                        footer.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Giảng viên hướng dẫn: {instructorName}").FontSize(12).Bold();
                            c.Item().Text($"Ngày cấp: {cert.IssuedAt.ToLocalTime():dd/MM/yyyy} · Đơn vị cấp: HUIT Learning").FontSize(11).FontColor("#64748B");
                        });
                        footer.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Mã chứng chỉ: {cert.Code}").FontSize(11).Bold().FontColor("#0F172A");
                            c.Item().AlignRight().Text("Quét mã QR để tra cứu & xác thực trực tuyến trên HUIT Learning").FontSize(10).FontColor("#64748B");
                        });
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }
}
