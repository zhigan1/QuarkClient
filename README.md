# 夸克网盘自动签到 (.NET 版)

.NET 8/10 控制台应用，签到接口为最新移动端接口，支持 GitHub Actions 自动化每日运行。

> 本项目仅供学习交流，请勿用于非法用途。

## 特性

- **极简配置**：支持直接粘贴移动端抓包完整链接（`https://drive-m.quark.cn/...`），自动解析 `kps`、`sign`、`vcode` 等参数，告别手动提取
- **多账号支持**：账号之间通过换行或 `&&` 分隔
- **自动防重复**：已签到自动跳过并展示进度
- **灵活运行**：
  - GitHub Actions 定时执行（默认北京时间每天 10:00，支持手动触发）
  - 本地命令行参数传参
  - 本地 `cookie.txt` 或 `.env` 配置文件
  - 本地控制台交互式粘贴输入
- **安全保障**：本地配置文件已加入 `.gitignore`，防止误传泄露

## 目录结构

```text
.
├── .github/workflows/
│   ├── quark-signin.yml            # 每日签到
│   └── empty-commit-keepalive.yml  # 维持仓库活跃
├── src/QuarkCheckIn/
│   ├── Program.cs                  # 程序入口与配置加载
│   ├── QuarkAccount.cs             # 账号解析（URL / Cookie 自动识别）
│   ├── QuarkClient.cs              # 签到 API 客户端
│   └── QuarkCheckIn.csproj
├── QuarkNetCheckIn.sln
└── README.md
```

## 账号配置格式说明

`COOKIE_QUARK` 支持以下多种格式：

### 1. 直接粘贴抓包链接（最推荐）

抓包获取到的类似如下完整 URL 直接填入即可：

```text
https://drive-m.quark.cn/1/clouddrive/backup/setting?device_model=HBN-AL00&kps=AATAM+uPVg...&sign=AATuCx...&vcode=1790046859657...
```

若未指定账号名，程序会自动读取链接中的 `device_model`（如 `设备_HBN-AL00`）。

### 2. 带自定义备注的抓包链接

支持在链接前加 `user=备注;` 或在链接末尾加 `#备注`：

```text
user=张三的主号; https://drive-m.quark.cn/1/clouddrive/backup/setting?...
```

或

```text
https://drive-m.quark.cn/1/clouddrive/backup/setting?...#张三的主号
```

### 3. 传统 Cookie 格式

```text
user=我的账号; kps=abcdefg; sign=hijklmn; vcode=111111111;
```

### 4. 多账号配置

多个账号之间使用换行或 `&&` 分隔：

```text
https://drive-m.quark.cn/1/clouddrive/backup/setting?...#账号1
&&
user=账号2; https://drive-m.quark.cn/1/clouddrive/backup/setting?...
```

---

## 快速开始（GitHub Actions）

1. Fork 或上传此仓库到你自己的 GitHub。
2. 在仓库 `Settings -> Secrets and variables -> Actions` 中添加 `COOKIE_QUARK` Secret，直接填入抓包链接或 Cookie。
3. 在 `Settings -> Actions -> General` 中将 Workflow permissions 设置为 **Read and write permissions**。
4. 到 Actions 页面，手动触发一次 `Quark 签到 (.NET)` 验证配置。

---

## 本地运行

需要 .NET 8 或更高版本 SDK。

### 方式一：直接运行（交互式输入）

直接运行程序，若未检测到配置，会提示直接在控制台粘贴链接，并可选择保存到 `cookie.txt`：

```bash
dotnet run --project src/QuarkCheckIn
```

### 方式二：命令行参数传参

```bash
dotnet run --project src/QuarkCheckIn -- "https://drive-m.quark.cn/1/clouddrive/backup/setting?..."
```

### 方式三：使用本地配置文件

在项目目录或运行目录创建 `cookie.txt`，将链接直接粘贴进去保存即可（已加入 `.gitignore`）。

### 方式四：环境变量

PowerShell:
```powershell
$env:COOKIE_QUARK = "https://drive-m.quark.cn/1/clouddrive/backup/setting?..."
dotnet run --project src/QuarkCheckIn
```

Linux / macOS:
```bash
export COOKIE_QUARK="https://drive-m.quark.cn/1/clouddrive/backup/setting?..."
dotnet run --project src/QuarkCheckIn
```
