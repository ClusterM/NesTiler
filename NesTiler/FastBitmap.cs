using SkiaSharp;
using System;
using System.Collections.Generic;

namespace com.clusterrr.Famicom.NesTiler;

public class FastBitmap
{
    public int Width { get; }
    public int Height { get; }

    private readonly int verticalOffset;
    private readonly SKColor[] pixels;
    private static readonly Dictionary<string, (SKColor[] pixels, int w, int h)> imagesCache = new();

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
            using var image = SKBitmap.Decode(filename);
            if (image == null)
                return null;
            SKColor[] pixels = image.Pixels;
            imagesCache[filename] = (pixels, image.Width, image.Height);
            return new FastBitmap(pixels, image.Width, image.Height, verticalOffset, height);
        }
        finally
        {
            GC.Collect();
        }
    }

    public SKColor GetPixelColor(int x, int y, SKColor? defaultColor = null)
    {
        var index = (y + verticalOffset) * Width + x;
        return index >= pixels.Length
            ? defaultColor.HasValue ? defaultColor.Value : throw new IndexOutOfRangeException($"Pixel {x}x{y} is out of range")
            : pixels[index];
    }

    public void SetPixelColor(int x, int y, SKColor color) => pixels[(y + verticalOffset) * Width + x] = color;

    public byte[] Encode(SKEncodedImageFormat format, int v)
    {
        using var skImage = new SKBitmap(Width, Height);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                SKColor color = GetPixelColor(x, y);
                skImage.SetPixel(x, y, color);
            }
        }
        return skImage.Encode(format, v).ToArray();
    }
}
