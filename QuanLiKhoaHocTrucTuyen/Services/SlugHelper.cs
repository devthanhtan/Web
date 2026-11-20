using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace QuanLiKhoaHocTrucTuyen.Services;

public static class SlugHelper
{
    public static string ToSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("n")[..8];

        var normalized = input.Trim().ToLowerInvariant()
            .Replace('đ', 'd')
            .Replace('Đ', 'd')
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var clean = sb.ToString().Normalize(NormalizationForm.FormC);
        clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");
        clean = Regex.Replace(clean, @"\s+", "-").Trim('-');
        clean = Regex.Replace(clean, @"-+", "-");
        return string.IsNullOrEmpty(clean) ? Guid.NewGuid().ToString("n")[..8] : clean;
    }
}
