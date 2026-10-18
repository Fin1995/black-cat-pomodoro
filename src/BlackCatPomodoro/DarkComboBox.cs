using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BlackCatPomodoro
{
    /// <summary>
    /// ComboBox with a dark owner-drawn field and list. The native
    /// DropDownList control ignores BackColor for its text area on several
    /// Windows versions, so the colors are applied through WM_CTLCOLOR*
    /// messages and owner drawing.
    /// </summary>
    public sealed class DarkComboBox : ComboBox
    {
        private IntPtr _backgroundBrush = IntPtr.Zero;
        private bool _darkMode;

        public Color DarkBackColor { get; set; } = Color.FromArgb(60, 60, 63);
        public Color DarkForeColor { get; set; } = Color.FromArgb(240, 240, 240);
        public Color DarkSelectionColor { get; set; } = Color.FromArgb(72, 72, 77);

        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                if (_darkMode == value) return;

                _darkMode = value;
                DrawMode = value ? DrawMode.OwnerDrawFixed : DrawMode.Normal;
                Invalidate();
            }
        }

        public DarkComboBox()
        {
            DrawItem += DrawDarkItem;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (_backgroundBrush == IntPtr.Zero)
                _backgroundBrush = CreateSolidBrush(ColorTranslator.ToWin32(DarkBackColor));
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_backgroundBrush != IntPtr.Zero)
            {
                DeleteObject(_backgroundBrush);
                _backgroundBrush = IntPtr.Zero;
            }

            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (!_darkMode) return;

            // WM_CTLCOLORLISTBOX, WM_CTLCOLOREDIT, WM_CTLCOLORSTATIC
            if (m.Msg == 0x0134 || m.Msg == 0x0133 || m.Msg == 0x0138)
            {
                SetBkColor(m.WParam, ColorTranslator.ToWin32(DarkBackColor));
                SetTextColor(m.WParam, ColorTranslator.ToWin32(DarkForeColor));
                m.Result = _backgroundBrush;
            }
        }

        private void DrawDarkItem(object sender, DrawItemEventArgs e)
        {
            if (!_darkMode) return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (var brush = new SolidBrush(selected ? DarkSelectionColor : DarkBackColor))
                e.Graphics.FillRectangle(brush, e.Bounds);

            string text = e.Index >= 0 && e.Index < Items.Count
                ? GetItemText(Items[e.Index])
                : Text;

            TextRenderer.DrawText(
                e.Graphics,
                text ?? string.Empty,
                Font,
                e.Bounds,
                DarkForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(int color);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern int SetBkColor(IntPtr hdc, int color);

        [DllImport("gdi32.dll")]
        private static extern int SetTextColor(IntPtr hdc, int color);
    }
}
