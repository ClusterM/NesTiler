using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace com.clusterrr.Famicom.NesTiler
{
    class ColorFinder
    {
        static byte[] FORBIDDEN_COLORS = { 0x0D };

        public readonly Dictionary<byte, SKColor> Colors;
        private readonly Dictionary<SKColor, byte> cache = new();

        public ColorFinder(string filename)
        {
            this.Colors = LoadColors(filename);
        }

        private static Dictionary<byte, SKColor> LoadColors(string filename)
        {
            Trace.WriteLine($"Loading colors from {filename}...");
            if (!File.Exists(filename)) throw new FileNotFoundException($"Could not find file '{filename}'.", filename);
            var data = File.ReadAllBytes(filename);
            Dictionary<byte, SKColor> nesColors;
            // Detect file type
            if ((Path.GetExtension(filename) == ".pal") || ((data.Length == 192 || data.Length == 1536) && data.Where(b => b >= 128).Any()))
            {
                // Binary file
                nesColors = new Dictionary<byte, SKColor>();
                for (byte c = 0; c < 64; c++)
                {
                    var color = new SKColor(data[c * 3], data[(c * 3) + 1], data[(c * 3) + 2]);
                    nesColors[c] = color;
                }
            }
            else
            {
                var paletteJson = File.ReadAllText(filename);
                var nesColorsStr = JsonSerializer.Deserialize<Dictionary<string, string>>(paletteJson);
                if (nesColorsStr == null) throw new InvalidDataException($"Can't parse {filename}");
                nesColors = nesColorsStr.ToDictionary(
                    kv =>
                    {
                        try
                        {
                            var index = kv.Key.ToLower().StartsWith("0x") ? Convert.ToByte(kv.Key[2..], 16) : byte.Parse(kv.Key);
                            if (FORBIDDEN_COLORS.Contains(index))
                                Trace.WriteLine($"WARNING! color #{kv.Key} is forbidden color, it will be ignored.");
                            if (index > 0x3F) throw new ArgumentException($"{kv.Key} - invalid color index.", filename);
                            return index;
                        }
                        catch (Exception ex) when (ex is FormatException || ex is OverflowException)
                        {
                            throw new ArgumentException($"{kv.Key} - invalid color index.", filename);
                        }
                    },
                    kv =>
                    {
                        try
                        {
                            var color = ColorTranslator.FromHtml(kv.Value); ;
                            return new SKColor(color.R, color.G, color.B);
                        }
                        catch (FormatException)
                        {
                            throw new ArgumentException($"{kv.Value} - invalid color.", filename);
                        }
                    }
                );
            }
            // filter out invalid colors;
            nesColors = nesColors.Where(kv => !FORBIDDEN_COLORS.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            return nesColors;
        }

        /// <summary>
        /// Find index of most similar color from NES colors
        /// </summary>
        /// <param name="color">Input color</param>
        /// <returns>Output color index</returns>
        public byte FindSimilarColorIndex(SKColor color)
        {
            if (cache.ContainsKey(color))
                return cache[color];
            byte result = byte.MaxValue;
            double minDelta = double.MaxValue;
            SKColor c = SKColors.Transparent;
            foreach (var index in Colors.Keys)
            {
                var delta = color.GetDelta(Colors[index]);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    result = index;
                    c = Colors[index];
                }
            }
            if (result == byte.MaxValue)
                throw new KeyNotFoundException($"Invalid color: {color}.");
            if (cache != null)
                cache[color] = result;
            return result;
        }

        /// <summary>
        /// Find most similar color from list of colors
        /// </summary>
        /// <param name="colors">Haystack</param>
        /// <param name="color">Niddle</param>
        /// <returns>Output color</returns>
        public SKColor FindSimilarColor(IEnumerable<SKColor> colors, SKColor color)
        {
            SKColor result = SKColors.Black;
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

        /// <summary>
        /// Find most similar color from NES colors
        /// </summary>
        /// <param name="color">Input colo</param>
        /// <returns>Output color</returns>
        public SKColor FindSimilarColor(SKColor color) => Colors[FindSimilarColorIndex(color)];

        public void WriteColorsTable(string filename)
        {
            // Export colors to nice table image
            const int colorSize = 64;
            const int colorColumns = 16;
            const int colorRows = 4;
            const int strokeWidth = 5;
            float textSize = 20;
            float textYOffset = 39;
            using var image = new SKBitmap(colorSize * colorColumns, colorSize * colorRows);
            using var canvas = new SKCanvas(image);
            for (int y = 0; y < colorRows; y++)
            {
                for (int x = 0; x < colorColumns; x++)
                {
                    SKColor color;
                    SKPaint paint;
                    if (Colors.TryGetValue((byte)((y * colorColumns) + x), out color))
                    {
                        paint = new SKPaint() { Color = color };
                        canvas.DrawRegion(new SKRegion(new SKRectI(x * colorSize, y * colorSize, (x + 1) * colorSize, (y + 1) * colorSize)), paint);

                        color = new SKColor((byte)(0xFF - color.Red), (byte)(0xFF - color.Green), (byte)(0xFF - color.Blue)); // invert color
                        paint = new SKPaint()
                        {
                            Color = color,
                            TextAlign = SKTextAlign.Center,
                            TextSize = textSize,
                            FilterQuality = SKFilterQuality.High,
                            IsAntialias = true
                        };
                        canvas.DrawText($"{(y * colorColumns) + x:X02}", (x * colorSize) + (colorSize / 2), (y * colorSize) + textYOffset, paint);
                    }
                    else
                    {
                        paint = new SKPaint() { Color = SKColors.Black };
                        SKPath path = new SKPath();
                        canvas.DrawRegion(new SKRegion(new SKRectI(x * colorSize, y * colorSize, (x + 1) * colorSize, (y + 1) * colorSize)), paint);
                        paint = new SKPaint()
                        {
                            Color = SKColors.Red,
                            Style = SKPaintStyle.Stroke,
                            StrokeCap = SKStrokeCap.Round,
                            StrokeWidth = strokeWidth,
                            FilterQuality = SKFilterQuality.High,
                            IsAntialias = true
                        };
                        canvas.DrawLine((x * colorSize) + strokeWidth, (y * colorSize) + strokeWidth, ((x + 1) * colorSize) - strokeWidth, ((y + 1) * colorSize) - strokeWidth, paint);
                        canvas.DrawLine(((x + 1) * colorSize) - strokeWidth, (y * colorSize) + strokeWidth, (x * colorSize) + strokeWidth, ((y + 1) * colorSize) - strokeWidth, paint);
                    }
                };
            }
            File.WriteAllBytes(filename, image.Encode(SKEncodedImageFormat.Png, 0).ToArray());
        }
    }
}
