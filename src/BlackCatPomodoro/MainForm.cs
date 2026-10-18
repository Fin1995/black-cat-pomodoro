using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BlackCatPomodoro
{
    public partial class MainForm : Form
    {
        private readonly DataService _data = new DataService();
        private readonly PomodoroService _pomodoro = new PomodoroService();
        private readonly AudioService _audio = new AudioService();
        private readonly Timer _uiTimer;

        private List<PomodoroTask> _tasks;
        private PomodoroTask _selectedTask;
        private AppTheme _currentTheme = AppTheme.Default;
        private SoundMode _soundMode = SoundMode.SystemBeep;
        private bool _notifyUseToast;
        private string _customSoundPath;
        private int _audioDeviceIdx = -1;
        private bool _exitRequested;
        private bool _trayHintShown;
        private bool _showCompletion;
        private bool _isRunning;

        private readonly List<Font> _ownedFonts = new List<Font>();
        private Font _uiFont;
        private Font _uiBoldFont;
        private Font _timerFont;

        private static readonly Color CFocus = Color.FromArgb(200, 40, 40);
        private static readonly Color CRest = Color.FromArgb(40, 140, 40);
        private static readonly Color CIdle = Color.DimGray;
        private static readonly Color CPaused = Color.FromArgb(200, 140, 0);

        public MainForm()
        {
            InitializeComponent();

            Localization.SetLanguage(Localization.ParseLanguage(Properties.Settings.Default.Language));

            menuThemeDefault.Checked = true;
            menuSoundBeep.Checked = true;
            menuNotifyBalloon.Checked = true;
            menuLanguageChinese.Checked = true;

            using (var ui = CreateDefaultUiFont())
                ApplyUiFont(ui);
            using (var timer = CreateDefaultTimerFont())
                ApplyTimerFont(timer);

            LoadIcon();

            _pomodoro.Tick += (remaining, total) => UpdateCountdown(remaining);
            _pomodoro.PhaseChanged += OnPhaseChanged;
            _pomodoro.CycleCompleted += OnComplete;

            _uiTimer = new Timer { Interval = 250 };
            _uiTimer.Tick += (s, e) => _pomodoro.Update(DateTime.UtcNow);

            LoadTasks();
            if (LocalizeDefaultSample()) _data.Save(_tasks);
            RefreshTaskList();
            WireAll();
            LoadPrefs();

            _uiTimer.Start();
        }

        // ==================== Icon ====================

        private void LoadIcon()
        {
            try
            {
                using (var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("BlackCatPomodoro.app.ico"))
                {
                    if (stream == null) return;

                    var icon = new Icon(stream);
                    Icon = icon;
                    notifyIcon.Icon = icon;
                }
            }
            catch
            {
                // The default application icon is an acceptable fallback.
            }
        }

        // ==================== Data ====================

        private void LoadTasks()
        {
            var result = _data.Load();
            _tasks = result.Tasks ?? new List<PomodoroTask>();

            if (string.IsNullOrEmpty(result.Error)) return;

            string backup = string.IsNullOrEmpty(result.BackupPath)
                ? Localization.T("BackupFailed")
                : result.BackupPath;

            MessageBox.Show(
                Localization.T("DataLoadWarningFormat", backup, result.Error),
                Localization.T("DataWarningTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void RefreshTaskList()
        {
            var previous = cmbTask.SelectedTask ?? _selectedTask;

            cmbTask.SetTasks(_tasks, previous);
            cmbTask.Enabled = !_isRunning && cmbTask.HasTasks;

            if (cmbTask.SelectedTask != null)
            {
                SelectTask(cmbTask.SelectedTask);
            }
            else
            {
                _selectedTask = null;
                ShowEmptyInfo();
            }
        }

        private void SelectTask(PomodoroTask task)
        {
            _selectedTask = task;

            if (task == null)
            {
                ShowEmptyInfo();
                return;
            }

            lblFocus.Text = Localization.T("TaskInfoFocusFormat", task.FocusMinutes);
            lblRest.Text = Localization.T("TaskInfoRestFormat", task.RestMinutes);
            lblRepeat.Text = Localization.T("TaskInfoRoundsFormat", task.RepeatCount);
            lblNotes.Text = string.IsNullOrEmpty(task.Notes)
                ? Localization.T("LabelNotesEmpty")
                : Localization.T("TaskInfoNotesFormat", task.Notes);
        }

        private void ShowEmptyInfo()
        {
            lblFocus.Text = Localization.T("LabelFocusEmpty");
            lblRest.Text = Localization.T("LabelRestEmpty");
            lblRepeat.Text = Localization.T("LabelRoundsEmpty");
            lblNotes.Text = Localization.T("LabelNotesEmpty");
        }

        private bool LocalizeDefaultSample()
        {
            string name = Localization.T("SampleTaskName");
            string notes = Localization.T("SampleTaskNotes");
            bool changed = false;

            foreach (var task in _tasks)
            {
                if (!Localization.IsDefaultSample(task)) continue;
                if (task.Name == name && task.Notes == notes) continue;

                task.Name = name;
                task.Notes = notes;
                changed = true;
            }

            return changed;
        }

        private void SaveAndRefresh()
        {
            if (!_data.Save(_tasks))
            {
                MessageBox.Show(
                    Localization.T("SaveFailedFormat", _data.LastError),
                    Localization.T("SaveFailedTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            RefreshTaskList();
        }

        private static void Msg(string message)
        {
            MessageBox.Show(
                message,
                Localization.T("MsgTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ==================== Events ====================

        private void WireAll()
        {
            FormClosing += (s, e) =>
            {
                if (_exitRequested) return;
                if (e.CloseReason != CloseReason.UserClosing) return;

                e.Cancel = true;
                HideToTray();
            };

            cmbTask.SelectedTaskChanged += (s, e) =>
            {
                if (cmbTask.SelectedTask != null) SelectTask(cmbTask.SelectedTask);
            };

            btnRefresh.Click += (s, e) => RefreshTaskList();
            btnAdd.Click += (s, e) =>
            {
                using (var dialog = new TaskEditForm(null, _uiFont))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Result != null)
                    {
                        _tasks.Add(dialog.Result);
                        SaveAndRefresh();
                    }
                }
            };
            btnEdit.Click += (s, e) =>
            {
                if (_selectedTask == null)
                {
                    Msg(Localization.T("SelectTaskFirst"));
                    return;
                }

                using (var dialog = new TaskEditForm(_selectedTask, _uiFont))
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK) SaveAndRefresh();
                }
            };
            btnDelete.Click += (s, e) =>
            {
                if (_selectedTask == null)
                {
                    Msg(Localization.T("SelectTaskFirst"));
                    return;
                }

                if (_pomodoro.IsRunning && _pomodoro.CurrentTask?.Id == _selectedTask.Id)
                {
                    Msg(Localization.T("TaskRunningDelete"));
                    return;
                }

                if (MessageBox.Show(
                        Localization.T("DeleteConfirmFormat", _selectedTask.Name),
                        Localization.T("ConfirmTitle"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                _tasks.Remove(_selectedTask);
                _selectedTask = null;
                SaveAndRefresh();
            };

            btnStart.Click += (s, e) => StartPomodoro();
            btnPause.Click += (s, e) => TogglePause();
            btnSkip.Click += (s, e) => SkipPhase();
            btnStop.Click += (s, e) => StopPomodoro();

            notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) ShowFromTray();
            };
            trayShow.Click += (s, e) => ShowFromTray();
            trayStart.Click += (s, e) => StartPomodoro();
            trayPause.Click += (s, e) => TogglePause();
            traySkip.Click += (s, e) => SkipPhase();
            trayStop.Click += (s, e) => StopPomodoro();
            traySettings.Click += (s, e) =>
            {
                ShowFromTray();
                menuSettings.ShowDropDown();
            };
            trayAbout.Click += (s, e) => ShowAbout();
            trayExit.Click += (s, e) => ExitApp();

            menuThemeDefault.Click += (s, e) => SwitchTheme(AppTheme.Default);
            menuThemeDark.Click += (s, e) => SwitchTheme(AppTheme.Dark);
            menuFont.Click += (s, e) => ChangeUiFont();
            menuTimerFont.Click += (s, e) => ChangeTimerFont();
            menuFontReset.Click += (s, e) => ResetFonts();
            menuLanguageEnglish.Click += (s, e) => SetLanguage(AppLanguage.English);
            menuLanguageChinese.Click += (s, e) => SetLanguage(AppLanguage.Chinese);
            menuSoundBeep.Click += (s, e) => SetSound(SoundMode.SystemBeep);
            menuSoundCustom.Click += (s, e) => SetSoundCustom();
            menuSoundSilent.Click += (s, e) => SetSound(SoundMode.Silent);
            menuSoundDevice.Click += (s, e) => ChooseDevice();
            menuNotifyBalloon.Click += (s, e) => SetNotify(false);
            menuNotifyToast.Click += (s, e) => SetNotify(true);
            menuTestToast.Click += (s, e) =>
                ShowToast(Localization.T("TestToastTitle"), Localization.T("TestToastMessage"));
            menuTestBalloon.Click += (s, e) =>
                ShowBalloon(Localization.T("TestBalloonTitle"), Localization.T("TestBalloonMessage"));
            menuTestSound.Click += (s, e) => PlayCompletionSound();
            menuTestCompletion.Click += (s, e) =>
            {
                PlayCompletionSound();
                ShowNotification(
                    Localization.T("TestCompletionTitle"),
                    Localization.T("TestCompletionMessage"));
            };
            menuReset.Click += (s, e) => ResetAllSettings();
            menuAbout.Click += (s, e) => ShowAbout();
        }

        // ==================== Tray ====================

        private void HideToTray()
        {
            Hide();
            notifyIcon.Visible = true;

            if (_trayHintShown) return;

            notifyIcon.ShowBalloonTip(
                2000,
                Localization.T("TrayBalloonTitle"),
                Localization.T("TrayBalloonText"),
                ToolTipIcon.Info);
            _trayHintShown = true;
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            notifyIcon.Visible = false;
        }

        private void ExitApp()
        {
            if (_pomodoro.IsRunning &&
                MessageBox.Show(
                    Localization.T("ExitConfirm"),
                    Localization.T("ExitTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _exitRequested = true;
            Close();
        }

        // ==================== Pomodoro ====================

        private void StartPomodoro()
        {
            if (_selectedTask == null)
            {
                Msg(Localization.T("SelectTaskFirst"));
                return;
            }

            try
            {
                _showCompletion = false;
                _pomodoro.Start(_selectedTask, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Msg(Localization.T("StartFailedFormat", ex.Message));
                return;
            }

            SetRunning(true);
            UpdateTimerUI();
            UpdateTrayState();
        }

        private void TogglePause()
        {
            _pomodoro.TogglePause(DateTime.UtcNow);
            UpdateTimerUI();
            UpdateTrayState();
        }

        private void SkipPhase()
        {
            _pomodoro.Skip(DateTime.UtcNow);
            UpdateTimerUI();
        }

        private void StopPomodoro()
        {
            if (!_pomodoro.IsRunning) return;

            if (MessageBox.Show(
                    Localization.T("StopConfirm"),
                    Localization.T("ConfirmTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _pomodoro.Stop();
            _showCompletion = false;
            SetRunning(false);
            ResetTimerUI();
            UpdateTrayState();
        }

        private void OnPhaseChanged()
        {
            UpdateTimerUI();
            UpdateTrayState();
        }

        private void OnComplete()
        {
            _showCompletion = true;
            PlayCompletionSound();

            SetRunning(false);
            lblCountdown.Text = "00:00";
            lblCountdown.ForeColor = CIdle;
            lblStatus.Text = Localization.T("StatusCompleted");
            lblStatus.ForeColor = CRest;
            lblRound.Text = Localization.T(
                "CompleteMessageFormat",
                _selectedTask?.Name ?? string.Empty,
                _pomodoro.TotalRounds);

            ShowNotification(
                Localization.T("CompleteTitle"),
                Localization.T("CompleteMessageFormat",
                    _selectedTask?.Name ?? string.Empty,
                    _pomodoro.TotalRounds));

            UpdateTrayState();
        }

        private void PlayCompletionSound()
        {
            if (_soundMode == SoundMode.Silent) return;
            if (_soundMode == SoundMode.SystemBeep)
            {
                AudioService.PlaySystemBeep();
                return;
            }

            PlayCustomSound();
        }

        private void PlayCustomSound()
        {
            try
            {
                if (!string.IsNullOrEmpty(_customSoundPath) && File.Exists(_customSoundPath))
                    _audio.Play(_customSoundPath, _audioDeviceIdx);
                else
                    AudioService.PlaySystemBeep();
            }
            catch
            {
                AudioService.PlaySystemBeep();
            }
        }

        private void UpdateTrayState()
        {
            bool running = _pomodoro.IsRunning;
            bool paused = _pomodoro.IsPaused;

            trayStart.Enabled = !running;
            trayPause.Enabled = running;
            traySkip.Enabled = running;
            trayStop.Enabled = running;
            trayPause.Text = paused
                ? Localization.T("ButtonResume")
                : Localization.T("ButtonPause");
        }

        // ==================== Notifications ====================

        private void ShowNotification(string title, string message)
        {
            if (_notifyUseToast) ShowToast(title, message);
            else ShowBalloon(title, message);
        }

        private void ShowToast(string title, string message)
        {
            try
            {
                new ToastForm(title, message).Pop();
            }
            catch
            {
                ShowBalloon(title, message);
            }
        }

        private void ShowBalloon(string title, string message)
        {
            try
            {
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
            }
            catch
            {
                // Notifications are best-effort.
            }
        }

        // ==================== Sound ====================

        private void SetSound(SoundMode mode)
        {
            _soundMode = mode;
            menuSoundBeep.Checked = mode == SoundMode.SystemBeep;
            menuSoundCustom.Checked = mode == SoundMode.CustomFile;
            menuSoundSilent.Checked = mode == SoundMode.Silent;

            if (mode == SoundMode.Silent) _audio.Stop();

            SaveAllPrefs();
        }

        private void SetSoundCustom()
        {
            using (var dialog = new OpenFileDialog
            {
                Filter = Localization.T("SoundFilter"),
                Title = Localization.T("SelectSoundTitle")
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                _customSoundPath = dialog.FileName;
                SetSound(SoundMode.CustomFile);
            }
        }

        private void ChooseDevice()
        {
            var devices = AudioService.GetDevices();
            if (devices.Count == 0)
            {
                Msg(Localization.T("NoAudioDevices"));
                _audioDeviceIdx = -1;
                SaveAllPrefs();
                return;
            }

            var items = new List<string> { Localization.T("SystemDefaultDevice") };
            items.AddRange(devices.Select(d => d.Name));

            using (var dialog = new Form
            {
                Text = Localization.T("SelectDeviceTitle"),
                ClientSize = new Size(380, 110),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent
            })
            {
                var combo = new DarkComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(12, 12),
                    Size = new Size(356, 25)
                };
                combo.Items.AddRange(items.ToArray());
                combo.SelectedIndex = (_audioDeviceIdx < 0 || _audioDeviceIdx >= devices.Count)
                    ? 0
                    : _audioDeviceIdx + 1;

                var button = new Button
                {
                    Text = Localization.T("Ok"),
                    Location = new Point(290, 48),
                    Size = new Size(75, 28)
                };
                button.Click += (s, e) =>
                {
                    _audioDeviceIdx = combo.SelectedIndex - 1;
                    SaveAllPrefs();
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };

                dialog.Controls.Add(combo);
                dialog.Controls.Add(button);
                ThemeService.Apply(dialog, _currentTheme);
                ThemeService.ApplyTitleBar(dialog, _currentTheme);
                dialog.ShowDialog(this);
            }
        }

        private void SetNotify(bool useToast)
        {
            _notifyUseToast = useToast;
            menuNotifyBalloon.Checked = !useToast;
            menuNotifyToast.Checked = useToast;
            SaveAllPrefs();
        }

        // ==================== Timer UI ====================

        private void UpdateCountdown(int seconds)
        {
            if (seconds < 0) seconds = 0;
            lblCountdown.Text = $"{seconds / 60:D2}:{seconds % 60:D2}";
        }

        private void UpdateTimerUI()
        {
            if (_pomodoro.Phase == PomodoroPhase.Idle)
            {
                lblCountdown.Text = "00:00";
                lblCountdown.ForeColor = CIdle;
                lblCountdown.Font = _timerFont;

                if (_showCompletion)
                {
                    lblStatus.Text = Localization.T("StatusCompleted");
                    lblStatus.ForeColor = CRest;
                }
                else
                {
                    lblStatus.Text = Localization.T("StatusReady");
                    lblStatus.ForeColor = CIdle;
                    lblRound.Text = string.Empty;
                }

                btnPause.Text = Localization.T("ButtonPause");
                return;
            }

            if (_pomodoro.Phase == PomodoroPhase.Focusing)
            {
                lblStatus.Text = _pomodoro.IsPaused
                    ? Localization.T("StatusFocusingPaused")
                    : Localization.T("StatusFocusing");
                lblStatus.ForeColor = _pomodoro.IsPaused ? CPaused : CFocus;
                lblCountdown.ForeColor = _pomodoro.IsPaused ? CPaused : CFocus;
            }
            else
            {
                lblStatus.Text = _pomodoro.IsPaused
                    ? Localization.T("StatusRestingPaused")
                    : Localization.T("StatusResting");
                lblStatus.ForeColor = _pomodoro.IsPaused ? CPaused : CRest;
                lblCountdown.ForeColor = _pomodoro.IsPaused ? CPaused : CRest;
            }

            lblRound.Text = Localization.T(
                "RoundFormat",
                _pomodoro.CurrentRound,
                _pomodoro.TotalRounds,
                _pomodoro.CurrentTask?.Name ?? string.Empty);

            lblCountdown.Font = _timerFont;
            UpdateCountdown(_pomodoro.RemainingSeconds);
            btnPause.Text = _pomodoro.IsPaused
                ? Localization.T("ButtonResume")
                : Localization.T("ButtonPause");
        }

        private void ResetTimerUI()
        {
            _showCompletion = false;
            lblCountdown.Text = "00:00";
            lblCountdown.ForeColor = CIdle;
            lblCountdown.Font = _timerFont;
            lblStatus.Text = Localization.T("StatusReady");
            lblStatus.ForeColor = CIdle;
            lblRound.Text = string.Empty;
            btnPause.Text = Localization.T("ButtonPause");
        }

        private void SetRunning(bool running)
        {
            _isRunning = running;

            btnStart.Enabled = !running;
            btnPause.Enabled = running;
            btnSkip.Enabled = running;
            btnStop.Enabled = running;
            cmbTask.Enabled = !running && cmbTask.HasTasks;
            btnRefresh.Enabled = !running;
            btnAdd.Enabled = !running;
            btnEdit.Enabled = !running;
            btnDelete.Enabled = !running;
        }

        // ==================== About ====================

        private void ShowAbout()
        {
            var version = Application.ProductVersion;
            MessageBox.Show(
                Localization.T("AboutText", version),
                Localization.T("AboutTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ==================== Theme ====================

        private void SwitchTheme(AppTheme theme)
        {
            _currentTheme = theme;
            menuThemeDefault.Checked = theme == AppTheme.Default;
            menuThemeDark.Checked = theme == AppTheme.Dark;

            BackColor = theme == AppTheme.Dark ? ThemeService.DarkBackground : SystemColors.Control;
            pnlTimer.BackColor = theme == AppTheme.Dark ? ThemeService.DarkSurface : SystemColors.Control;

            ThemeService.Apply(this, theme);

            lblNotes.ForeColor = lblTrayHint.ForeColor = theme == AppTheme.Dark
                ? ThemeService.DarkSubText
                : SystemColors.GrayText;

            ApplyTitleBarTheme();
            UpdateTimerUI();
            SaveAllPrefs();
        }

        private void ApplyTitleBarTheme()
        {
            if (!IsHandleCreated) return;

            int value = _currentTheme == AppTheme.Dark ? 1 : 0;

            try { DwmSetWindowAttribute(Handle, 20, ref value, sizeof(int)); } catch { }
            try { DwmSetWindowAttribute(Handle, 19, ref value, sizeof(int)); } catch { }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int value,
            int size);

        // ==================== Fonts ====================

        private static Font CreateDefaultUiFont()
        {
            return new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
        }

        private static Font CreateDefaultTimerFont()
        {
            return new Font("Consolas", 32F, FontStyle.Bold);
        }

        private static Font ParseFont(string value, string defaultFamily, float defaultSize, FontStyle defaultStyle)
        {
            try
            {
                var font = new FontConverter().ConvertFromString(value) as Font;
                if (font != null) return font;
            }
            catch
            {
                // Fall back to the default font.
            }

            return new Font(defaultFamily, defaultSize, defaultStyle);
        }

        private void ChangeUiFont()
        {
            using (var dialog = new FontDialog
            {
                Font = _uiFont,
                FontMustExist = true,
                MinSize = 8,
                MaxSize = 12,
                ShowColor = false,
                ShowEffects = false,
                AllowScriptChange = false
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                ApplyUiFont(dialog.Font);
                SaveAllPrefs();
            }
        }

        private void ChangeTimerFont()
        {
            using (var dialog = new FontDialog
            {
                Font = _timerFont,
                FontMustExist = true,
                MinSize = 20,
                MaxSize = 36,
                ShowColor = false,
                ShowEffects = false,
                AllowScriptChange = false
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                ApplyTimerFont(dialog.Font);
                SaveAllPrefs();
            }
        }

        private void ResetFonts()
        {
            using (var ui = CreateDefaultUiFont())
                ApplyUiFont(ui);
            using (var timer = CreateDefaultTimerFont())
                ApplyTimerFont(timer);

            SaveAllPrefs();
        }

        private void ApplyUiFont(Font font)
        {
            if (font == null) return;

            var newFont = (Font)font.Clone();
            var newBold = new Font(newFont, FontStyle.Bold);

            _ownedFonts.Add(newFont);
            _ownedFonts.Add(newBold);

            _uiFont = newFont;
            _uiBoldFont = newBold;
            Font = _uiFont;

            FontHelper.Apply(this, _uiFont, lblCountdown);
            btnStart.Font = _uiBoldFont;
            FontHelper.ApplyToolStrip(menuStrip.Items, _uiFont);
            FontHelper.ApplyToolStrip(trayMenu.Items, _uiFont);
        }

        private void ApplyTimerFont(Font font)
        {
            if (font == null) return;

            var newFont = (Font)font.Clone();

            _ownedFonts.Add(newFont);
            _timerFont = newFont;
            lblCountdown.Font = _timerFont;
        }

        // ==================== Language ====================

        private void SetLanguage(AppLanguage language)
        {
            Localization.SetLanguage(language);

            bool sampleChanged = LocalizeDefaultSample();
            if (sampleChanged) _data.Save(_tasks);

            ApplyLanguage();
            if (sampleChanged) RefreshTaskList();

            SaveAllPrefs();
        }

        private void ApplyLanguage()
        {
            Text = Localization.T("AppTitle");
            notifyIcon.Text = Localization.T("AppTitle");

            menuSettings.Text = Localization.T("MenuSettings");
            menuTheme.Text = Localization.T("MenuTheme");
            menuThemeDefault.Text = Localization.T("ThemeDefault");
            menuThemeDark.Text = Localization.T("ThemeDark");
            menuFont.Text = Localization.T("MenuFont");
            menuTimerFont.Text = Localization.T("MenuTimerFont");
            menuFontReset.Text = Localization.T("MenuFontReset");
            menuLanguage.Text = Localization.T("MenuLanguage");
            menuLanguageEnglish.Text = Localization.T("LanguageEnglish");
            menuLanguageChinese.Text = Localization.T("LanguageChinese");
            menuSound.Text = Localization.T("MenuSound");
            menuSoundBeep.Text = Localization.T("SoundBeep");
            menuSoundCustom.Text = Localization.T("SoundCustom");
            menuSoundSilent.Text = Localization.T("SoundSilent");
            menuSoundDevice.Text = Localization.T("SoundDevice");
            menuNotify.Text = Localization.T("MenuNotify");
            menuNotifyBalloon.Text = Localization.T("NotifyBalloon");
            menuNotifyToast.Text = Localization.T("NotifyToast");
            menuTest.Text = Localization.T("MenuTest");
            menuTestToast.Text = Localization.T("TestToast");
            menuTestBalloon.Text = Localization.T("TestBalloon");
            menuTestSound.Text = Localization.T("TestSound");
            menuTestCompletion.Text = Localization.T("TestCompletion");
            menuReset.Text = Localization.T("MenuReset");
            menuHelp.Text = Localization.T("MenuHelp");
            menuAbout.Text = Localization.T("MenuAbout");

            grpSelect.Text = Localization.T("GroupTask");
            btnRefresh.Text = Localization.T("ButtonRefresh");
            btnAdd.Text = Localization.T("ButtonNew");
            btnEdit.Text = Localization.T("ButtonEdit");
            btnDelete.Text = Localization.T("ButtonDelete");
            grpInfo.Text = Localization.T("GroupInfo");
            lblTrayHint.Text = Localization.T("TrayHint");
            btnStart.Text = Localization.T("ButtonStart");
            btnPause.Text = Localization.T("ButtonPause");
            btnSkip.Text = Localization.T("ButtonSkip");
            btnStop.Text = Localization.T("ButtonStop");

            trayShow.Text = Localization.T("TrayShow");
            trayStart.Text = Localization.T("ButtonStart");
            trayPause.Text = Localization.T("ButtonPause");
            traySkip.Text = Localization.T("ButtonSkip");
            trayStop.Text = Localization.T("ButtonStop");
            traySettings.Text = Localization.T("TraySettings");
            trayAbout.Text = Localization.T("TrayAbout");
            trayExit.Text = Localization.T("TrayExit");

            menuLanguageEnglish.Checked = Localization.Current == AppLanguage.English;
            menuLanguageChinese.Checked = Localization.Current == AppLanguage.Chinese;

            if (_selectedTask != null) SelectTask(_selectedTask);
            else ShowEmptyInfo();

            UpdateTimerUI();
            UpdateTrayState();
        }

        // ==================== Preferences ====================

        private void LoadPrefs()
        {
            var settings = Properties.Settings.Default;

            Localization.SetLanguage(Localization.ParseLanguage(settings.Language));

            using (var ui = ParseFont(settings.AppFont, "Microsoft YaHei UI", 9F, FontStyle.Regular))
                ApplyUiFont(ui);

            using (var timer = ParseFont(settings.TimerFont, "Consolas", 32F, FontStyle.Bold))
                ApplyTimerFont(timer);

            ApplyLanguage();

            try
            {
                if (settings.Theme == "Dark")
                    SwitchTheme(AppTheme.Dark);
            }
            catch
            {
            }

            try
            {
                _soundMode = (SoundMode)settings.SoundMode;
                _customSoundPath = settings.SoundFilePath;
                _audioDeviceIdx = settings.SoundDevice;
                _notifyUseToast = settings.NotifyToast;

                menuSoundBeep.Checked = _soundMode == SoundMode.SystemBeep;
                menuSoundCustom.Checked = _soundMode == SoundMode.CustomFile;
                menuSoundSilent.Checked = _soundMode == SoundMode.Silent;
                menuNotifyBalloon.Checked = !_notifyUseToast;
                menuNotifyToast.Checked = _notifyUseToast;
            }
            catch
            {
            }
        }

        private void SaveAllPrefs()
        {
            try
            {
                var settings = Properties.Settings.Default;
                settings.Theme = _currentTheme == AppTheme.Dark ? "Dark" : "Default";
                settings.SoundMode = (int)_soundMode;
                settings.SoundFilePath = _customSoundPath ?? string.Empty;
                settings.SoundDevice = _audioDeviceIdx;
                settings.NotifyToast = _notifyUseToast;
                settings.AppFont = new FontConverter().ConvertToString(_uiFont);
                settings.TimerFont = new FontConverter().ConvertToString(_timerFont);
                settings.Language = Localization.Current == AppLanguage.Chinese ? "zh-CN" : "en";
                settings.Save();
            }
            catch
            {
                // Preferences are best-effort; task data is handled separately.
            }
        }

        private void ResetAllSettings()
        {
            if (MessageBox.Show(
                    Localization.T("ResetConfirm"),
                    Localization.T("ResetTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Properties.Settings.Default.Reset();
            }
            catch
            {
            }

            _soundMode = SoundMode.SystemBeep;
            _customSoundPath = null;
            _audioDeviceIdx = -1;
            _notifyUseToast = false;

            menuSoundBeep.Checked = true;
            menuSoundCustom.Checked = false;
            menuSoundSilent.Checked = false;
            menuNotifyBalloon.Checked = true;
            menuNotifyToast.Checked = false;

            Localization.SetLanguage(AppLanguage.Chinese);
            using (var ui = CreateDefaultUiFont())
                ApplyUiFont(ui);
            using (var timer = CreateDefaultTimerFont())
                ApplyTimerFont(timer);

            SwitchTheme(AppTheme.Default);
            ApplyLanguage();
            SaveAllPrefs();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(
                area.Left + Math.Max(0, (area.Width - Width) / 2),
                area.Top + Math.Max(0, (area.Height - Height) / 2));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Re-apply after the native handles exist. This is needed for the
            // custom task selector and the DWM title bar.
            ThemeService.Apply(this, _currentTheme);
            ApplyTitleBarTheme();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _uiTimer?.Stop();
            _uiTimer?.Dispose();

            if (_tasks != null) _data.Save(_tasks);

            _audio.Dispose();

            foreach (var font in _ownedFonts)
                font.Dispose();
            _ownedFonts.Clear();

            notifyIcon.Visible = false;

            base.OnFormClosed(e);
        }
    }
}
