namespace QuanLiKhoaHocTrucTuyen.Options;

/// <summary>
/// Nạp file .env (local, không commit) vào IConfiguration.
/// Hỗ trợ tên tham số VNPAY gửi qua email (vnp_TmnCode, vnp_HashSecret, vnp_Url)
/// và dạng chung Section__Key (ví dụ Smtp__Password).
/// </summary>
public static class DotEnvConfiguration
{
    private static readonly Dictionary<string, string> VnpayAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vnp_TmnCode"] = "Vnpay:TmnCode",
        ["vnp_HashSecret"] = "Vnpay:HashSecret",
        ["vnp_Url"] = "Vnpay:BaseUrl"
    };

    public static ConfigurationManager AddDotEnvFile(this ConfigurationManager configuration, string path)
    {
        if (!File.Exists(path))
            return configuration;

        configuration.AddInMemoryCollection(Parse(File.ReadAllLines(path)));
        // Real environment variables must still win over the local .env file.
        configuration.AddEnvironmentVariables();
        return configuration;
    }

    public static Dictionary<string, string?> Parse(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("export ", StringComparison.Ordinal))
                line = line["export ".Length..].TrimStart();

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
                value = value[1..^1];

            var configKey = VnpayAliases.TryGetValue(key, out var alias) ? alias : key.Replace("__", ":");
            result[configKey] = value;
        }

        // Có đủ thông tin kết nối VNPAY thì dùng cổng thật, trừ khi .env chỉ định khác.
        if (!string.IsNullOrWhiteSpace(result.GetValueOrDefault("Vnpay:TmnCode")) &&
            !string.IsNullOrWhiteSpace(result.GetValueOrDefault("Vnpay:HashSecret")) &&
            !result.ContainsKey("Vnpay:UseRealGateway"))
        {
            result["Vnpay:UseRealGateway"] = "true";
        }

        return result;
    }
}
