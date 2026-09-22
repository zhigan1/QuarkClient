using System.Text;
using QuarkNetCheckIn;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("---------- 夸克网盘自动签到 (.NET) ----------");

string? raw = null;

// 1. 优先从命令行参数获取
if (args.Length > 0)
{
    raw = string.Join(" ", args);
}

// 2. 其次从环境变量 COOKIE_QUARK 获取
if (string.IsNullOrWhiteSpace(raw))
{
    raw = Environment.GetEnvironmentVariable("COOKIE_QUARK");
}

// 3. 再次从本地文件 cookie.txt / cookies.txt / .env 获取
if (string.IsNullOrWhiteSpace(raw))
{
    string[] candidateFiles = ["cookie.txt", "cookies.txt", ".env"];
    foreach (string file in candidateFiles)
    {
        string path = Path.Combine(AppContext.BaseDirectory, file);
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), file);
        }

        if (File.Exists(path))
        {
            string content = File.ReadAllText(path).Trim();
            if (!string.IsNullOrWhiteSpace(content))
            {
                if (file.Equals(".env", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (line.StartsWith("COOKIE_QUARK=", StringComparison.OrdinalIgnoreCase))
                        {
                            raw = line["COOKIE_QUARK=".Length..].Trim().Trim('"', '\'');
                            break;
                        }
                    }
                }
                else
                {
                    raw = content;
                }

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    Console.WriteLine($"已从本地配置文件 {Path.GetFileName(path)} 读取账号信息。");
                    break;
                }
            }
        }
    }
}

// 4. 控制台交互式输入（当没有重定向且未读取到有效配置时）
if (string.IsNullOrWhiteSpace(raw) && !Console.IsInputRedirected)
{
    Console.WriteLine("未检测到 COOKIE_QUARK 配置。");
    Console.WriteLine("支持直接粘贴夸克抓包链接 (https://drive-m.quark.cn/...) 或传统 Cookie 格式。");
    Console.Write("请输入链接或 Cookie: ");
    raw = Console.ReadLine();

    if (!string.IsNullOrWhiteSpace(raw))
    {
        Console.Write("是否将此配置保存到 cookie.txt 以便下次直接免输入运行？(y/N): ");
        string? saveChoice = Console.ReadLine()?.Trim();
        if (string.Equals(saveChoice, "y", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                File.WriteAllText("cookie.txt", raw);
                Console.WriteLine("已保存至 cookie.txt（该文件已被 git 忽略，不会泄露）。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存失败: {ex.Message}");
            }
        }
    }
}

if (string.IsNullOrWhiteSpace(raw))
{
    Console.WriteLine("未提供有效的账号配置，程序退出。");
    Console.WriteLine("配置方式：");
    Console.WriteLine("  1. 设置环境变量 COOKIE_QUARK");
    Console.WriteLine("  2. 在程序同级目录新建 cookie.txt 放入抓包链接或 Cookie");
    Console.WriteLine("  3. 命令行参数: dotnet run -- \"<夸克链接或Cookie>\"");
    return 1;
}

IReadOnlyList<QuarkAccount> accounts;
try
{
    accounts = QuarkAccount.ParseAll(raw);
}
catch (FormatException ex)
{
    Console.WriteLine($"账号解析错误: {ex.Message}");
    return 1;
}

if (accounts.Count == 0)
{
    Console.WriteLine("没有解析到有效的账号信息。");
    return 1;
}

Console.WriteLine($"检测到 {accounts.Count} 个夸克账号");

bool hasError = false;
for (int i = 0; i < accounts.Count; i++)
{
    Console.WriteLine();
    Console.WriteLine($"===== 账号 {i + 1}/{accounts.Count} =====");

    try
    {
        string result = await QuarkClient.SignInAsync(accounts[i]);
        Console.WriteLine(result);
    }
    catch (Exception ex)
    {
        hasError = true;
        Console.WriteLine($"签到异常: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("---------- 夸克网盘签到完毕 ----------");
return hasError ? 1 : 0;
