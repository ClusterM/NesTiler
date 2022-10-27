using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.clusterrr.Famicom.NesTiler
{
    class ColorsFinder 
    {
        private readonly Dictionary<byte, Color> colors;
        private readonly Dictionary<Color, byte> cache = new();

        public ColorsFinder(Dictionary<byte, Color> colors)
        {
            this.colors = colors;
        }

        public byte FindSimilarColor(Color color)
        {
            if (cache.ContainsKey(color))
                return cache[color];
            byte result = byte.MaxValue;
            double minDelta = double.MaxValue;
            Color c = Color.Transparent;
            foreach (var index in colors.Keys)
            {
                var delta = color.GetDelta(colors[index]);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    result = index;
                    c = colors[index];
                }
            }
            if (result == byte.MaxValue)
                throw new KeyNotFoundException($"Invalid color: {color}.");
            if (cache != null)
                cache[color] = result;
            return result;
        }

        public Color FindSimilarColor(IEnumerable<Color> colors, Color color)
        {
            Color result = Color.Black;
            double minDelta = double.MaxValue;
            foreach (var c in colors)
            {
                var delta = color.GetDelta(c);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    result = c;
                }
            }
            return result;
        }
    }
}
