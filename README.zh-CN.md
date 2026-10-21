# PictureFetcher

[English](README.md) | 简体中文

PictureFetcher 是一款用于 **128 × 64 单色 OLED 屏幕**的轻量级 Windows 图像取模工具，支持图像缩放、黑白二值化、批量 BMP 转换，以及生成单片机项目可用的十六进制或 C 数组格式字模数据。

## 功能

- 将图像转换为 128 × 64 的单色图像，并提供可调节阈值。
- 支持颜色取反、十六进制模式和 C 数组模式。
- 单张图片导入、批量转换和导出 BMP 文件。
- 支持调整窗口透明度。
- 支持**简体中文 / English** 界面切换，首次运行按照系统 UI 语言选择（非中文系统默认英文），手动选择后自动记忆。

## 下载安装

前往 [GitHub Releases](https://github.com/bestyize/PictureFetcher/releases) 下载 Windows 发布包。发布包包含自带运行时的可执行文件，**无需另行安装 .NET 桌面运行时**。

- `win-x64`：适用于大部分 Intel/AMD Windows 10/11 设备。
- `win-arm64`：适用于 ARM 架构 Windows 设备。

解压 ZIP 后直接运行 `PictureReader.exe`。当前提供免安装版本，尚未进行代码签名，首次启动可能出现 Windows SmartScreen 提示。

**平台说明：**本软件采用 Windows Forms，仅支持 Windows。升级到 .NET 10 并不意味着支持 macOS 或 Linux。

## 使用方法

1. 点击**单个转换**选择图片，或者使用**批量转换**选择多张图片。
2. 根据预览调整**阈值**，必要时勾选**取反**。
3. 使用**十六进制模式**或**字模模式**查看生成的数据。
4. 点击**保存 BMP**导出当前图片；批量转换将自动导出。
5. 在窗口右上角通过**语言**选择框切换中文和英文。

**注意：**BMP 文件保存在原始图片目录下，采用相同的文件主名。如果目录中已存在同名 BMP，请预先备份，避免覆盖。

## 源码构建

需要 **.NET 10 SDK** 和 Windows 开发环境（也可以使用 Windows 构建代理）。

```powershell
dotnet restore PictureFetcher.csproj
dotnet build PictureFetcher.csproj --configuration Release
dotnet run --project PictureFetcher.csproj
```

发布独立运行的 Windows x64 单文件程序：

```powershell
dotnet publish PictureFetcher.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None --output publish/win-x64
```

## 自动化构建和发布

- **CI**：每次推送以及 PR 都会触发 Windows 构建。
- **Release**：推送 `v*` 标签后构建 Windows x64 和 ARM64 压缩包，并上传到 GitHub Releases；手动触发时只生成构建产物，不自动发布正式版本。

合并 PR 后可通过标签发布：

```bash
git tag v1.0.0
git push origin v1.0.0
```

## 开源许可

使用 [MIT License](LICENSE) 授权。Copyright © 2019–2026 bestyize。
