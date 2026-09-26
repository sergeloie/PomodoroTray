using System;
using System.IO;
using System.Text.Json;

namespace PomodoroTray
{
    /// <summary>
    /// Настройки приложения: JSON в %AppData%\PomodoroTray\settings.json.
    /// Отсутствующий или повреждённый файл — не ошибка: используются значения
    /// по умолчанию. Проблемы с диском не должны ломать работу таймера.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Автоматически запускать следующую фазу после завершения текущей.</summary>
        public bool AutoStart { get; set; } = true;

        /// <summary>Показывать balloon-уведомления при завершении фазы.</summary>
        public bool Notifications { get; set; } = true;

        // Длительности фаз (в минутах) и количество рабочих сессий перед
        // длинным перерывом — редактируются в диалоге «Settings...».
        public int WorkMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;
        public int SessionsBeforeLongBreak { get; set; } = 4;

        /// <summary>
        /// Приводит интервалы к разумным границам: битый settings.json или
        /// мусор из диалога не должны приводить к отрицательному таймеру.
        /// </summary>
        public void Clamp()
        {
            const int MinMinutes = 1;
            const int MaxMinutes = 180;
            const int MinSessions = 1;
            const int MaxSessions = 12;

            WorkMinutes = Math.Clamp(WorkMinutes, MinMinutes, MaxMinutes);
            ShortBreakMinutes = Math.Clamp(ShortBreakMinutes, MinMinutes, MaxMinutes);
            LongBreakMinutes = Math.Clamp(LongBreakMinutes, MinMinutes, MaxMinutes);
            SessionsBeforeLongBreak = Math.Clamp(SessionsBeforeLongBreak, MinSessions, MaxSessions);
        }

        private static string DirPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PomodoroTray");

        public static string FilePath => Path.Combine(DirPath, "settings.json");

        private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                    if (loaded is not null)
                        return loaded;
                }
            }
            catch
            {
                // Файл отсутствует/битый/не читается — откатываемся к дефолтам.
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(DirPath);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, WriteOptions));
            }
            catch
            {
                // Диск недоступен — настройки живут до конца сессии, таймер не падает.
            }
        }
    }
}
