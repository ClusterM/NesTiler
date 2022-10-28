using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using SkiaSharp;
using System.Collections.Generic;
using System.Drawing;

namespace com.clusterrr.Famicom.NesTiler
{
    public record ColorPair
    {
        public SKColor Color1;
        public SKColor Color2;
    }

    static class ColorExtensions
    {
        private static Dictionary<ColorPair, double> cache = new();
        private static CieDe2000Comparison comparer = new();

        public static void ClearCache() => cache.Clear();

        public static Color ToColor(this SKColor color)
            => Color.FromArgb((int)color.ToArgb());

        public static SKColor ToSKColor(this Color color)
            => new SKColor((uint)color.ToArgb());

        public static uint ToArgb(this SKColor color)
            => (uint)((color.Alpha << 24) | (color.Red << 16) | (color.Green << 8) | color.Blue);

        public static string ToHtml(this Color color) => ColorTranslator.ToHtml(color);

        public static string ToHtml(this SKColor color) => color.ToColor().ToHtml();

        public static double GetDelta(this SKColor color1, SKColor color2)
        {
            var pair = new ColorPair()
            {
                Color1 = color1,
                Color2 = color2
            };
            if (cache.ContainsKey(pair)) return cache[pair];
            var a = new Rgb { R = color1.Red, G = color1.Green, B = color1.Blue };
            var b = new Rgb { R = color2.Red, G = color2.Green, B = color2.Blue };
            var delta = a.Compare(b, comparer);
            cache[pair] = delta;
            return delta;
        }
    }
}
