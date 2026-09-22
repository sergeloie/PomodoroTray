using System;

namespace PomodoroTray
{
    public enum PomodoroPhase
    {
        Work,
        ShortBreak,
        LongBreak
    }

    /// <summary>
    /// Чистая логика таймера: ничего не знает про WinForms, трей и т.д.
    /// Легко тестировать и легко поменять длительности фаз.
    /// </summary>
    public class PomodoroEngine
    {
        // Длительности фаз (в минутах) — при желании можно вынести в настройки/UI.
        public int WorkMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;

        // После скольких рабочих сессий подряд — длинный перерыв.
        public int SessionsBeforeLongBreak { get; set; } = 4;

        public PomodoroPhase CurrentPhase { get; private set; } = PomodoroPhase.Work;
        public int CompletedWorkSessions { get; private set; } = 0;
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Автостарт: после завершения фазы следующая начинается сразу (true)
        /// или таймер встаёт на паузу до ручного «Старт» (false).
        /// </summary>
        public bool AutoStart { get; set; } = true;

        private TimeSpan _remaining;
        public TimeSpan Remaining => _remaining;

        public event EventHandler? Tick;
        public event EventHandler<PomodoroPhase>? PhaseCompleted; // фаза, которая только что завершилась

        public PomodoroEngine()
        {
            _remaining = TimeSpan.FromMinutes(WorkMinutes);
        }

        public void Start() => IsRunning = true;

        public void Pause() => IsRunning = false;

        public void TogglePause()
        {
            IsRunning = !IsRunning;
        }

        public void Reset()
        {
            IsRunning = false;
            CurrentPhase = PomodoroPhase.Work;
            CompletedWorkSessions = 0;
            _remaining = TimeSpan.FromMinutes(WorkMinutes);
            Tick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Пропустить текущую фазу и сразу перейти к следующей.</summary>
        public void SkipPhase()
        {
            AdvancePhase();
            Tick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Вызывать раз в секунду (например, из System.Windows.Forms.Timer).</summary>
        public void OnSecondElapsed()
        {
            if (!IsRunning) return;

            if (_remaining > TimeSpan.Zero)
            {
                _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
                Tick?.Invoke(this, EventArgs.Empty);
            }

            if (_remaining <= TimeSpan.Zero)
            {
                var finished = CurrentPhase;
                AdvancePhase();
                PhaseCompleted?.Invoke(this, finished);
                Tick?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AdvancePhase()
        {
            switch (CurrentPhase)
            {
                case PomodoroPhase.Work:
                    CompletedWorkSessions++;
                    CurrentPhase = (CompletedWorkSessions % SessionsBeforeLongBreak == 0)
                        ? PomodoroPhase.LongBreak
                        : PomodoroPhase.ShortBreak;
                    break;

                case PomodoroPhase.ShortBreak:
                case PomodoroPhase.LongBreak:
                    CurrentPhase = PomodoroPhase.Work;
                    break;
            }

            _remaining = CurrentPhase switch
            {
                PomodoroPhase.Work => TimeSpan.FromMinutes(WorkMinutes),
                PomodoroPhase.ShortBreak => TimeSpan.FromMinutes(ShortBreakMinutes),
                PomodoroPhase.LongBreak => TimeSpan.FromMinutes(LongBreakMinutes),
                _ => TimeSpan.FromMinutes(WorkMinutes)
            };

            // Автостарт: следующая фаза стартует сразу или ждёт ручного «Старт»
            // — в зависимости от настройки (пункт меню «Автостарт»).
            IsRunning = AutoStart;
        }

        public string PhaseLabel => CurrentPhase switch
        {
            PomodoroPhase.Work => "Работа",
            PomodoroPhase.ShortBreak => "Короткий перерыв",
            PomodoroPhase.LongBreak => "Длинный перерыв",
            _ => ""
        };
    }
}
