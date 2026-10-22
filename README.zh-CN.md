# PictureFetcher

> **嵌入式图形工作室（第二阶段）**：支持 SSD1306 128×64 / 128×32 及 SH1106 128×64，并内置可处理控制器指令与数据流的虚拟 OLED 显示器。可切换青蓝、琥珀和白色显示色，以及电源与显示反色状态。字节数组大小随所选规格自动变化（1024 / 512 字节）。
>
> 模拟范围包括 SSD1306 页/水平/垂直寻址、SH1106 的 132 列物理显存与 2 列默认偏移、部分控制指令及显示状态；**不包括** I2C/SPI 电气层、MCU 运行或完整滚屏/模拟电路。后续控制器、彩屏及动画规划见 [架构与路线图](docs/EMBEDDED_STUDIO.md)。

[English](README.md) · **简体中文**

PictureFetcher 是面向嵌入式开发的跨平台**图形资源工作室**，提供图像取模、控制器数据导出与 OLED 虚拟屏幕，使用 .NET 10 + Avalonia，界面采用简洁的 Mac 风格。

## 安装包瘦身

通过 .NET **部分 IL 裁剪（TrimMode=partial）**，在仍然自包含、用户无需安装 .NET 运行时的前提下，四平台匹配构建的压缩输出减少约 **61%～66%**。保留中英文资源及 Avalonia 动态 UI；Linux 版已进行真实窗口启动与 SSD1306/SH1106、数据导出等功能冒烟测试。Windows ZIP、macOS DMG 最终体积与对照测试的 tar.gz 会有所不同，详见[测试与打包说明](docs/PACKAGING.md)。

## v2.1.1 正式版本

本次整合 OLED 模拟屏幕、SSD1306 命令流模拟、SH1106 的 132 列显存与 Controller TX 预览。Windows 和 Linux 将自动生成公开安装包；macOS DMG 只有完成 Developer ID 签名与苹果公证后才纳入公开 Release，否则仅保留为 CI 构建产物，避免“应用已损坏”提示误导用户。

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
- 打开或拖入图片，可选择 SSD1306 128×64 / 128×32、SH1106 128×64，并查看原图与模拟屏幕。
- 阈值调整、黑白取反。
- 提供 HEX、C 数组、Controller TX（指令与数据流）三种格式：SSD1306 水平寻址；SH1106 按页写入并处理显存列偏移。
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

CI 在四个架构上构建发布程序并执行核心回归测试，同时验证 ZIP、tar.gz、DMG 包能生成。同步更新项目版本号和仓库根目录 `VERSION`，合并到 `master` 后自动创建 `vX.Y.Z` 标签并发布 Windows/Linux 安装包及 SHA-256 校验文件；macOS DMG 只有验证通过 Apple 公证后才公开发布。

安装包采用单文件压缩、自包含但不混入其他架构运行时、移除调试符号、关闭 ReadyToRun 以降低体积。目前不启用不经验证的 IL Trimming，避免 Avalonia XAML 反射内容被错误删除导致软件打不开。详见[打包说明](docs/PACKAGING.md)。

## 开源许可

[MIT License](LICENSE) · Copyright © 2019–2026 bestyize。
