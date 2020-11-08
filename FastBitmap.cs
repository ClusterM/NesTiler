using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.clusterrr.Famicom.NesTiler
{
    public class FastBitmap : IDisposable
    {
        readonly Bitmap bitmap;
        BitmapData data;

        public int Width { get => bitmap.Width; }
        public int Height { get => bitmap.Height; }

        public FastBitmap(Bitmap bitmap)
        {
            //if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            //    throw new FormatException($"Invalid pixel format, only {PixelFormat.Format32bppArgb} is supported");
            this.bitmap = bitmap;
            Lock();
        }

        private void Lock()
        {
            data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        }

        private void Unlock()
        {
            bitmap.UnlockBits(data);
        }

        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x >= bitmap.Width)
                throw new IndexOutOfRangeException($"X out of range: {x}");
            if (y < 0 || y >= bitmap.Height)
                throw new IndexOutOfRangeException($"Y out of range: {y}");
            unsafe
            {
                byte* pixel = (byte*)data.Scan0 + (y * data.Stride) + x * 3;
                return Color.FromArgb(pixel[2], pixel[1], pixel[0]);
            }
        }

        public void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || x >= bitmap.Width)
                throw new IndexOutOfRangeException($"X out of range: {x}");
            if (y < 0 || y >= bitmap.Height)
                throw new IndexOutOfRangeException($"Y out of range: {y}");
            unsafe
            {
                byte* pixel = (byte*)data.Scan0 + (y * data.Stride) + x * 3;
                pixel[0] = color.B;
                pixel[1] = color.G;
                pixel[2] = color.R;
            }
        }

        public void Save(string filename)
        {
            Unlock();
            bitmap.Save(filename);
            Lock();
        }

        public void Save(string filename, ImageFormat format)
        {
            Unlock();
            bitmap.Save(filename, format);
            Lock();
        }

        public void Save(Stream stream, ImageFormat format)
        {
            Unlock();
            bitmap.Save(stream, format);
            Lock();
        }

        public Bitmap GetBitmap()
        {
            Unlock();
            var result = new Bitmap(bitmap);
            Lock();
            return result;
        }

        public void Dispose()
        {
            Lock();
        }

        public static FastBitmap FromFile(string filename)
        {
            var bitmap = (Bitmap)Image.FromFile(filename);
            return new FastBitmap(bitmap);
        }

        public static FastBitmap FromStream(Stream stream)
        {
            var bitmap = (Bitmap)Image.FromStream(stream);
            return new FastBitmap(bitmap);
        }

        public static FastBitmap Copy(Image image)
        {
            var bitmap = new Bitmap(image);
            return new FastBitmap(bitmap);
        }
    }
}
