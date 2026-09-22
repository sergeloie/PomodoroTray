using System;
using System.Windows.Forms;

namespace PomodoroTray
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly System.Windows.Forms.Timer _uiTimer;
        private readonly PomodoroEngine _engine;
        private readonly PopupPanel _popup;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _startPauseMenuItem;
        private readonly AppSettings _settings;

        // Кэш иконки: рисуем заново только когда изменились минуты/фаза/состояние,
        // а не каждую секунду (дизайн меняется раз в минуту).
        private int _iconMinutes = -1;
        private PomodoroPhase _iconPhase;
        private bool _iconRunning;
        private bool _iconDirty = true;

        public TrayApplicationContext()
        {
            _settings = AppSettings.Load();

            _engine = new PomodoroEngine { AutoStart = _settings.AutoStart };

            // При включённом автостарте первая фаза стартует сразу при запуске.
            if (_settings.AutoStart)
                _engine.Start();

            _popup = new PopupPanel();

            _menu = BuildContextMenu(out _startPauseMenuItem);

            _trayIcon = new NotifyIcon
            {
                Icon = IconRenderer.Render(_engine.WorkMinutes, _engine.CurrentPhase, _engine.IsRunning),
                Visible = true,
                ContextMenuStrip = _menu,
                Text = BuildTooltip()
            };

            _trayIcon.MouseUp += TrayIcon_MouseUp;

            _popup.StartPauseClicked += (s, e) => TogglePause();
            _popup.ResetClicked += (s, e) => ResetTimer();
            _popup.SkipClicked += (s, e) => SkipPhase();

            _engine.Tick += (s, e) => RefreshUI();
            _engine.PhaseCompleted += Engine_PhaseCompleted;

            // Таймер UI тикает раз в секунду и продвигает движок.
            _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uiTimer.Tick += (s, e) => _engine.OnSecondElapsed();
            _uiTimer.Start();

            RefreshUI();
        }

        private ContextMenuStrip BuildContextMenu(out ToolStripMenuItem startPauseItem)
        {
            var menu = new ContextMenuStrip();

            startPauseItem = new ToolStripMenuItem("Start", null, (s, e) => TogglePause());
            var resetItem = new ToolStripMenuItem("Reset", null, (s, e) => ResetTimer());
            var skipItem = new ToolStripMenuItem("Skip Phase", null, (s, e) => SkipPhase());
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitApp());

            var autoStartItem = new ToolStripMenuItem("Autostart")
            {
                CheckOnClick = true,
                Checked = _settings.AutoStart
            };
            autoStartItem.CheckedChanged += (s, e) =>
            {
                _settings.AutoStart = autoStartItem.Checked;
                _engine.AutoStart = _settings.AutoStart;
                _settings.Save();
            };

            var notificationsItem = new ToolStripMenuItem("Notifications")
            {
                CheckOnClick = true,
                Checked = _settings.Notifications
            };
            notificationsItem.CheckedChanged += (s, e) =>
            {
                _settings.Notifications = notificationsItem.Checked;
                _settings.Save();
            };

            menu.Items.Add(startPauseItem);
            menu.Items.Add(resetItem);
            menu.Items.Add(skipItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(autoStartItem);
            menu.Items.Add(notificationsItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            return menu;
        }

        private void TrayIcon_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_popup.Visible)
                    _popup.Hide();
                else
                    _popup.ShowNearTray();
            }
        }

        private void TogglePause()
        {
            _engine.TogglePause();
            RefreshUI();
        }

        private void ResetTimer()
        {
            _engine.Reset();
            RefreshUI();
        }

        private void SkipPhase()
        {
            _engine.SkipPhase();
            RefreshUI();
        }

        private void Engine_PhaseCompleted(object? sender, PomodoroPhase finishedPhase)
        {
            if (!_settings.Notifications)
                return;

            string title = finishedPhase == PomodoroPhase.Work
                ? "Work session finished"
                : "Break finished";

            string body = _engine.CurrentPhase == PomodoroPhase.Work
                ? "Time to get back to work."
                : $"Started: {_engine.PhaseLabel.ToLower()}.";

            _trayIcon.ShowBalloonTip(4000, title, body, ToolTipIcon.Info);
        }

        private void RefreshUI()
        {
            var remaining = _engine.Remaining;
            int minutesForIcon = (int)Math.Ceiling(remaining.TotalMinutes);
            if (remaining.TotalSeconds > 0 && minutesForIcon == 0) minutesForIcon = 1;

            if (_iconDirty || minutesForIcon != _iconMinutes ||
                _engine.CurrentPhase != _iconPhase || _engine.IsRunning != _iconRunning)
            {
                var oldIcon = _trayIcon.Icon;
                _trayIcon.Icon = IconRenderer.Render(minutesForIcon, _engine.CurrentPhase, _engine.IsRunning);
                oldIcon?.Dispose();

                _iconMinutes = minutesForIcon;
                _iconPhase = _engine.CurrentPhase;
                _iconRunning = _engine.IsRunning;
                _iconDirty = false;
            }

            _trayIcon.Text = BuildTooltip();
            _startPauseMenuItem.Text = _engine.IsRunning ? "Pause" : "Start";

            if (_popup.Visible)
            {
                _popup.UpdateDisplay(remaining, _engine.PhaseLabel, _engine.IsRunning);
            }
        }

        private string BuildTooltip()
        {
            var r = _engine.Remaining;
            string time = $"{(int)r.TotalMinutes:00}:{r.Seconds:00}";
            string state = _engine.IsRunning ? "" : " (paused)";
            // NotifyIcon.Text ограничен ~127 символами — укладываемся с запасом.
            return $"Pomodoro — {_engine.PhaseLabel}: {time}{state}";
        }

        private void ExitApp()
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _uiTimer.Stop();
            _uiTimer.Dispose();
            _popup.Dispose();
            Application.Exit();
        }
    }
}
