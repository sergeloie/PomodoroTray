using System;
using System.Windows.Forms;

namespace PomodoroTray
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize(); // требует net8.0-windows + UseWindowsForms
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            using var trayApp = new TrayApplicationContext();
            Application.Run(trayApp);
        }
    }
}
