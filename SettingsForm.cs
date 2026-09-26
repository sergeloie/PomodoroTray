using System;
using System.Drawing;
using System.Windows.Forms;

namespace PomodoroTray
{
    /// <summary>
    /// Модальный диалог редактирования интервалов: работа, короткий/длинный
    /// перерывы и количество рабочих сессий перед длинным перерывом.
    /// Тема согласована с PopupPanel (тёмная). FlatStyle.Flat убирает белую
    /// рамку контролов — «фирменную» для тёмной темы WinForms.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly NumericUpDown _work;
        private readonly NumericUpDown _shortBreak;
        private readonly NumericUpDown _longBreak;
        private readonly NumericUpDown _sessions;
        private readonly Button _ok;
        private readonly Button _cancel;

        /// <summary>Результат редактирования; валиден после DialogResult.OK.</summary>
        public int WorkMinutes => (int)_work.Value;
        public int ShortBreakMinutes => (int)_shortBreak.Value;
        public int LongBreakMinutes => (int)_longBreak.Value;
        public int SessionsBeforeLongBreak => (int)_sessions.Value;

        public SettingsForm(int workMinutes, int shortBreakMinutes,
                            int longBreakMinutes, int sessionsBeforeLongBreak)
        {
            Text = "Pomodoro Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(32, 32, 34);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 9f);
            AutoScaleMode = AutoScaleMode.Dpi;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 12, 14, 6),
                ColumnCount = 2,
                RowCount = 5,
                BackColor = Color.FromArgb(32, 32, 34)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _work = MakeRow(layout, 0, "Work (minutes)");
            _shortBreak = MakeRow(layout, 1, "Short break (minutes)");
            _longBreak = MakeRow(layout, 2, "Long break (minutes)");
            _sessions = MakeRow(layout, 3, "Sessions before long break");

            // Длинный перерыв после каждой N-й работы: логично ограничить 1..12.
            _sessions.Minimum = 1;
            _sessions.Maximum = 12;

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(6, 12, 0, 0)
            };
            _ok = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 75,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 110, 190),
                ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 }
            };
            _cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 75,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.Gainsboro
            };
            buttons.Controls.Add(_ok);
            buttons.Controls.Add(_cancel);
            layout.Controls.Add(buttons, 0, 4);
            layout.SetColumnSpan(buttons, 2);

            AcceptButton = _ok;
            CancelButton = _cancel;

            Controls.Add(layout);

            _work.Value = workMinutes;
            _shortBreak.Value = shortBreakMinutes;
            _longBreak.Value = longBreakMinutes;
            _sessions.Value = sessionsBeforeLongBreak;
        }

        private static NumericUpDown MakeRow(TableLayoutPanel layout, int row, string labelText)
        {
            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 8, 12, 3)
            };
            var numeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 180,
                Width = 90,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(3, 6, 3, 3)
            };
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(numeric, 1, row);
            return numeric;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _work.Focus();
        }
    }
}
