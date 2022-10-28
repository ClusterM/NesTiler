using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Color = System.Drawing.Color;

namespace com.clusterrr.Famicom.NesTiler
{
    class Palette : IEquatable<Palette>, IEnumerable<SKColor>
    {
        public struct LossyInfo
        {
            public int ImageNum { get; init; }
            public int ColorCount { get; init; }
            public int TileX { get; init; }
            public int TileY { get; init; }
            public int TileWidth { get; init; }
            public int TileHeight { get; init; }
        }

        private SKColor[] colors;
        private Dictionary<ColorPair, (SKColor color, double delta)> deltaCache = new();
        public LossyInfo? ColorLossy { get; init; } = null;

        public SKColor? this[int i]
        {
            get
            {
                if (i > colors.Length) return null;
                if (i <= 0) throw new ArgumentOutOfRangeException("Invalid color index");
                return colors[i - 1];
            }
        }
        public int Count => colors.Length;

        public Palette(int imageNum, FastBitmap image, int tileX, int tileY, int tileWidth, int tileHeight, SKColor bgColor)
        {
            Dictionary<SKColor, int> colorCounter = new();
            for (int y = tileY; y < tileY + tileHeight; y++)
            {
                if (y < 0) continue;
                for (int x = tileX; x < tileX + tileWidth; x++)
                {
                    var color = image.GetPixelColor(x, y);
                    if (color == bgColor) continue;
                    if (!colorCounter.ContainsKey(color)) colorCounter[color] = 0;
                    colorCounter[color]++;
                }
            }

            // TODO: one more lossy level?
            var colorsCandidates = colorCounter.OrderByDescending(kv => kv.Value);
            if (colorsCandidates.Count() > 3) ColorLossy = new()
            {
                ImageNum = imageNum,
                ColorCount = colorsCandidates.Count(),
                TileX = tileX,
                TileY = tileY,
                TileWidth = tileWidth,
                TileHeight = tileHeight
            };
            colors = colorsCandidates.Take(3).OrderBy(kv => kv.Key.ToArgb()).Select(kv => kv.Key).ToArray();
        }

        public Palette(IEnumerable<SKColor> colors)
        {
            this.colors = colors.OrderBy(c => c.ToArgb()).Take(3).ToArray();
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
            var ac = Enumerable.Concat(colors, new SKColor[] { bgColor });
            var result = ac.OrderBy(c => c.GetDelta(color)).First();
            var r = (result, result.GetDelta(color));
            deltaCache[pair] = r;
            return r;
        }

        public bool Equals(Palette? other)
        {
            if (other == null) return false;
            var r = Enumerable.SequenceEqual(this, other);
            return r;
        }

        public bool Contains(Palette other)
        {
            foreach (var color in other)
            {
                if (!this.Contains(color))
                    return false;
            }
            return true;
        }

        public IEnumerator<SKColor> GetEnumerator()
        {
            return colors.Select(c => c).GetEnumerator(); // wtf?
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString() => string.Join(", ", colors.Select(c => ColorTranslator.ToHtml(c.ToColor())).OrderBy(c => c));

        public override int GetHashCode() => (int)((this[1]?.ToArgb() ?? 0) + ((this[2]?.ToArgb() ?? 0) << 4) + ((this[3]?.ToArgb() ?? 0) << 8));
    }
}
