using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace com.clusterrr.Famicom.NesTiler
{
    public class FastBitmap
    {
        public int Width { get; }
        public int Height { get; }

        private readonly SKColor[] colors;
        private static Dictionary<string, SKBitmap> imagesCache = new Dictionary<string, SKBitmap>();

        private FastBitmap(SKBitmap skBitmap, int verticalOffset = 0, int height = -1)
        {
            Width = skBitmap.Width;
            Height = height <= 0 ? skBitmap.Height - verticalOffset : height;
            if (skBitmap.Height - verticalOffset - Height < 0 || Height <= 0) throw new InvalidOperationException("Invalid image height.");
            var pixels = skBitmap.Pixels;
            colors = skBitmap.Pixels.Skip(verticalOffset * Width).Take(Width * Height).ToArray();
        }

        public static FastBitmap? Decode(string filename, int verticalOffset = 0, int height = -1)
        {
            try
            {
                using (var image = SKBitmap.Decode(filename))
                {
                    if (image == null) return null;
                    imagesCache[filename] = image;
                    return new FastBitmap(image, verticalOffset, height);
                }
            }
            finally
            {
                GC.Collect();
            }
        }

        public SKColor GetPixelColor(int x, int y)
        {
            return colors[(y * Width) + x];
        }

        public void SetPixelColor(int x, int y, SKColor color)
        {
            colors[(y * Width) + x] = color;
        }

        public byte[] Encode(SKEncodedImageFormat format, int v)
        {
            using var skImage = new SKBitmap(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var color = colors[(y * Width) + x];
                    skImage.SetPixel(x, y, color);
                }
            }
            return skImage.Encode(format, v).ToArray();
        }
    }
}
