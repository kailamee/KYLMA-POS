using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace KYLMA.WinForms
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Money is shown in Philippine pesos (P) on every screen and receipt,
            // regardless of the Windows region setting.
            var pesos = new CultureInfo("en-PH");
            CultureInfo.DefaultThreadCurrentCulture = pesos;
            CultureInfo.DefaultThreadCurrentUICulture = pesos;
            Thread.CurrentThread.CurrentCulture = pesos;
            Thread.CurrentThread.CurrentUICulture = pesos;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Errors on the UI thread: log them and show a safe message instead of crashing.
            Application.ThreadException += (sender, e) => UiHelper.ShowError(null, "Unexpected error", e.Exception);

            // Errors on background threads: log only, there is nowhere safe to show them.
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                if (ex != null) AppLogger.LogException("Fatal", ex);
            };

            Application.Run(new LoginForm());
        }
    }
}
