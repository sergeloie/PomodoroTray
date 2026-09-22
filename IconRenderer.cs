using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace PomodoroTray
{
    /// <summary>
    /// Рисует иконку трея "на лету" в нативном размере (SM_CXSMICON —
    /// обычно 16×16, с DPI — больше): пиксельные цифры PixelClock во весь
    /// квадрат, цвет = фаза. Внешние .ico-файлы не нужны.
    /// </summary>
    public static class IconRenderer
    {
        private const int SM_CXSMICON = 49; // winuser.h

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon Render(int minutesRemaining, PomodoroPhase phase, bool isRunning)
        {
            int size = GetSystemMetrics(SM_CXSMICON);
            if (size <= 0) size = PixelClock.DesignSize;

            using var bmp = PixelClock.Render(minutesRemaining, phase, isRunning, size);

            IntPtr hIcon = bmp.GetHicon();
            try
            {
                // Icon.FromHandle не копирует память — оборачиваем в новый Icon,
                // чтобы можно было безопасно освободить hIcon.
                using var tempIcon = Icon.FromHandle(hIcon);
                return (Icon)tempIcon.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
    }
}
