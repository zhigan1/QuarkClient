namespace QuarkNetCheckIn;

public sealed record QuarkAccount(string? User, string Kps, string? Sign, string? Vcode)
{
    public static IReadOnlyList<QuarkAccount> ParseAll(string raw)
    {
        var accounts = new List<QuarkAccount>();
        string[] chunks = raw
            .Replace("&&", "\n")
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < chunks.Length; i++)
        {
            accounts.Add(Parse(chunks[i], i + 1));
        }

        return accounts;
    }

    public static QuarkAccount Parse(string chunk, int index)
    {
        chunk = chunk.Trim();
        if (string.IsNullOrWhiteSpace(chunk))
        {
            throw new FormatException($"第 {index} 个账号内容为空。");
        }

        string? user = null;
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 检查是否包含 URL (http:// 或 https://)
        int httpIndex = chunk.IndexOf("http://", StringComparison.OrdinalIgnoreCase);
        if (httpIndex < 0)
        {
            httpIndex = chunk.IndexOf("https://", StringComparison.OrdinalIgnoreCase);
        }

        if (httpIndex >= 0)
        {
            // 如果 URL 前面有备注，例如 "user=我的账号; https://..." 或 "张三: https://..."
            if (httpIndex > 0)
            {
                string prefix = chunk[..httpIndex].Trim().TrimEnd(';', ',', ' ');
                ParsePrefix(prefix, ref user, fields);
            }

            string urlPart = chunk[httpIndex..].Trim();

            // 提取 URL 后面的 #fragment，例如 #我的账号 或 #user=我的账号
            int hashIndex = urlPart.IndexOf('#');
            if (hashIndex >= 0)
            {
                string fragment = urlPart[(hashIndex + 1)..].Trim();
                urlPart = urlPart[..hashIndex];

                if (!string.IsNullOrEmpty(fragment))
                {
                    if (fragment.StartsWith("user=", StringComparison.OrdinalIgnoreCase))
                    {
                        user = fragment["user=".Length..].Trim();
                    }
                    else if (!fragment.Contains('='))
                    {
                        user = fragment;
                    }
                }
            }

            // 提取 Query String
            int qIndex = urlPart.IndexOf('?');
            string queryString = qIndex >= 0 ? urlPart[(qIndex + 1)..] : urlPart;

            ParseDelimitedKeyValues(queryString, '&', fields);
        }
        else if (chunk.Contains('&') && !chunk.Contains(';'))
        {
            // 纯 query 格式，例如 kps=xxx&sign=yyy&vcode=zzz
            int qIndex = chunk.IndexOf('?');
            string queryString = qIndex >= 0 ? chunk[(qIndex + 1)..] : chunk;
            ParseDelimitedKeyValues(queryString, '&', fields);
        }
        else
        {
            // 传统 Cookie 格式，例如 user=xxx; kps=xxx; sign=xxx; vcode=xxx;
            ParseDelimitedKeyValues(chunk, ';', fields);
        }

        if (fields.Count == 0)
        {
            throw new FormatException($"第 {index} 个账号没有解析出任何参数，请检查输入格式。");
        }

        if (!fields.TryGetValue("kps", out string? kps) || string.IsNullOrWhiteSpace(kps))
        {
            throw new FormatException($"第 {index} 个账号缺少 kps 参数。");
        }

        // 如果没有单独指定 user，优先从提取出的 fields 取 user
        if (string.IsNullOrWhiteSpace(user))
        {
            fields.TryGetValue("user", out user);
        }

        // 如果依然没有 user，可优先读取 device_model 作为设备标识
        if (string.IsNullOrWhiteSpace(user) && fields.TryGetValue("device_model", out string? deviceModel) && !string.IsNullOrWhiteSpace(deviceModel))
        {
            user = $"设备_{deviceModel}";
        }

        fields.TryGetValue("sign", out string? sign);
        fields.TryGetValue("vcode", out string? vcode);

        return new QuarkAccount(user, kps, sign, vcode);
    }

    private static void ParsePrefix(string prefix, ref string? user, Dictionary<string, string> fields)
    {
        if (prefix.EndsWith(':'))
        {
            user = prefix[..^1].Trim();
            return;
        }

        if (prefix.StartsWith("user=", StringComparison.OrdinalIgnoreCase))
        {
            user = prefix["user=".Length..].Trim();
            return;
        }

        if (prefix.StartsWith("user:", StringComparison.OrdinalIgnoreCase))
        {
            user = prefix["user:".Length..].Trim();
            return;
        }

        foreach (string part in prefix.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = part.IndexOf('=');
            if (eq > 0)
            {
                string k = part[..eq].Trim();
                string v = part[(eq + 1)..].Trim();
                if (k.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    user = v;
                }
                else
                {
                    fields[k] = v;
                }
            }
            else if (string.IsNullOrEmpty(user))
            {
                user = part;
            }
        }
    }

    private static void ParseDelimitedKeyValues(string input, char delimiter, Dictionary<string, string> fields)
    {
        string[] parts = input.Split(delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            int eq = part.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            string key = part[..eq].Trim();
            string rawValue = part[(eq + 1)..].Trim();

            // 若包含 URL 编码字符则进行解码，保留 Base64 的 + 和 =
            string value = rawValue.Contains('%') ? Uri.UnescapeDataString(rawValue) : rawValue;

            if (key.Length > 0)
            {
                fields[key] = value;
            }
        }
    }
}
