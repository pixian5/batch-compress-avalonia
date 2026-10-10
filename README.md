# BatchCompress

A cross-platform batch compression and decompression tool built with Avalonia.

## 【当前开发进度】

当前版本 **0.0.2**。UI 与 MVVM 骨架已完成，压缩/解压/测试功能代码已完成，但**尚未通过真机验证，目前存在 6 个 P0 级问题导致本机实际无法完成任何压缩/解压操作**（最主要是找不到压缩引擎：本机只有 `7zz`，且 .app 从 Finder 启动时 PATH 不含 `/opt/homebrew/bin`）。

- 详细进度：[docs/20260929-2350-当前开发进度-未标注.md](docs/20260929-2350-当前开发进度-未标注.md)

## 【下一步待实现】

按 P0（让功能真正可用）→ P1（补齐核心能力）→ P2（工程化与体验）三阶段推进，每阶段完成后必须真机运行验证再递增版本号、重建 .app 并复制到 `/Applications`。

- 详细计划：[docs/20260929-2350-下一步待实现-未标注.md](docs/20260929-2350-下一步待实现-未标注.md)

## Features

- Compress files and folders using 7-Zip
- Support multiple formats: 7z, zip, rar, tar, gz
- Compression levels: 无压缩, 快速, 正常, 最大, 极限
- Decompress archives (7z, zip, rar, tar, gz, bz2)
- Password protection (random or custom)
- Solid archive support
- Skip already compressed files
- Cross-platform support (Windows, macOS, Linux)
- Progress display and cancellation

## Requirements

- .NET 10.0 or later
- 7-Zip installed and in PATH
  - macOS: `brew install sevenzip`
  - Windows: Download from https://www.7-zip.org/ and add to PATH
  - Linux: `sudo apt install p7zip-full` or equivalent
- **Note**: RAR format requires appropriate licensing. Consider using 7z for better compatibility.

## Building and Running

1. Restore dependencies:
   ```
   dotnet restore
   ```

2. Build the project:
   ```
   dotnet build
   ```

3. Run the application:
   ```
   dotnet run --project BatchCompress/BatchCompress.csproj
   ```

## Usage

1. Select the source folder containing files to compress/decompress.
2. Choose the save path for compressed files.
3. Select archive format (7z, zip, tar, gz).
4. Choose compression level (无压缩 to 极限).
5. Enter password or check "随机密码" for random password.
6. Configure options as needed (solid archive, skip compressed files).
7. Click "压缩" to compress or "解压" to decompress.
8. Monitor progress and logs.

## Development

This project uses Avalonia for the UI and CommunityToolkit.Mvvm for MVVM pattern.

## 文档索引（docs 目录）

> 完整索引另见根目录 [文档索引.md](文档索引.md)。

| 文档 | 作用 |
|---|---|
| [202609292350项目分析与开发建议.md](docs/202609292350项目分析与开发建议.md) | 全量代码分析报告：项目现状、6 个 P0 致命问题（含本机实测证据）、13 项 P1 功能缺陷、9 项 P2 工程质量问题、三阶段推进路线、待决策项、实测证据清单 |
| [20260929-2350-当前开发进度-未标注.md](docs/20260929-2350-当前开发进度-未标注.md) | 当前版本、已完成项、实际可用性验证状态、已发现问题的汇总与阻塞情况 |
| [20260929-2350-下一步待实现-未标注.md](docs/20260929-2350-下一步待实现-未标注.md) | 待办清单（P0/P1/P2 分阶段勾选项）、每项验收标准、需项目所有者确认的决策项 |