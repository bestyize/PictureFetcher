# PictureFetcher

[English](README.md) · **简体中文**

PictureFetcher 是专为 **128 × 64 单色 OLED 屏幕**开发的跨平台图像取模工具，采用 .NET 10 + Avalonia，界面采用简洁的 Mac 风格。

## 下载安装

请在 [GitHub Releases](https://github.com/bestyize/PictureFetcher/releases) 下载对应平台的安装包。

| 系统 | 发布包 | 使用方式 |
| --- | --- | --- |
| Windows x64 | `PictureFetcher-vX.Y.Z-win-x64.zip` | 解压后运行 `PictureFetcher.exe` |
| Linux x64 | `PictureFetcher-vX.Y.Z-linux-x64.tar.gz` | 解压后运行 `./PictureFetcher` |
| macOS Apple Silicon | `PictureFetcher-vX.Y.Z-osx-arm64.dmg` | 打开 DMG，将 PictureFetcher 拖入应用程序文件夹 |
| macOS Intel | `PictureFetcher-vX.Y.Z-osx-x64.dmg` | 打开 DMG，将 PictureFetcher 拖入应用程序文件夹 |

四平台采用自包含发布，**无需另装 .NET 10 运行时**。Linux 需要桌面图形环境及常见 X11/Wayland、字体等系统依赖。

**macOS 签名提醒：**当前公开构建为 *ad-hoc（临时）签名*，**不等于 Apple Developer ID 签名或苹果公证**，从网络下载后可能被 Gatekeeper 拦截。要做到普通用户双击即可顺利安装，需要开发者证书和公证。详见[打包说明](docs/PACKAGING.md)。

## 功能

- Mac 风格跨平台桌面 UI，支持中英文切换并记忆选择。
- 打开或拖入图片，同时显示原图和 128 × 64 转换预览。
- 阈值调整、黑白取反。
- SSD1306 页序 1024 字节数据，支持十六进制文本、C 数组和一键复制。
- 单张导出 BMP、批量转换。

**兼容性说明：**保留老版本的阈值极性：默认当 R+G+B 大于阈值时为黑色，开启取反则相反。字节以每页 128 字节、纵向最低位优先编码。

**安全输出：**批量生成的 BMP 会使用 `-oled.bmp` 后缀；如已存在同名文件则自动增加数字后缀，不覆盖源文件或之前导出的图片。

## 源码构建

先安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。

```bash
dotnet restore PictureFetcher.sln
dotnet build PictureFetcher.sln -c Release
dotnet run --project PictureFetcher.csproj
dotnet run --project tests/PictureFetcher.Core.Tests/PictureFetcher.Core.Tests.csproj
```

发布特定架构的自包含单文件程序：

```bash
dotnet publish PictureFetcher.csproj -c Release -r osx-arm64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=false \
  -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false
```

可将 RID 改为 `win-x64`、`linux-x64`、`osx-arm64` 或 `osx-x64`；正式发布建议在对应系统的 GitHub Actions Runner 上打包。

## 自动化发布和瘦身策略

CI 在四个架构上构建发布程序并执行核心回归测试，同时验证 ZIP、tar.gz、DMG 包能生成。合并后推送 `vX.Y.Z` 标签，即可发布四种包和 `SHA256SUMS.txt`。

安装包采用单文件压缩、自包含但不混入其他架构运行时、移除调试符号、关闭 ReadyToRun 以降低体积。目前不启用不经验证的 IL Trimming，避免 Avalonia XAML 反射内容被错误删除导致软件打不开。详见[打包说明](docs/PACKAGING.md)。

## 开源许可

[MIT License](LICENSE) · Copyright © 2019–2026 bestyize。
