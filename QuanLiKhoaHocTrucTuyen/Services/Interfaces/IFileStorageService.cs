namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public interface IFileStorageService
{
    Task<(bool Success, string? RelativePath, string? Error)> SaveFileAsync(
        IFormFile file,
        string subfolder,
        string[] allowedExtensions,
        long maxSizeBytes,
        CancellationToken ct = default);

    void DeleteRelativeFile(string? relativePath);
    string ResolvePhysicalPath(string relativePath);
}
