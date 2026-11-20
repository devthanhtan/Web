using QuanLiKhoaHocTrucTuyen.Models;

namespace QuanLiKhoaHocTrucTuyen.Services.Interfaces;

public record QuestionImportResult(
    bool Success,
    int ImportedCount,
    List<string> RowErrors);

public interface IQuestionImportService
{
    byte[] GenerateExcelTemplate();
    Task<QuestionImportResult> ImportFromStreamAsync(int courseId, Stream stream, string fileExtension);
}
