using System.Text;
using ClosedXML.Excel;
using QuanLiKhoaHocTrucTuyen.Data;
using QuanLiKhoaHocTrucTuyen.Models;
using QuanLiKhoaHocTrucTuyen.Services.Interfaces;

namespace QuanLiKhoaHocTrucTuyen.Services;

public class QuestionImportService : IQuestionImportService
{
    private readonly ApplicationDbContext _db;

    public QuestionImportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public byte[] GenerateExcelTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Questions");

        string[] headers = ["Topic", "Content", "OptionA", "OptionB", "OptionC", "OptionD", "CorrectOption", "Explanation"];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7ED");
        }

        // Sample row
        ws.Cell(2, 1).Value = "ASP.NET Core";
        ws.Cell(2, 2).Value = "Phương thức nào trong Controller trả về Razor View?";
        ws.Cell(2, 3).Value = "View()";
        ws.Cell(2, 4).Value = "Sql()";
        ws.Cell(2, 5).Value = "Console()";
        ws.Cell(2, 6).Value = "Build()";
        ws.Cell(2, 7).Value = "A";
        ws.Cell(2, 8).Value = "Controller.View() trả về ViewResult mặc định.";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<QuestionImportResult> ImportFromStreamAsync(int courseId, Stream stream, string fileExtension)
    {
        var ext = fileExtension.ToLowerInvariant();
        var parsedRows = new List<(int RowNumber, string Topic, string Content, string A, string B, string C, string D, string Correct, string Explanation)>();
        var errors = new List<string>();

        if (ext == ".xlsx")
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null)
                return new QuestionImportResult(false, 0, ["File Excel không có trang tính (worksheet) nào."]);

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (int r = 2; r <= lastRow; r++)
            {
                var topic = ws.Cell(r, 1).GetString().Trim();
                var content = ws.Cell(r, 2).GetString().Trim();
                var a = ws.Cell(r, 3).GetString().Trim();
                var b = ws.Cell(r, 4).GetString().Trim();
                var c = ws.Cell(r, 5).GetString().Trim();
                var d = ws.Cell(r, 6).GetString().Trim();
                var correct = ws.Cell(r, 7).GetString().Trim().ToUpperInvariant();
                var exp = ws.Cell(r, 8).GetString().Trim();

                if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(correct))
                    continue;

                parsedRows.Add((r, topic, content, a, b, c, d, correct, exp));
            }
        }
        else if (ext == ".csv")
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            int lineNum = 0;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                lineNum++;
                if (lineNum == 1 || string.IsNullOrWhiteSpace(line)) continue;

                var cols = ParseCsvLine(line);
                if (cols.Count < 7)
                {
                    errors.Add($"Dòng {lineNum}: Thiếu cột dữ liệu (yêu cầu tối thiểu 7 cột: Topic,Content,OptionA,OptionB,OptionC,OptionD,CorrectOption).");
                    continue;
                }

                parsedRows.Add((
                    lineNum,
                    cols[0].Trim(),
                    cols[1].Trim(),
                    cols[2].Trim(),
                    cols[3].Trim(),
                    cols[4].Trim(),
                    cols[5].Trim(),
                    cols[6].Trim().ToUpperInvariant(),
                    cols.Count > 7 ? cols[7].Trim() : string.Empty));
            }
        }
        else
        {
            return new QuestionImportResult(false, 0, ["Chỉ hỗ trợ định dạng .xlsx hoặc .csv."]);
        }

        if (!parsedRows.Any() && !errors.Any())
        {
            errors.Add("File không có dòng dữ liệu câu hỏi nào.");
        }

        var validEntities = new List<Question>();
        foreach (var row in parsedRows)
        {
            var rowErrs = new List<string>();
            if (string.IsNullOrWhiteSpace(row.Content)) rowErrs.Add("thiếu nội dung câu hỏi");
            if (string.IsNullOrWhiteSpace(row.A) || string.IsNullOrWhiteSpace(row.B) ||
                string.IsNullOrWhiteSpace(row.C) || string.IsNullOrWhiteSpace(row.D))
                rowErrs.Add("thiếu đủ 4 đáp án A, B, C, D");
            if (row.Correct is not ("A" or "B" or "C" or "D"))
                rowErrs.Add($"đáp án đúng '{row.Correct}' phải là A, B, C hoặc D");

            if (rowErrs.Any())
            {
                errors.Add($"Dòng {row.RowNumber}: {string.Join("; ", rowErrs)}.");
            }
            else
            {
                validEntities.Add(new Question
                {
                    CourseId = courseId,
                    Topic = string.IsNullOrEmpty(row.Topic) ? null : row.Topic,
                    Content = row.Content,
                    OptionA = row.A,
                    OptionB = row.B,
                    OptionC = row.C,
                    OptionD = row.D,
                    CorrectOption = row.Correct,
                    Explanation = string.IsNullOrEmpty(row.Explanation) ? null : row.Explanation,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (errors.Any())
        {
            return new QuestionImportResult(false, 0, errors);
        }

        _db.Questions.AddRange(validEntities);
        await _db.SaveChangesAsync();
        return new QuestionImportResult(true, validEntities.Count, []);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }
        result.Add(sb.ToString());
        return result;
    }
}
