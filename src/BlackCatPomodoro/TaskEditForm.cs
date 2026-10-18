using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlackCatPomodoro
{
    /// <summary>
    /// Add or edit a task. Passing null creates a new task.
    /// </summary>
    public partial class TaskEditForm : Form
    {
        public PomodoroTask Result { get; private set; }

        public TaskEditForm(PomodoroTask task = null, Font uiFont = null)
        {
            InitializeComponent();

            txtName.MaxLength = 20;
            txtNotes.MaxLength = 200;
            txtRepeat.MaxLength = 3;
            txtFocusCustom.MaxLength = 3;
            txtRestCustom.MaxLength = 3;

            _editingTask = task;

            ThemeService.Apply(this, ThemeService.CurrentTheme);
            WireEvents();

            if (task != null) LoadTask(task);
            else SetDefaults();

            ApplyLanguage();

            if (uiFont != null)
                FontHelper.Apply(this, uiFont);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ThemeService.ApplyTitleBar(this, ThemeService.CurrentTheme);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Native text-box handles are created when the dialog is shown.
            // Re-apply the theme so enabled text boxes keep the dark input
            // background instead of falling back to the system window color.
            ThemeService.Apply(this, ThemeService.CurrentTheme);
            ThemeService.ApplyTitleBar(this, ThemeService.CurrentTheme);
        }

        private void WireEvents()
        {
            txtName.TextChanged += (s, e) => UpdateCharCounts();

            txtNotes.TextChanged += (s, e) => UpdateCharCounts();

            rdoFocus15.CheckedChanged += (s, e) =>
            {
                if (rdoFocus15.Checked)
                {
                    txtFocusCustom.Enabled = false;
                    txtFocusCustom.Text = "15";
                }
            };
            rdoFocus25.CheckedChanged += (s, e) =>
            {
                if (rdoFocus25.Checked)
                {
                    txtFocusCustom.Enabled = false;
                    txtFocusCustom.Text = "25";
                }
            };
            rdoFocusCustom.CheckedChanged += (s, e) =>
            {
                if (rdoFocusCustom.Checked)
                {
                    txtFocusCustom.Enabled = true;
                    txtFocusCustom.Focus();
                }
            };

            rdoRest5.CheckedChanged += (s, e) =>
            {
                if (rdoRest5.Checked)
                {
                    txtRestCustom.Enabled = false;
                    txtRestCustom.Text = "5";
                }
            };
            rdoRestCustom.CheckedChanged += (s, e) =>
            {
                if (rdoRestCustom.Checked)
                {
                    txtRestCustom.Enabled = true;
                    txtRestCustom.Focus();
                }
            };

            txtFocusCustom.Enter += (s, e) => rdoFocusCustom.Checked = true;
            txtRestCustom.Enter += (s, e) => rdoRestCustom.Checked = true;

            btnConfirm.Click += BtnConfirm_Click;
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
        }

        private void ApplyLanguage()
        {
            Text = _editingTask == null
                ? Localization.T("NewTaskTitle")
                : Localization.T("EditTaskTitle");
            btnConfirm.Text = _editingTask == null
                ? Localization.T("ButtonAdd")
                : Localization.T("ButtonSave");
            btnCancel.Text = Localization.T("ButtonCancel");

            grpName.Text = Localization.T("TaskNameLabel");
            grpFocus.Text = Localization.T("FocusGroup");
            rdoFocus15.Text = Localization.T("Focus15");
            rdoFocus25.Text = Localization.T("Focus25");
            rdoFocusCustom.Text = Localization.T("Custom");
            lblFocusUnit.Text = Localization.T("MinutesUnit");

            grpRest.Text = Localization.T("RestGroup");
            rdoRest5.Text = Localization.T("Rest5");
            rdoRestCustom.Text = Localization.T("Custom");
            lblRestUnit.Text = Localization.T("MinutesUnit");

            grpAdvanced.Text = Localization.T("AdvancedGroup");
            lblRepeat.Text = Localization.T("RoundsLabel");
            lblRepeatUnit.Text = Localization.T("TimesUnit");

            grpNotes.Text = Localization.T("NotesGroup");
            lblHint.Text = Localization.T("RequiredHint");

            UpdateCharCounts();
        }

        private void UpdateCharCounts()
        {
            lblCharCount.Text = Localization.T("CharCountFormat", txtName.Text.Length, 20);
            lblCharCount.ForeColor = txtName.Text.Length > 20
                ? Color.Red
                : SystemColors.GrayText;

            lblNotesCount.Text = Localization.T("CharCountFormat", txtNotes.Text.Length, 200);
        }

        private void SetDefaults()
        {
            Text = Localization.T("NewTaskTitle");
            btnConfirm.Text = Localization.T("ButtonAdd");

            rdoFocus25.Checked = false;
            rdoFocus25.Checked = true;
            txtFocusCustom.Text = "25";
            txtFocusCustom.Enabled = false;

            rdoRest5.Checked = false;
            rdoRest5.Checked = true;
            txtRestCustom.Text = "5";
            txtRestCustom.Enabled = false;

            txtRepeat.Text = "1";
        }

        private void LoadTask(PomodoroTask task)
        {
            Text = Localization.T("EditTaskTitle");
            btnConfirm.Text = Localization.T("ButtonSave");
            txtName.Text = task.Name;

            if (task.FocusMinutes == 15)
            {
                rdoFocus15.Checked = false;
                rdoFocus15.Checked = true;
                txtFocusCustom.Text = "15";
                txtFocusCustom.Enabled = false;
            }
            else if (task.FocusMinutes == 25)
            {
                rdoFocus25.Checked = false;
                rdoFocus25.Checked = true;
                txtFocusCustom.Text = "25";
                txtFocusCustom.Enabled = false;
            }
            else
            {
                rdoFocusCustom.Checked = false;
                rdoFocusCustom.Checked = true;
                txtFocusCustom.Text = task.FocusMinutes.ToString();
                txtFocusCustom.Enabled = true;
            }

            if (task.RestMinutes == 5)
            {
                rdoRest5.Checked = false;
                rdoRest5.Checked = true;
                txtRestCustom.Text = "5";
                txtRestCustom.Enabled = false;
            }
            else
            {
                rdoRestCustom.Checked = false;
                rdoRestCustom.Checked = true;
                txtRestCustom.Text = task.RestMinutes.ToString();
                txtRestCustom.Enabled = true;
            }

            txtRepeat.Text = task.RepeatCount.ToString();
            txtNotes.Text = task.Notes;
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(
                    Localization.T("NameEmpty"),
                    Localization.T("NoticeTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (name.Length > 20)
            {
                MessageBox.Show(
                    Localization.T("NameTooLong"),
                    Localization.T("NoticeTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (!int.TryParse(txtFocusCustom.Text, out int focusMin) || focusMin < 1 || focusMin > 180)
            {
                MessageBox.Show(
                    Localization.T("FocusRange"),
                    Localization.T("NoticeTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtFocusCustom.Focus();
                return;
            }

            if (!int.TryParse(txtRestCustom.Text, out int restMin) || restMin < 0 || restMin > 60)
            {
                MessageBox.Show(
                    Localization.T("RestRange"),
                    Localization.T("NoticeTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtRestCustom.Focus();
                return;
            }

            if (!int.TryParse(txtRepeat.Text, out int repeat) || repeat < 1 || repeat > 20)
            {
                MessageBox.Show(
                    Localization.T("RoundsRange"),
                    Localization.T("NoticeTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtRepeat.Focus();
                return;
            }

            string notes = txtNotes.Text.Trim();
            if (notes.Length > 200)
            {
                MessageBox.Show(
                    Localization.T("NotesTooLong"),
                    Localization.T("NoticeTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtNotes.Focus();
                return;
            }

            if (_editingTask != null)
            {
                _editingTask.Name = name;
                _editingTask.FocusMinutes = focusMin;
                _editingTask.RestMinutes = restMin;
                _editingTask.RepeatCount = repeat;
                _editingTask.Notes = notes;
                Result = _editingTask;
            }
            else
            {
                Result = new PomodoroTask
                {
                    Name = name,
                    FocusMinutes = focusMin,
                    RestMinutes = restMin,
                    RepeatCount = repeat,
                    Notes = notes
                };
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
