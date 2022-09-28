using System;

namespace com.clusterrr.Famicom.NesTiler
{
    class Tile : IEquatable<Tile>
    {
        public readonly byte[,] pixels;

        public Tile(byte[,] data)
        {
            pixels = data;
        }

        public byte[] GetRawData()
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var raw = new byte[width * height / 8 * 2];
            int pixel = 0;
            byte bit = 7;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if ((pixels[x, y] & 1) != 0)
                        raw[pixel / 64 * 2 + y] |= (byte)(1 << bit);
                    if ((pixels[x, y] & 2) != 0)
                        raw[pixel / 64 * 2 + y + 8] |= (byte)(1 << bit);
                    pixel++;
                    bit = (byte)((byte)(bit - 1) % 8);
                }
            }
            return raw;
        }

        public bool Equals(Tile other)
        {
            if ((pixels.GetLength(0) != other.pixels.GetLength(0))
                || (pixels.GetLength(1) != other.pixels.GetLength(1)))
                return false;
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (pixels[x, y] != other.pixels[x, y]) 
                        return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    hash ^= hash >> 28;
                    hash <<= 4;
                    hash ^= pixels[x, y];
                }
            return hash;
        }
    }
}
