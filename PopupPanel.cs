using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace PomodoroTray
{
    /// <summary>
    /// Маленькое окно без рамки, которое всплывает над треем при клике на иконку —
    /// визуально воспринимается как "панель" таскбара, хотя физически это
    /// обычное WinForms-окно, позиционируемое рядом с треем.
    /// Кнопки — системные значки (Segoe Fluent / MDL2), без текста; у закреплённой
    /// панели есть переключатель 📌: пока включён, панель не гаснет при потере фокуса.
    /// </summary>
    public class PopupPanel : Form
    {
        // Кодовые точки системных значков (Segoe Fluent Icons / Segoe MDL2 Assets).
        private const string GlyphPlay = "\uE768";
        private const string GlyphPause = "\uE769";
        private const string GlyphReset = "\uE72C"; // Refresh — сброс
        private const string GlyphSkip = "\uE893";   // Next — пропустить фазу
        private const string GlyphPin = "\uE718";
        private const string GlyphUnpin = "\uE77A";

        private readonly Label _timeLabel;
        private readonly Label _phaseLabel;
        private readonly Button _startPauseButton;
        private readonly Button _resetButton;
        private readonly Button _skipButton;
        private readonly Button _pinButton;
        private readonly ToolTip _tip;

        private bool _pinned;
        private bool _isRunning;

        public event EventHandler? StartPauseClicked;
        public event EventHandler? ResetClicked;
        public event EventHandler? SkipClicked;

        public PopupPanel()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(32, 32, 34);
            Size = new Size(220, 150);
            Padding = new Padding(1); // тонкая рамка за счёт паддинга + цвета формы

            _tip = new ToolTip();

            var inner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            Controls.Add(inner);

            _phaseLabel = new Label
            {
                Text = "Work",
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 24
            };

            _timeLabel = new Label
            {
                Text = "25:00",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 28f, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            // 4 иконочные кнопки в одну строку: 4*(44+4) = 196 <= 202 доступных —
            // переноса на вторую строку (и «белой полоски» обрезанной кнопки) нет.
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(8, 4, 8, 4),
                WrapContents = false
            };

            var glyphFont = MakeGlyphFont();

            _startPauseButton = MakeGlyphButton(glyphFont, GlyphPlay, 44);
            _resetButton = MakeGlyphButton(glyphFont, GlyphReset, 44);
            _skipButton = MakeGlyphButton(glyphFont, GlyphSkip, 44);
            _pinButton = MakeGlyphButton(glyphFont, GlyphPin, 44);

            _tip.SetToolTip(_startPauseButton, "Start");
            _tip.SetToolTip(_resetButton, "Reset");
            _tip.SetToolTip(_skipButton, "Skip phase");
            _tip.SetToolTip(_pinButton, "Pin panel");

            _startPauseButton.Click += (s, e) => StartPauseClicked?.Invoke(this, EventArgs.Empty);
            _resetButton.Click += (s, e) => ResetClicked?.Invoke(this, EventArgs.Empty);
            _skipButton.Click += (s, e) => SkipClicked?.Invoke(this, EventArgs.Empty);
            _pinButton.Click += (s, e) => TogglePin();

            buttonsPanel.Controls.Add(_startPauseButton);
            buttonsPanel.Controls.Add(_resetButton);
            buttonsPanel.Controls.Add(_skipButton);
            buttonsPanel.Controls.Add(_pinButton);

            inner.Controls.Add(buttonsPanel);
            inner.Controls.Add(_timeLabel);
            inner.Controls.Add(_phaseLabel);

            // Закрываем панель при потере фокуса — если она не закреплена.
            Deactivate += (s, e) =>
            {
                if (!_pinned) Hide();
            };
        }

        /// <summary>Системный шрифт значков: Fluent на Win11, фолбэк на MDL2 (Win10).</summary>
        private static Font MakeGlyphFont()
        {
            foreach (var family in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
            {
                foreach (var installed in FontFamily.Families)
                {
                    if (string.Equals(installed.Name, family, StringComparison.OrdinalIgnoreCase))
                        return new Font(family, 14f, FontStyle.Regular, GraphicsUnit.Point);
                }
            }
            // Нет ни одного шрифта значков — отрисуем кодовую точку как текст
            // (увидим вместо иконки символ-квадрат; лучше, чем падение).
            return new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point);
        }

        private static Button MakeGlyphButton(Font glyphFont, string glyph, int width)
        {
            var button = new Button
            {
                Text = glyph,
                Width = width,
                Height = 36,
                Margin = new Padding(2, 4, 2, 4),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance =
                {
                    BorderSize = 0, // убираем рамку — источник «белой полоски»
                    MouseOverBackColor = Color.FromArgb(75, 75, 84),
                    MouseDownBackColor = Color.FromArgb(55, 55, 62)
                },
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.Gainsboro,
                Font = glyphFont,
                UseVisualStyleBackColor = false
            };
            return button;
        }

        private void TogglePin()
        {
            _pinned = !_pinned;

            _pinButton.Text = _pinned ? GlyphUnpin : GlyphPin;
            _pinButton.BackColor = _pinned
                ? Color.FromArgb(80, 110, 190)  // закреплено — подсвечено
                : Color.FromArgb(63, 63, 70);
            _tip.SetToolTip(_pinButton, _pinned ? "Unpin" : "Pin panel");
        }

        public void UpdateDisplay(TimeSpan remaining, string phaseLabel, bool isRunning)
        {
            _timeLabel.Text = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
            _phaseLabel.Text = phaseLabel;

            if (_isRunning != isRunning)
            {
                _isRunning = isRunning;
                _startPauseButton.Text = isRunning ? GlyphPause : GlyphPlay;
                _tip.SetToolTip(_startPauseButton, isRunning ? "Pause" : "Start");
            }
        }

        /// <summary>Показывает панель рядом с треем, у правого нижнего угла экрана.</summary>
        public void ShowNearTray()
        {
            var workArea = Screen.PrimaryScreen!.WorkingArea; // область экрана без таскбара
            int x = workArea.Right - Width - 8;
            int y = workArea.Bottom - Height - 8;
            Location = new Point(x, y);

            Show();
            Activate();
        }

        protected override bool ShowWithoutActivation => false;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _tip.Dispose();
            base.Dispose(disposing);
        }
    }
}
