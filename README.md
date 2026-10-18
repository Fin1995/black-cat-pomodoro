# 黑猫番茄钟

[简体中文](README.md) | [English](README.en.md)

[![build](https://github.com/Fin1995/black-cat-pomodoro/actions/workflows/build.yml/badge.svg)](https://github.com/Fin1995/black-cat-pomodoro/actions/workflows/build.yml)

一个使用 C# 和 WinForms（.NET Framework 4.7.2）编写的 Windows 桌面番茄钟。它把专注/休息计时、任务管理、托盘运行、主题和提示音整合在一个小工具里。

![主窗口](docs/images/main-window-zh.png)

## 功能

| 功能 | 说明 |
|---|---|
| 番茄钟 | 专注与休息双阶段，可自定义分钟数和循环次数 |
| 任务管理 | 新增、编辑、删除任务，支持备注 |
| 托盘运行 | 关闭窗口后计时器继续在通知区域运行 |
| 通知 | 系统托盘气泡或软件内置 Toast |
| 提示音 | 系统提示音、自定义 WAV/MP3，或静音 |
| 主题 | 默认主题和暗色主题 |
| 本地数据 | XML 保存在 `%LOCALAPPDATA%`，原子写入并自动备份 |
| 单实例 | 同一时间只允许运行一个实例 |
| 高 DPI | PerMonitorV2 清单和 DPI 感知布局 |
| 字体 | 界面字体和计时字体可分别设置，并支持重置 |
| 语言 | 默认简体中文，运行时切换英文 |
| 内置测试 | 测试 Toast、托盘气泡、提示音和完成提醒 |

## 截图

截图使用默认主题，分别展示英文和简体中文界面。

| 英文 | 简体中文 |
|---|---|
| ![主窗口英文](docs/images/main-window.png) | ![主窗口中文](docs/images/main-window-zh.png) |

## 运行环境

- Windows 10 或 Windows 11（x64）
- .NET Framework 4.7.2 或更高版本（Windows 10/11 自带 4.8）
- 构建需要 .NET SDK 8.0+ 或 Visual Studio 2022

## 下载

从 [GitHub Releases](https://github.com/Fin1995/black-cat-pomodoro/releases) 下载最新二进制包。

发布包包含程序和依赖，不需要安装器。

## 构建

```powershell
dotnet build BlackCatPomodoro.sln -c Release
dotnet test BlackCatPomodoro.sln -c Release
```

生成位置：

```text
src/BlackCatPomodoro/bin/Release/net472/BlackCatPomodoro.exe
```

也可以用 Visual Studio 2022 打开 `BlackCatPomodoro.sln`。

## 使用

1. 点击“新增”创建任务。
2. 选择任务后点击“开始专注”。
3. 根据需要点击“暂停”“跳过”或“停止”。
4. 关闭窗口会把程序隐藏到通知区域；通过托盘菜单恢复或退出。
5. 在“设置”中修改主题、界面字体、计时字体、语言、提示音、通知方式，或运行内置测试。

## 数据与隐私

- 任务文件：`%LOCALAPPDATA%\BlackCatPomodoro\tasks.xml`
- 备份文件：`%LOCALAPPDATA%\BlackCatPomodoro\tasks.xml.bak`
- 文件损坏时会先复制为 `tasks.corrupt-YYYYMMDD-HHMMSS.xml`，再继续运行。
- 程序不联网、不上报遥测、不做云同步。

## 项目结构

```text
BlackCatPomodoro.sln
src/BlackCatPomodoro/
  Program.cs                 程序入口和单实例保护
  MainForm.cs                主窗口、托盘、偏好设置
  PomodoroService.cs         基于墙钟的番茄钟状态机
  DataService.cs             原子 XML 持久化
  AudioService.cs            NAudio 播放，失败时回退系统提示音
  ThemeService.cs            默认主题和暗色主题
  Localization.cs            英文/中文字符串表
  FontHelper.cs              递归应用字体
  TaskEditForm.cs            任务编辑窗口
  ToastForm.cs               软件内通知
  Resources/app.ico          程序图标
  app.manifest               DPI 感知和系统兼容性
tests/BlackCatPomodoro.Tests/
  PomodoroServiceTests.cs
  DataServiceTests.cs
  LocalizationTests.cs
docs/images/                 README 截图
```

## 设计说明

- `PomodoroService` 自己不持有计时器。界面每 250 ms 调用一次 `Update(DateTime.UtcNow)`，服务用当前时间和阶段截止时间计算剩余秒数。这样即使界面卡顿或机器休眠，计时也不会漂移。
- `DataService` 先写入临时文件，再用 `File.Replace` 替换正式文件，并保留 `tasks.xml.bak` 作为上一版备份。
- `AudioService` 使用 NAudio 播放 WAV/MP3；如果运行时缺少 NAudio 程序集，会回退到 Windows 系统提示音。
- `FontHelper` 会把界面字体递归应用到窗体、子控件和工具条；倒计时使用独立的计时字体，两个区域可以分别配置。
- `Localization` 是一个运行时字符串表；默认简体中文，切换英文不需要重启。
- “测试”菜单可以快速验证 Toast、托盘气泡、提示音和完成提醒。
- 未处理的界面异常会写入 `%TEMP%\BlackCatPomodoro-crash.log`。
- 程序是单实例的；重复启动只会提示，不会打开第二个计时器。

## 常见问题

| 问题 | 检查项 |
|---|---|
| 没有声音 | 确认 `NAudio.dll` 及其依赖和 exe 在同一目录；否则程序会使用系统提示音。检查 Windows 音量合成器。 |
| 自定义音效变成系统提示音 | 播放器会按文件内容识别格式；有效的 WAV/MP3 都能播放，包括扩展名为 `.wav` 的 MP3。 |
| 窗口位置 | 启动时窗口会居中到鼠标所在的屏幕。 |
| 示例任务语言 | 切换语言时会翻译程序自带的示例任务；用户编辑过的任务不会被改动。 |
| 高 DPI 下界面模糊 | 程序已启用 PerMonitorV2；修改显示缩放后请重启程序。 |
| 任务不见了 | 检查 `%LOCALAPPDATA%\BlackCatPomodoro` 下的 `tasks.xml.bak` 和 `tasks.corrupt-*.xml`。 |
| 程序崩溃 | 检查 `%TEMP%\BlackCatPomodoro-crash.log` 中的异常和堆栈。 |
| 无法启动第二个实例 | 这是有意设计：只允许一个实例运行。 |

## 参与贡献

欢迎提交 Issue 和 Pull Request。提交前请先运行测试：

```powershell
dotnet test BlackCatPomodoro.sln -c Release
```

## 许可证

MIT，见 [LICENSE](LICENSE)。

Copyright (c) 2026 Fin1995。
