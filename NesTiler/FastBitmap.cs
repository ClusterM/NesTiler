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

        private readonly int verticalOffset;
        private readonly SKColor[] pixels;
        private static Dictionary<string, (SKColor[] pixels, int w, int h)> imagesCache = new();

        private FastBitmap(SKColor[] pixels, int originalWidth, int originalHeight, int verticalOffset = 0, int height = -1)
        {
            Width = originalWidth;
            Height = height <= 0 ? originalHeight - verticalOffset : height;
            this.verticalOffset = verticalOffset;
            this.pixels = pixels;
        }

        public static FastBitmap? Decode(string filename, int verticalOffset = 0, int height = -1)
        {
            if (imagesCache.TryGetValue(filename, out (SKColor[] pixels, int w, int h) cachedImage))
            {
                return new FastBitmap(cachedImage.pixels, cachedImage.w, cachedImage.h, verticalOffset, height);
            }
            try
            {
                using (var image = SKBitmap.Decode(filename))
                {
                    if (image == null) return null;
                    var pixels = image.Pixels;
                    imagesCache[filename] = (pixels, image.Width, image.Height);
                    return new FastBitmap(pixels, image.Width, image.Height, verticalOffset, height);
                }
            }
            finally
            {
                GC.Collect();
            }
        }

        public SKColor GetPixelColor(int x, int y)
        {
            return pixels[((y + verticalOffset) * Width) + x];
        }

        public void SetPixelColor(int x, int y, SKColor color)
        {
            pixels[((y + verticalOffset) * Width) + x] = color;
        }

        public byte[] Encode(SKEncodedImageFormat format, int v)
        {
            using var skImage = new SKBitmap(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var color = GetPixelColor(x, y);
                    skImage.SetPixel(x, y, color);
                }
            }
            return skImage.Encode(format, v).ToArray();
        }
    }
}
