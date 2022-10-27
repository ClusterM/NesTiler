using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Color = System.Drawing.Color;

namespace com.clusterrr.Famicom.NesTiler
{
    class Palette : IEquatable<Palette>, IEnumerable<SKColor>
    {
        private SKColor?[] colors = new SKColor?[3];
        private Dictionary<ColorPair, (SKColor color, double delta)> deltaCache = new();

        public SKColor? this[int i]
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

        public Palette(FastBitmap image, int leftX, int topY, int width, int height, SKColor bgColor)
        {
            Dictionary<SKColor, int> colorCounter = new();
            for (int y = topY; y < topY + height; y++)
            {
                if (y < 0) continue;
                for (int x = leftX; x < leftX + width; x++)
                {
                    var color = image.GetPixelColor(x, y);
                    if (color == bgColor) continue;
                    if (!colorCounter.ContainsKey(color)) colorCounter[color] = 0;
                    colorCounter[color]++;
                }
            }

            var sortedColors = colorCounter
                .OrderByDescending(kv => kv.Value).ToArray();
            for (int i = 0; i < 3; i++)
                if (sortedColors.Length > i)
                    this[i + 1] = sortedColors[i].Key;
        }

        public void Add(SKColor color)
        {
            if (Count >= 3) throw new IndexOutOfRangeException();
            this[Count + 1] = color;
            deltaCache.Clear();
        }

        public Palette(IEnumerable<SKColor> colors)
        {
            var colorsList = colors.ToList();
            for (int i = 0; i < 3; i++)
                if (colorsList.Count > i) this[i + 1] = colorsList[i];
        }

        public double GetTileDelta(FastBitmap image, int leftX, int topY, int width, int height, SKColor bgColor)
        {
            double delta = 0;
            for (int y = topY; y < topY + height; y++)
            {
                if (y < 0) continue;
                for (int x = leftX; x < leftX + width; x++)
                {
                    var color = image.GetPixelColor(x, y);
                    delta += GetMinDelta(color, bgColor).delta;
                }
            }
            return delta;
        }

        private (SKColor color, double delta) GetMinDelta(SKColor color, SKColor bgColor)
        {
            var pair = new ColorPair()
            {
                Color1 = color,
                Color2 = bgColor
            };
            if (deltaCache.ContainsKey(pair)) return deltaCache[pair];
            var ac = Enumerable.Concat(colors.Where(c => c.HasValue).Select(c => c!.Value), new SKColor[] { bgColor });
            var result = ac.OrderBy(c => c.GetDelta(color)).First();
            var r = (result, result.GetDelta(color));
            deltaCache[pair] = r;
            return r;
        }

        public bool Equals(Palette? other)
        {
            if (other == null) return false;
            var colors1 = colors.Where(c => c.HasValue)
                .OrderBy(c => c!.Value.ToArgb())
                .Select(c => c!.Value)
                .ToArray();
            var colors2 = new SKColor?[] { other[1], other[2], other[3] }
                .Where(c => c.HasValue)
                .OrderBy(c => c!.Value.ToArgb())
                .Select(c => c!.Value)
                .ToArray();
            return Enumerable.SequenceEqual(colors1, colors2);
        }

        public bool Contains(Palette other)
        {
            var thisColors = colors.Where(c => c.HasValue);
            var otherColors = new SKColor?[] { other[1], other[2], other[3] }.Where(c => c.HasValue).Select(c => c!.Value);

            foreach (var color in otherColors)
            {
                if (!thisColors.Contains(color))
                    return false;
            }
            return true;
        }

        public IEnumerator<SKColor> GetEnumerator()
        {
            return colors.Where(c => c.HasValue).Select(c => c!.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString() => string.Join(", ", colors.Where(c => c.HasValue).Select(c => ColorTranslator.ToHtml(c!.Value.ToColor())).OrderBy(c => c));

        public override int GetHashCode()
        {
            return ((this[1]?.Red ?? 0) + (this[2]?.Red ?? 0) + (this[3]?.Red ?? 0))
                | (((this[1]?.Green ?? 0) + (this[2]?.Green ?? 0) + (this[3]?.Green ?? 0)) << 10)
                | (((this[1]?.Blue ?? 0) + (this[2]?.Blue ?? 0) + (this[3]?.Blue ?? 0)) << 20);
        }
    }
}
