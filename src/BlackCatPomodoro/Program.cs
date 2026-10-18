using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace BlackCatPomodoro
{
    internal static class Program
    {
        private const string MutexName = @"Local\BlackCatPomodoro.SingleInstance";

        [STAThread]
        private static void Main()
        {
            try
            {
                Localization.SetLanguage(
                    Localization.ParseLanguage(Properties.Settings.Default.Language));
            }
            catch
            {
                // Use the default language if settings cannot be read.
            }

            using (var mutex = new Mutex(true, MutexName, out bool createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        Localization.T("AlreadyRunning"),
                        Localization.T("AppTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => LogException(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => LogException(e.ExceptionObject as Exception);

                Application.Run(new MainForm());
            }
        }

        private static void LogException(Exception exception)
        {
            if (exception == null) return;

            try
            {
                string path = Path.Combine(Path.GetTempPath(), "BlackCatPomodoro-crash.log");
                File.WriteAllText(path, DateTime.Now.ToString("O") + Environment.NewLine + exception);
            }
            catch
            {
                // Logging is best-effort.
            }
        }
    }
}
