using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using System.Collections.Generic;
using System.Drawing;

namespace com.clusterrr.Famicom.NesTiler
{
    public record ColorPair
    {
        public Color Color1;
        public Color Color2;
    }

    static class ColorExtensions
    {
        static Dictionary<ColorPair, double> cache = new();

        public static void ClearCache() => cache.Clear();

        public static double GetDelta(this Color color1, Color color2)
        {
            var pair = new ColorPair()
            {
                Color1 = color1,
                Color2 = color2
            };
            if (cache.ContainsKey(pair))
                return cache[pair];
            var a = new Rgb { R = color1.R, G = color1.G, B = color1.B };
            var b = new Rgb { R = color2.R, G = color2.G, B = color2.B };
            var delta = a.Compare(b, new CieDe2000Comparison());
            cache[pair] = delta;
            return delta;
        }
    }
}
