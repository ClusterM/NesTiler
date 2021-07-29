using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace com.clusterrr.Famicom.NesTiler
{
    class Palette : IEquatable<Palette>, IEnumerable<Color>
    {
        private Color?[] colors = new Color?[3];

        public Color? this[int i]
        {
            get
            {
                if (i < 1 || i > 3) throw new IndexOutOfRangeException("Color index must be between 1 and 3");
                return colors[i - 1];
            }
            set
            {
                if (i < 1 || i > 3) throw new IndexOutOfRangeException("Color index must be between 1 and 3");
                colors[i - 1] = value;
            }
        }
        public int Count { get => colors.Where(c => c.HasValue).Count(); }

        public Palette()
        {
            // Empty palette
        }

        public Palette(FastBitmap image, int leftX, int topY, int width, int height, Color bgColor)
        {
            Dictionary<Color, int> colorCounter = new Dictionary<Color, int>();
            colorCounter[bgColor] = 0;
            for (int y = topY; y < topY + height; y++)
            {
                for (int x = leftX; x < leftX + width; x++)
                {
                    var color = image.GetPixel(x, y);
                    if (!colorCounter.ContainsKey(color)) colorCounter[color] = 0;
                    colorCounter[color]++;
                }
            }

            var sortedColors = colorCounter.Where(kv => kv.Key != bgColor).OrderByDescending(kv => kv.Value).ToList();
            for (int i = 0; i < 3; i++)
                if (sortedColors.Count > i)
                    this[i + 1] = sortedColors[i].Key;
        }

        public void Add(Color color)
        {
            if (Count < 3)
                this[Count + 1] = color;
            else
                throw new IndexOutOfRangeException();
        }

        public Palette(IEnumerable<Color> colors)
        {
            var colorsList = colors.ToList();
            for (int i = 0; i < 3; i++)
                if (colorsList.Count > i) this[i + 1] = colorsList[i];
        }

        public double GetTileDelta(FastBitmap image, int leftX, int topY, int width, int height, Color bgColor)
        {
            double delta = 0;
            for (int y = topY; y < topY + height; y++)
            {
                for (int x = leftX; x < leftX + width; x++)
                {
                    var color = image.GetPixel(x, y);
                    delta += GetMinDelta(color, bgColor).delta;
                }
            }
            return delta;
        }

        private (Color color, double delta) GetMinDelta(Color color, Color bgColor)
        {
            var ac = Enumerable.Concat(colors.Where(c => c.HasValue).Select(c => c.Value), new Color[] { bgColor });
            var result = ac.OrderBy(c => c.GetDelta(color)).First();
            return (result, result.GetDelta(color));
        }

        public bool Equals(Palette other)
        {
            var colors1 = colors.Where(c => c.HasValue)
                .OrderBy(c => c.Value.ToArgb())
                .Select(c => c.Value)
                .ToArray();
            var colors2 = new Color?[] { other[1], other[2], other[3] }
                .Where(c => c.HasValue)
                .OrderBy(c => c.Value.ToArgb())
                .Select(c => c.Value)
                .ToArray();
            return Enumerable.SequenceEqual<Color>(colors1, colors2);
        }

        public bool Contains(Palette other)
        {
            var thisColors = colors.Where(c => c.HasValue);
            var otherColors = new Color?[] { other[1], other[2], other[3] }.Where(c => c.HasValue).Select(c => c.Value);

            foreach (var color in otherColors)
            {
                if (!thisColors.Contains(color))
                    return false;
            }
            return true;
        }

        public IEnumerator<Color> GetEnumerator()
        {
            return colors.Where(c => c.HasValue).Select(c => c.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString() => string.Join(", ", colors.Where(c => c.HasValue).Select(c => ColorTranslator.ToHtml(c.Value)));

        public override int GetHashCode()
        {
            return (this[1]?.R ?? 0) + (this[2]?.R ?? 0) + (this[3]?.R ?? 0)
                | (((this[1]?.G ?? 0) + (this[2]?.G ?? 0) + (this[3]?.G ?? 0)) << 10)
                | (((this[1]?.B ?? 0) + (this[2]?.B ?? 0) + (this[3]?.B ?? 0)) << 20);
        }
    }
}
