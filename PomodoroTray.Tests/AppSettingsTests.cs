using PomodoroTray;

namespace PomodoroTray.Tests;

/// <summary>
/// Clamp() — единственная чистая (без обращения к диску) логика AppSettings:
/// границы интервалов. Верхняя граница 99 минут существует потому, что трей-иконка
/// (PixelClock) умеет показывать максимум две цифры.
/// </summary>
public class AppSettingsTests
{
    [Fact]
    public void Clamp_limits_intervals_to_two_digit_maximum()
    {
        var s = new AppSettings
        {
            WorkMinutes = 123,
            ShortBreakMinutes = 0,
            LongBreakMinutes = -5,
            SessionsBeforeLongBreak = 99
        };
        s.Clamp();

        Assert.Equal(99, s.WorkMinutes);
        Assert.Equal(1, s.ShortBreakMinutes);
        Assert.Equal(1, s.LongBreakMinutes);
        Assert.Equal(12, s.SessionsBeforeLongBreak);
    }

    [Fact]
    public void Clamp_keeps_valid_values_unchanged()
    {
        var s = new AppSettings
        {
            WorkMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            SessionsBeforeLongBreak = 4
        };
        s.Clamp();

        Assert.Equal(25, s.WorkMinutes);
        Assert.Equal(5, s.ShortBreakMinutes);
        Assert.Equal(15, s.LongBreakMinutes);
        Assert.Equal(4, s.SessionsBeforeLongBreak);
    }
}
