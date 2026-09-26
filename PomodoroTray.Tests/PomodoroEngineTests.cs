using PomodoroTray;

namespace PomodoroTray.Tests;

/// <summary>
/// Логика таймера — чистая, без WinForms: интервалы фаз и количество
/// рабочих сессий перед длинным перерывом можно задавать программно
/// (в приложении их подставляет диалог «Settings...»).
/// </summary>
public class PomodoroEngineTests
{
    private static PomodoroEngine MakeEngine() => new()
    {
        WorkMinutes = 25,
        ShortBreakMinutes = 5,
        LongBreakMinutes = 15,
        SessionsBeforeLongBreak = 4
    };

    [Fact]
    public void ApplyDurations_resets_current_phase_to_new_full_duration()
    {
        var engine = MakeEngine();
        engine.Start();

        // Прокручиваем 10 минут работы.
        for (int i = 0; i < 600; i++) engine.OnSecondElapsed();
        Assert.Equal(15 * 60, engine.Remaining.TotalSeconds);

        // Меняем длительность работы на 50 минут — текущая фаза перезапускается.
        engine.WorkMinutes = 50;
        engine.ApplyDurations();

        Assert.Equal(50 * 60, engine.Remaining.TotalSeconds);
    }

    [Fact]
    public void Long_break_comes_after_configured_number_of_work_sessions()
    {
        var engine = MakeEngine();
        engine.SessionsBeforeLongBreak = 3;
        engine.Start();

        // 3 работы подряд должны закончиться длинным перерывом.
        for (int session = 0; session < 3; session++)
        {
            // Досчитываем рабочую фазу до конца (плюс запас — автостарт
            // уже переключил фазу).
            for (int i = 0; i <= 25 * 60; i++) engine.OnSecondElapsed();

            if (session < 2)
            {
                Assert.Equal(PomodoroPhase.ShortBreak, engine.CurrentPhase);
                // Пропускаем короткий перерыв.
                engine.SkipPhase();
            }
        }

        Assert.Equal(PomodoroPhase.LongBreak, engine.CurrentPhase);
    }

    [Fact]
    public void New_durations_take_effect_on_next_phase_after_skip()
    {
        var engine = MakeEngine();
        engine.ShortBreakMinutes = 7;

        engine.SkipPhase(); // Work -> ShortBreak

        Assert.Equal(PomodoroPhase.ShortBreak, engine.CurrentPhase);
        Assert.Equal(7 * 60, engine.Remaining.TotalSeconds);
    }
}
