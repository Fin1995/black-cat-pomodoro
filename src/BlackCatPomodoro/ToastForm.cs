using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlackCatPomodoro
{
    /// <summary>
    /// Borderless bottom-right notification. It closes itself after a few
    /// seconds, or immediately when clicked.
    /// </summary>
    public partial class ToastForm : Form
    {
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint flags);

        private readonly System.Windows.Forms.Timer _closeTimer;
        private CancellationTokenSource _fadeCts;

        public ToastForm(string title, string message)
        {
            InitializeComponent();

            lblTitle.Text = title;
            lblMessage.Text = message;

            Click += (s, e) => CloseToast();
            pnlBorder.Click += (s, e) => CloseToast();
            lblTitle.Click += (s, e) => CloseToast();
            lblMessage.Click += (s, e) => CloseToast();

            _closeTimer = new System.Windows.Forms.Timer { Interval = 6000 };
            _closeTimer.Tick += (s, e) => CloseToast();
        }

        public void Pop()
        {
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(area.Right - Width - 16, area.Bottom - Height - 16);

            Opacity = 0;
            Show();

            TopMost = true;
            SetWindowPos(
                Handle,
                HWND_TOPMOST,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            _fadeCts = new CancellationTokenSource();
            _ = FadeInAsync(_fadeCts.Token);
            _closeTimer.Start();
        }

        private async Task FadeInAsync(CancellationToken token)
        {
            try
            {
                for (double opacity = 0; opacity <= 1.0; opacity += 0.1)
                {
                    if (IsDisposed || token.IsCancellationRequested) return;
                    Opacity = opacity;
                    await Task.Delay(20, token);
                }

                if (!IsDisposed) Opacity = 1.0;
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void CloseToast()
        {
            if (IsDisposed) return;

            _closeTimer.Stop();
            _fadeCts?.Cancel();
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _fadeCts?.Cancel();
            _fadeCts?.Dispose();
            _fadeCts = null;

            _closeTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
