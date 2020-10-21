using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.clusterrr.Famicom.NesTiler
{
    static class ColorExtensions
    {
        public static double GetDelta(this Color color1, Color color2)
        {
            /*
            var deltaR = Math.Abs((int)color1.R - (int)color2.R);
            var deltaG = Math.Abs((int)color1.G - (int)color2.G);
            var deltaB = Math.Abs((int)color1.B - (int)color2.B);
            var delta = deltaR + deltaG + deltaB;
            */
            /*
            var delta = Math.Sqrt(Math.Pow(color1.R - color2.R, 2) + Math.Pow(color1.G - color2.G, 2) + Math.Pow(color1.B - color2.B, 2));
            */
            var a = new Rgb { R = color1.R, G = color1.G, B = color1.B };
            var b = new Rgb { R = color2.R, G = color2.G, B = color2.B };
            var delta = a.Compare(b, new CieDe2000Comparison());
            return delta;
        }
    }
}
