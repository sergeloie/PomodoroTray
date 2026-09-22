using System;
using System.Drawing;

namespace PomodoroTray
{
    /// <summary>
    /// Пиксельные цифры для иконки трея: глифы нарисованы вручную под
    /// сетку 7×14 и компонуются в квадрат 16×16 (два знака — во всю ширину).
    /// Никакого сглаживания: каждый пиксель либо залит, либо пуст.
    /// </summary>
    public static class PixelClock
    {
        public const int GlyphWidth = 7;
        public const int GlyphHeight = 14;
        public const int DesignSize = 16; // нативный размер иконки, под который нарисованы глифы

        // Паттерны-эталоны дизайна: '#' = залитый пиксель, '.' = пустой.
        private static readonly string[][] DigitPatterns =
        {
            /* 0 */ new[]
            {
                "#######",
                "#######",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "#######",
                "#######",
            },
            /* 1 */ new[]
            {
                "..##...",
                ".###...",
                "..##...",
                "..##...",
                "..##...",
                "..##...",
                "..##...",
                "..##...",
                "..##...",
                "..##...",
                "..##...",
                "..##...",
                ".####..",
                ".####..",
            },
            /* 2 */ new[]
            {
                "#######",
                "#######",
                ".....##",
                ".....##",
                ".....##",
                "....##.",
                "...##..",
                "..##...",
                ".##....",
                "##.....",
                "##.....",
                "##.....",
                "#######",
                "#######",
            },
            /* 3 */ new[]
            {
                "#######",
                "#######",
                ".....##",
                ".....##",
                ".....##",
                ".#####.",
                ".#####.",
                ".....##",
                ".....##",
                ".....##",
                ".....##",
                ".....##",
                "#######",
                "#######",
            },
            /* 4 */ new[]
            {
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "#######",
                "#######",
                ".....##",
                ".....##",
                ".....##",
                ".....##",
                ".....##",
                ".....##",
            },
            /* 5 */ new[]
            {
                "#######",
                "#######",
                "##.....",
                "##.....",
                "##.....",
                "##.....",
                "#######",
                "#######",
                ".....##",
                ".....##",
                ".....##",
                ".....##",
                "#######",
                "#######",
            },
            /* 6 */ new[]
            {
                "#######",
                "#######",
                "##.....",
                "##.....",
                "##.....",
                "##.....",
                "#######",
                "#######",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "#######",
                "#######",
            },
            /* 7 */ new[]
            {
                "#######",
                "#######",
                ".....##",
                ".....##",
                "....##.",
                "....##.",
                "...##..",
                "...##..",
                "..##...",
                "..##...",
                ".##....",
                ".##....",
                ".##....",
                ".##....",
            },
            /* 8 */ new[]
            {
                "#######",
                "#######",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "#######",
                "#######",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "#######",
                "#######",
            },
            /* 9 */ new[]
            {
                "#######",
                "#######",
                "##...##",
                "##...##",
                "##...##",
                "##...##",
                "#######",
                "#######",
                ".....##",
                ".....##",
                ".....##",
                ".....##",
                ".....##",
                "....###",
            },
        };

        /// <summary>Возвращает глиф цифры 0–9 размером 7×14 (индексация [x, y]).</summary>
        /// <exception cref="ArgumentOutOfRangeException">Цифра вне диапазона 0–9.</exception>
        public static bool[,] GetDigitGlyph(char digit)
        {
            if (digit < '0' || digit > '9')
                throw new ArgumentOutOfRangeException(nameof(digit), digit, "Ожидаются цифры 0–9.");

            var rows = DigitPatterns[digit - '0'];
            var glyph = new bool[GlyphWidth, GlyphHeight];
            for (int y = 0; y < GlyphHeight; y++)
                for (int x = 0; x < GlyphWidth; x++)
                    glyph[x, y] = rows[y][x] == '#';
            return glyph;
        }

        /// <summary>
        /// Рисует квадрат с числом минут: глифы 7×14 во весь холст
        /// (два знака — во всю ширину), цвет = фаза, при paused — серый.
        /// Дизайн-сетка 16×16; при другом size масштабируется nearest-neighbor,
        /// чтобы пиксели не размывались.
        /// </summary>
        public static Bitmap Render(int minutes, PomodoroPhase phase, bool isRunning, int size)
        {
            if (minutes < 0) minutes = 0;
            if (minutes > 99) minutes = 99;
            string text = minutes.ToString();

            Color color = GetColor(phase, isRunning);

            // Маска в дизайн-размере: раскладка-спека — глифы по вертикали с
            // отступом 1 (y=1..14); два знака на x=0 и x=9 (во всю ширину),
            // один знак — по центру на x=4.
            var mask = new bool[DesignSize, DesignSize];
            int[] xs = text.Length == 1 ? new[] { 4 } : new[] { 0, 9 };
            for (int i = 0; i < text.Length; i++)
            {
                var g = GetDigitGlyph(text[i]);
                for (int y = 0; y < GlyphHeight; y++)
                    for (int x = 0; x < GlyphWidth; x++)
                        if (g[x, y])
                            mask[xs[i] + x, 1 + y] = true;
            }

            var bmp = new Bitmap(size, size);
            // Nearest-neighbor: пиксель выходного холста → пиксель дизайн-сетки.
            for (int oy = 0; oy < size; oy++)
            {
                int iy = oy * DesignSize / size;
                for (int ox = 0; ox < size; ox++)
                {
                    int ix = ox * DesignSize / size;
                    if (mask[ix, iy])
                        bmp.SetPixel(ox, oy, color);
                }
            }
            return bmp;
        }

        /// <summary>Цвет цифр: фаза → цвет, пауза → серый (семантика из IconRenderer).</summary>
        public static Color GetColor(PomodoroPhase phase, bool isRunning)
        {
            if (!isRunning)
                return Color.FromArgb(190, 190, 190); // серый = на паузе/остановлено

            return phase switch
            {
                PomodoroPhase.Work => Color.FromArgb(255, 90, 90),        // красный — работа
                PomodoroPhase.ShortBreak => Color.FromArgb(90, 220, 140), // зелёный — короткий перерыв
                PomodoroPhase.LongBreak => Color.FromArgb(110, 170, 255), // синий — длинный перерыв
                _ => Color.White
            };
        }
    }
}
