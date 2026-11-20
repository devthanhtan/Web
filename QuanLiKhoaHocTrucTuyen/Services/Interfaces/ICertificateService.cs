using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface ICertificateService
{
    Task<Certificate> IssueOrGetCertificateAsync(string userId, int courseId, int quizAttemptId, string baseUrl);
    Task<Certificate?> FindByCodeAsync(string code);
    Task<byte[]> GeneratePdfBytesAsync(Certificate certificate);
    byte[] GenerateQrPngBytes(string qrData);
}
