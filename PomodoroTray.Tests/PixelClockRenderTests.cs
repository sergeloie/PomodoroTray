using System.Drawing;
using PomodoroTray;

namespace PomodoroTray.Tests;

/// <summary>
/// Шов A (продолжение): композиция цифр в растровую иконку.
/// Эталон собирается из ASCII-паттернов глифов — геометрия раскладки
/// (отступы, положение знаков) зафиксирована здесь как спецификация.
/// </summary>
public class PixelClockRenderTests
{
    private const int Size = 16;

    // Эталонные паттерны (spec) — те же, что в PixelClockGlyphTests.
    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['2'] = Glyph(
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
            "#######"),
        ['5'] = Glyph(
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
            "#######"),
        ['7'] = Glyph(
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
            ".##...."),
    };

    private static string[] Glyph(params string[] rows) => rows;

    private static bool[,] Parse(string[] rows)
    {
        var result = new bool[rows[0].Length, rows.Length];
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[0].Length; x++)
                result[x, y] = rows[y][x] == '#';
        return result;
    }

    /// <summary>Собирает ожидаемую маску 16×16 из глифов цифр (раскладка-спека).</summary>
    private static bool[,] ExpectedMask(string number)
    {
        var mask = new bool[Size, Size];
        // Раскладка: глиф 7×14, по вертикали отступ 1 (итог 14, поля по 1).
        // Два знака: x=0..6 и x=9..15 (во всю ширину). Один знак: x=4..10.
        int[] xs = number.Length == 1 ? new[] { 4 } : new[] { 0, 9 };
        for (int i = 0; i < number.Length; i++)
        {
            var g = Parse(Glyphs[number[i]]);
            for (int y = 0; y < 14; y++)
                for (int x = 0; x < 7; x++)
                    if (g[x, y])
                        mask[xs[i] + x, 1 + y] = true;
        }
        return mask;
    }

    [Fact]
    public void Render_two_digit_number_fills_the_square_with_phase_color()
    {
        using var bmp = PixelClock.Render(25, PomodoroPhase.Work, isRunning: true, Size);

        Assert.Equal(Size, bmp.Width);
        Assert.Equal(Size, bmp.Height);

        var expected = ExpectedMask("25");
        var expectedColor = Color.FromArgb(255, 90, 90); // красный — работа (палитра из IconRenderer)

        AssertMaskMatches(bmp, expected, expectedColor);
    }

    [Theory]
    [InlineData(PomodoroPhase.Work, 255, 90, 90)]        // красный — работа
    [InlineData(PomodoroPhase.ShortBreak, 90, 220, 140)] // зелёный — короткий перерыв
    [InlineData(PomodoroPhase.LongBreak, 110, 170, 255)] // синий — длинный перерыв
    public void Render_uses_phase_color_when_running(PomodoroPhase phase, int r, int g, int b)
    {
        using var bmp = PixelClock.Render(25, phase, isRunning: true, Size);
        AssertMaskMatches(bmp, ExpectedMask("25"), Color.FromArgb(r, g, b));
    }

    [Fact]
    public void Render_is_grey_when_paused()
    {
        using var bmp = PixelClock.Render(25, PomodoroPhase.Work, isRunning: false, Size);
        AssertMaskMatches(bmp, ExpectedMask("25"), Color.FromArgb(190, 190, 190));
    }

    [Fact]
    public void Render_single_digit_is_centered()
    {
        using var bmp = PixelClock.Render(7, PomodoroPhase.ShortBreak, isRunning: true, Size);

        // Одна цифра «7»: x=4..10 (поля 4 слева, 5 справа), y=1..14.
        AssertMaskMatches(bmp, ExpectedMask("7"), Color.FromArgb(90, 220, 140));
    }

    [Fact]
    public void Render_scales_by_nearest_neighbor_when_size_differs_from_design()
    {
        const int scale = 2;
        using var bmp = PixelClock.Render(25, PomodoroPhase.Work, isRunning: true, Size * scale);

        var designMask = ExpectedMask("25");
        var color = Color.FromArgb(255, 90, 90);

        for (int oy = 0; oy < Size * scale; oy++)
        {
            for (int ox = 0; ox < Size * scale; ox++)
            {
                bool expectedLit = designMask[ox / scale, oy / scale];
                var pixel = bmp.GetPixel(ox, oy);
                if (expectedLit && (pixel.A != 255 || pixel.R != color.R || pixel.G != color.G || pixel.B != color.B))
                    Assert.Fail($"Пиксель ({ox},{oy}) блокной заливки ожидался {color}, получен {pixel}");
                if (!expectedLit && pixel.A != 0)
                    Assert.Fail($"Пиксель ({ox},{oy}) должен быть пустым, получен {pixel}");
            }
        }
    }

    private static void AssertMaskMatches(Bitmap bmp, bool[,] expected, Color expectedColor)
    {
        int size = expected.GetLength(0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var pixel = bmp.GetPixel(x, y);
                if (expected[x, y])
                {
                    if (pixel.A != 255 || pixel.R != expectedColor.R ||
                        pixel.G != expectedColor.G || pixel.B != expectedColor.B)
                        Assert.Fail(
                            $"Залитый пиксель ({x},{y}): ожидался {expectedColor} с альфой 255, получен {pixel}");
                }
                else if (pixel.A != 0)
                {
                    Assert.Fail($"Пиксель ({x},{y}) должен быть пустым (прозрачным), получен {pixel}");
                }
            }
        }
    }
}
