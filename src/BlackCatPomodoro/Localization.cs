using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlackCatPomodoro
{
    public enum AppLanguage
    {
        English,
        Chinese
    }

    /// <summary>
    /// Small runtime string table. The default language is Simplified Chinese;
    /// English can be selected from Settings > Language without restarting.
    /// </summary>
    public static class Localization
    {
        public static AppLanguage Current { get; private set; } = AppLanguage.Chinese;

        public static IReadOnlyCollection<string> AllKeys => English.Keys;

        public static void SetLanguage(AppLanguage language)
        {
            Current = language;
        }

        public static AppLanguage ParseLanguage(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? AppLanguage.Chinese
                : AppLanguage.English;
        }

        public static string T(string key)
        {
            return T(key, Current);
        }

        public static string T(string key, AppLanguage language)
        {
            var table = language == AppLanguage.Chinese ? Chinese : English;
            if (table.TryGetValue(key, out string value)) return value;
            if (English.TryGetValue(key, out value)) return value;
            return key;
        }

        public static bool HasKey(string key, AppLanguage language)
        {
            var table = language == AppLanguage.Chinese ? Chinese : English;
            return table.ContainsKey(key);
        }

        private static readonly string[] DefaultSampleNames =
        {
            "Sample: Learn programming",
            "示例：学习编程",
            "[示例] 学习编程"
        };

        private static readonly string[] DefaultSampleNotes =
        {
            "This is a sample task. You can edit or delete it.",
            "这是一个示例待办，可以删除或修改。",
            "这是一个示例待办，可以删除或修改"
        };

        /// <summary>
        /// True only when the task still looks exactly like an app-provided
        /// sample. User-edited tasks are never rewritten by a language change.
        /// </summary>
        public static bool IsDefaultSample(PomodoroTask task)
        {
            if (task == null) return false;

            return Array.IndexOf(DefaultSampleNames, task.Name) >= 0 &&
                   Array.IndexOf(DefaultSampleNotes, task.Notes) >= 0;
        }

        public static string T(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, T(key), args);
        }

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["AppTitle"] = "Black Cat Pomodoro",
            ["AlreadyRunning"] = "Black Cat Pomodoro is already running.",
            ["MenuSettings"] = "&Settings",
            ["MenuTheme"] = "&Theme",
            ["ThemeDefault"] = "System default",
            ["ThemeDark"] = "Dark",
            ["MenuFont"] = "Interface font...",
            ["MenuTimerFont"] = "Timer font...",
            ["MenuFontReset"] = "Reset fonts",
            ["MenuLanguage"] = "&Language",
            ["LanguageEnglish"] = "English",
            ["LanguageChinese"] = "简体中文",
            ["MenuSound"] = "&Sound",
            ["SoundBeep"] = "System sound",
            ["SoundCustom"] = "Custom audio file...",
            ["SoundSilent"] = "Silent",
            ["SoundDevice"] = "Playback device...",
            ["MenuNotify"] = "&Notification",
            ["NotifyBalloon"] = "Tray balloon",
            ["NotifyToast"] = "In-app toast",
            ["MenuTest"] = "&Test",
            ["TestToast"] = "Test toast",
            ["TestBalloon"] = "Test tray balloon",
            ["TestSound"] = "Test sound",
            ["TestCompletion"] = "Test completion",
            ["MenuReset"] = "Reset all settings",
            ["MenuHelp"] = "&Help",
            ["MenuAbout"] = "&About",
            ["GroupTask"] = "Task",
            ["ButtonRefresh"] = "Refresh",
            ["ButtonNew"] = "New",
            ["ButtonEdit"] = "Edit",
            ["ButtonDelete"] = "Delete",
            ["GroupInfo"] = "Task details",
            ["LabelFocusEmpty"] = "Focus: --",
            ["LabelRestEmpty"] = "Rest: --",
            ["LabelRoundsEmpty"] = "Rounds: --",
            ["LabelNotesEmpty"] = "Notes: --",
            ["StatusReady"] = "Ready",
            ["StatusCompleted"] = "Completed",
            ["StatusFocusing"] = "[>] Focusing",
            ["StatusFocusingPaused"] = "[||] Focusing - paused",
            ["StatusResting"] = "[.] Resting",
            ["StatusRestingPaused"] = "[||] Resting - paused",
            ["RoundFormat"] = "Round {0}/{1}  |  Task: {2}",
            ["ButtonStart"] = "Start",
            ["ButtonPause"] = "Pause",
            ["ButtonResume"] = "Resume",
            ["ButtonSkip"] = "Skip",
            ["ButtonStop"] = "Stop",
            ["TrayHint"] = "Tip: closing the window minimizes it to the notification area",
            ["TrayShow"] = "Show window",
            ["TraySettings"] = "Settings",
            ["TrayAbout"] = "About",
            ["TrayExit"] = "Exit",
            ["TaskInfoFocusFormat"] = "Focus: {0} min",
            ["TaskInfoRestFormat"] = "Rest: {0} min",
            ["TaskInfoRoundsFormat"] = "Rounds: {0}",
            ["TaskInfoNotesFormat"] = "Notes: {0}",
            ["TrayBalloonTitle"] = "Black Cat Pomodoro",
            ["TrayBalloonText"] = "The app is still running in the notification area.",
            ["CompleteTitle"] = "Pomodoro complete",
            ["CompleteMessageFormat"] = "\"{0}\" completed {1} rounds.",
            ["TestToastTitle"] = "Test toast",
            ["TestToastMessage"] = "In-app toast notifications are working.",
            ["TestBalloonTitle"] = "Test notification",
            ["TestBalloonMessage"] = "Tray balloon notifications are working.",
            ["TestCompletionTitle"] = "Test completion",
            ["TestCompletionMessage"] = "Completion sound and notification are working.",
            ["AboutTitle"] = "About",
            ["AboutText"] = "Black Cat Pomodoro {0}\n\nA small Windows Pomodoro timer with task management, tray support, themes, and optional audio cues.\n\nhttps://github.com/Fin1995/black-cat-pomodoro",
            ["MsgTitle"] = "Black Cat Pomodoro",
            ["DataLoadWarningFormat"] = "The task file could not be read.\n\nA copy of the unreadable file was saved to:\n{0}\n\nDetails: {1}",
            ["BackupFailed"] = "(backup could not be created)",
            ["DataWarningTitle"] = "Task data warning",
            ["SaveFailedFormat"] = "Failed to save tasks.\n\n{0}",
            ["SaveFailedTitle"] = "Save failed",
            ["SelectTaskFirst"] = "Select a task first.",
            ["TaskRunningDelete"] = "This task is running. Stop the timer first.",
            ["DeleteConfirmFormat"] = "Delete \"{0}\"?",
            ["ConfirmTitle"] = "Confirm",
            ["StartFailedFormat"] = "Could not start the timer:\n{0}",
            ["StopConfirm"] = "Stop the current Pomodoro?",
            ["ExitConfirm"] = "The timer is still running. Exit anyway?",
            ["ExitTitle"] = "Exit",
            ["NoAudioDevices"] = "No audio output devices were detected. The system default will be used.",
            ["SelectDeviceTitle"] = "Select playback device",
            ["SystemDefaultDevice"] = "System default",
            ["Ok"] = "OK",
            ["SelectSoundTitle"] = "Select sound",
            ["SoundFilter"] = "Audio files|*.wav;*.mp3|WAV|*.wav|MP3|*.mp3|All files|*.*",
            ["ResetConfirm"] = "Reset all settings?\n\nThis clears the theme, fonts, sound, notification, and playback device settings.",
            ["ResetTitle"] = "Confirm reset",
            ["NewTaskTitle"] = "New task",
            ["EditTaskTitle"] = "Edit task",
            ["TaskNameLabel"] = "Task name",
            ["FocusGroup"] = "Focus time",
            ["Focus15"] = "15 minutes",
            ["Focus25"] = "25 minutes",
            ["Custom"] = "Custom",
            ["MinutesUnit"] = "min",
            ["RestGroup"] = "Rest time",
            ["Rest5"] = "5 minutes",
            ["AdvancedGroup"] = "Advanced",
            ["RoundsLabel"] = "Rounds",
            ["TimesUnit"] = "times",
            ["NotesGroup"] = "Notes",
            ["RequiredHint"] = "* Name and focus time are required",
            ["ButtonAdd"] = "Add",
            ["ButtonSave"] = "Save",
            ["ButtonCancel"] = "Cancel",
            ["CharCountFormat"] = "{0}/{1}",
            ["NameEmpty"] = "Task name cannot be empty.",
            ["NameTooLong"] = "Task name must be 20 characters or fewer.",
            ["FocusRange"] = "Focus time must be between 1 and 180 minutes.",
            ["RestRange"] = "Rest time must be between 0 and 60 minutes.",
            ["RoundsRange"] = "Rounds must be between 1 and 20.",
            ["NotesTooLong"] = "Notes must be 200 characters or fewer.",
            ["NoticeTitle"] = "Notice",
            ["SampleTaskName"] = "Sample: Learn programming",
            ["SampleTaskNotes"] = "This is a sample task. You can edit or delete it."
        };

        private static readonly Dictionary<string, string> Chinese = new Dictionary<string, string>
        {
            ["AppTitle"] = "黑猫番茄钟",
            ["AlreadyRunning"] = "黑猫番茄钟已经在运行。",
            ["MenuSettings"] = "设置(&S)",
            ["MenuTheme"] = "主题(&T)",
            ["ThemeDefault"] = "系统默认",
            ["ThemeDark"] = "暗色模式",
            ["MenuFont"] = "界面字体...",
            ["MenuTimerFont"] = "计时字体...",
            ["MenuFontReset"] = "重置字体",
            ["MenuLanguage"] = "语言(&L)",
            ["LanguageEnglish"] = "English",
            ["LanguageChinese"] = "简体中文",
            ["MenuSound"] = "提示音(&N)",
            ["SoundBeep"] = "系统提示音",
            ["SoundCustom"] = "自定义音频文件...",
            ["SoundSilent"] = "静音",
            ["SoundDevice"] = "播放设备(&D)...",
            ["MenuNotify"] = "通知样式(&O)",
            ["NotifyBalloon"] = "系统托盘气泡",
            ["NotifyToast"] = "软件内置弹窗",
            ["MenuTest"] = "测试(&T)",
            ["TestToast"] = "测试 Toast 通知",
            ["TestBalloon"] = "测试托盘气泡",
            ["TestSound"] = "测试提示音",
            ["TestCompletion"] = "测试完成提醒",
            ["MenuReset"] = "重置所有设置",
            ["MenuHelp"] = "帮助(&H)",
            ["MenuAbout"] = "关于(&A)",
            ["GroupTask"] = "选择待办",
            ["ButtonRefresh"] = "刷新",
            ["ButtonNew"] = "新增",
            ["ButtonEdit"] = "编辑",
            ["ButtonDelete"] = "删除",
            ["GroupInfo"] = "待办信息",
            ["LabelFocusEmpty"] = "专注时间: --",
            ["LabelRestEmpty"] = "休息时间: --",
            ["LabelRoundsEmpty"] = "执行: --",
            ["LabelNotesEmpty"] = "备注: --",
            ["StatusReady"] = "就绪",
            ["StatusCompleted"] = "已完成",
            ["StatusFocusing"] = "[>] 专注中",
            ["StatusFocusingPaused"] = "[||] 专注 - 已暂停",
            ["StatusResting"] = "[.] 休息中",
            ["StatusRestingPaused"] = "[||] 休息 - 已暂停",
            ["RoundFormat"] = "第 {0}/{1} 轮  |  任务: {2}",
            ["ButtonStart"] = "开始专注",
            ["ButtonPause"] = "暂停",
            ["ButtonResume"] = "继续",
            ["ButtonSkip"] = "跳过",
            ["ButtonStop"] = "停止",
            ["TrayHint"] = "提示: 关闭窗口将最小化到系统托盘",
            ["TrayShow"] = "显示主窗口",
            ["TraySettings"] = "设置",
            ["TrayAbout"] = "关于",
            ["TrayExit"] = "退出",
            ["TaskInfoFocusFormat"] = "专注时间: {0} 分钟",
            ["TaskInfoRestFormat"] = "休息时间: {0} 分钟",
            ["TaskInfoRoundsFormat"] = "执行: {0} 次",
            ["TaskInfoNotesFormat"] = "备注: {0}",
            ["TrayBalloonTitle"] = "黑猫番茄钟",
            ["TrayBalloonText"] = "程序仍在通知区域运行。",
            ["CompleteTitle"] = "番茄钟完成",
            ["CompleteMessageFormat"] = "\"{0}\" {1} 轮完成。",
            ["TestToastTitle"] = "测试通知",
            ["TestToastMessage"] = "软件内置 Toast 通知正常。",
            ["TestBalloonTitle"] = "测试通知",
            ["TestBalloonMessage"] = "系统托盘气泡正常。",
            ["TestCompletionTitle"] = "测试完成提醒",
            ["TestCompletionMessage"] = "完成提示音和通知正常。",
            ["AboutTitle"] = "关于",
            ["AboutText"] = "黑猫番茄钟 {0}\n\n一个轻量的 Windows 桌面番茄钟，支持任务管理、托盘运行、主题和提示音。\n\nhttps://github.com/Fin1995/black-cat-pomodoro",
            ["MsgTitle"] = "黑猫番茄钟",
            ["DataLoadWarningFormat"] = "任务文件无法读取。\n\n原文件已备份到：\n{0}\n\n详细信息：{1}",
            ["BackupFailed"] = "（备份文件创建失败）",
            ["DataWarningTitle"] = "任务数据警告",
            ["SaveFailedFormat"] = "保存任务失败。\n\n{0}",
            ["SaveFailedTitle"] = "保存失败",
            ["SelectTaskFirst"] = "请先选择一个待办事项。",
            ["TaskRunningDelete"] = "该待办正在运行中，请先停止计时。",
            ["DeleteConfirmFormat"] = "确定删除 \"{0}\"？",
            ["ConfirmTitle"] = "确认",
            ["StartFailedFormat"] = "无法开始计时：\n{0}",
            ["StopConfirm"] = "确定停止当前番茄钟？",
            ["ExitConfirm"] = "番茄钟仍在运行，确定退出？",
            ["ExitTitle"] = "退出",
            ["NoAudioDevices"] = "未检测到音频输出设备，将使用系统默认设备。",
            ["SelectDeviceTitle"] = "选择播放设备",
            ["SystemDefaultDevice"] = "系统默认设备",
            ["Ok"] = "确定",
            ["SelectSoundTitle"] = "选择提示音",
            ["SoundFilter"] = "音频文件|*.wav;*.mp3|WAV|*.wav|MP3|*.mp3|所有文件|*.*",
            ["ResetConfirm"] = "确定重置所有设置吗？\n\n这将清除主题、字体、提示音、通知和播放设备设置。",
            ["ResetTitle"] = "确认重置",
            ["NewTaskTitle"] = "新增待办",
            ["EditTaskTitle"] = "编辑待办",
            ["TaskNameLabel"] = "事件名称",
            ["FocusGroup"] = "专注时间",
            ["Focus15"] = "15 分钟",
            ["Focus25"] = "25 分钟",
            ["Custom"] = "自定义",
            ["MinutesUnit"] = "分",
            ["RestGroup"] = "休息时间",
            ["Rest5"] = "5 分钟",
            ["AdvancedGroup"] = "高级设置",
            ["RoundsLabel"] = "执行次数",
            ["TimesUnit"] = "次",
            ["NotesGroup"] = "任务备注",
            ["RequiredHint"] = "* 名称和专注时间为必填项",
            ["ButtonAdd"] = "新增",
            ["ButtonSave"] = "保存",
            ["ButtonCancel"] = "取消",
            ["CharCountFormat"] = "{0}/{1}",
            ["NameEmpty"] = "事件名称不能为空！",
            ["NameTooLong"] = "事件名称最多 20 个字！",
            ["FocusRange"] = "专注时间需在 1-180 分钟之间。",
            ["RestRange"] = "休息时间需在 0-60 分钟之间。",
            ["RoundsRange"] = "执行次数需在 1-20 次之间。",
            ["NotesTooLong"] = "备注最多 200 个字符。",
            ["NoticeTitle"] = "提示",
            ["SampleTaskName"] = "示例：学习编程",
            ["SampleTaskNotes"] = "这是一个示例待办，可以删除或修改。"
        };
    }
}
