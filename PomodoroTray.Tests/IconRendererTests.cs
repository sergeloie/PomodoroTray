using System.Runtime.InteropServices;
using PomodoroTray;

namespace PomodoroTray.Tests;

/// <summary>
/// Шов B: IconRenderer — адаптер «растровый квадрат → Icon» в нативном
/// размере иконки трея (SM_CXSMICON). Ожидаемый размер берём напрямую
/// из системы — независимый источник истины.
/// </summary>
public class IconRendererTests
{
    private const int SM_CXSMICON = 49; // winuser.h: SM_CXSMICON

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [Fact]
    public void Icon_has_the_system_tray_icon_size()
    {
        int expected = GetSystemMetrics(SM_CXSMICON);
        Assert.True(expected > 0, "Системный размер иконки трея должен быть > 0");

        using var icon = IconRenderer.Render(25, PomodoroPhase.Work, isRunning: false);

        Assert.Equal(expected, icon.Width);
        Assert.Equal(expected, icon.Height);
    }
}
