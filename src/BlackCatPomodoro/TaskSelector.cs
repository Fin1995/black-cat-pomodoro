using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace BlackCatPomodoro
{
    /// <summary>
    /// Task picker that draws its own field and drop-down arrow. The native
    /// ComboBox arrow cannot be recolored reliably on all Windows versions,
    /// so this control keeps the field and the arrow in one consistent style.
    /// </summary>
    public sealed class TaskSelector : Control
    {
        private readonly ContextMenuStrip _menu;
        private List<PomodoroTask> _tasks = new List<PomodoroTask>();
        private PomodoroTask _selected;
        private bool _darkMode;

        public event EventHandler SelectedTaskChanged;

        public PomodoroTask SelectedTask => _selected;

        public bool HasTasks => _tasks.Count > 0;

        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                if (_darkMode == value) return;

                _darkMode = value;
                ApplyTheme();
            }
        }

        public TaskSelector()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);

            TabStop = true;
            Cursor = Cursors.Hand;

            _menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                ShowCheckMargin = true
            };
            _menu.Closed += (s, e) => Focus();

            ApplyTheme();
        }

        public void SetTasks(IEnumerable<PomodoroTask> tasks, PomodoroTask selected)
        {
            _tasks = tasks?.ToList() ?? new List<PomodoroTask>();
            _selected = selected;

            if (_selected == null || !_tasks.Contains(_selected))
                _selected = _tasks.Count > 0 ? _tasks[0] : null;

            Enabled = _tasks.Count > 0;
            Invalidate();
        }

        public void SelectTask(PomodoroTask task, bool raiseEvent)
        {
            if (ReferenceEquals(_selected, task)) return;

            _selected = task;
            Invalidate();

            if (raiseEvent)
                SelectedTaskChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyTheme()
        {
            BackColor = _darkMode ? ThemeService.DarkInput : SystemColors.Window;
            ForeColor = _darkMode ? ThemeService.DarkText : SystemColors.WindowText;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Color border = _darkMode ? ThemeService.DarkBorder : SystemColors.ControlDark;
            Color text = Enabled
                ? (_darkMode ? ThemeService.DarkText : SystemColors.WindowText)
                : SystemColors.GrayText;

            using (var brush = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(brush, ClientRectangle);

            using (var pen = new Pen(border))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            const int arrowArea = 22;
            var textBounds = new Rectangle(8, 1, Math.Max(0, Width - arrowArea - 8), Height - 2);
            TextRenderer.DrawText(
                e.Graphics,
                _selected?.Name ?? string.Empty,
                Font,
                textBounds,
                text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            int cx = Width - arrowArea / 2 - 1;
            int cy = Height / 2;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(text, 1.6f))
            {
                e.Graphics.DrawLine(pen, cx - 4, cy - 2, cx, cy + 2);
                e.Graphics.DrawLine(pen, cx, cy + 2, cx + 4, cy - 2);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
                ShowMenu();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down)
            {
                ShowMenu();
                e.Handled = true;
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        private void ShowMenu()
        {
            if (!Enabled || _tasks.Count == 0) return;

            _menu.Items.Clear();
            _menu.Font = Font;
            ThemeService.ApplyMenuRenderer(_menu, _darkMode ? AppTheme.Dark : AppTheme.Default);

            foreach (var task in _tasks)
            {
                var item = new ToolStripMenuItem(task.Name)
                {
                    Tag = task,
                    Checked = ReferenceEquals(task, _selected)
                };
                item.Click += (s, e) => SelectTask(task, true);
                _menu.Items.Add(item);
            }

            _menu.MinimumSize = new Size(Width, 0);
            _menu.Show(this, new Point(0, Height));
        }
    }
}
