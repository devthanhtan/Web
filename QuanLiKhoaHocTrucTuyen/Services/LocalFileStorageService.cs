using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<(bool Success, string? RelativePath, string? Error)> SaveFileAsync(
        IFormFile file,
        string subfolder,
        string[] allowedExtensions,
        long maxSizeBytes,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return (false, null, "Vui lòng chọn tệp hợp lệ.");

        if (file.Length > maxSizeBytes)
        {
            var maxMb = Math.Round(maxSizeBytes / (1024.0 * 1024.0), 1);
            return (false, null, $"Dung lượng tệp vượt quá giới hạn {maxMb} MB.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
        {
            return (false, null, $"Định dạng tệp không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}");
        }

        var safeSubfolder = subfolder.Replace("..", "").Trim('/', '\\');
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", safeSubfolder);
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(uploadDir, fileName);

        await using var stream = new FileStream(physicalPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        var relativePath = $"/uploads/{safeSubfolder.Replace('\\', '/')}/{fileName}";
        return (true, relativePath, null);
    }

    public void DeleteRelativeFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        var physical = ResolvePhysicalPath(relativePath);
        if (File.Exists(physical))
        {
            File.Delete(physical);
        }
    }

    public string ResolvePhysicalPath(string relativePath)
    {
        var cleaned = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_env.WebRootPath, cleaned));
        var rootFull = Path.GetFullPath(_env.WebRootPath);
        if (!fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn tệp không hợp lệ.");
        return fullPath;
    }
}
