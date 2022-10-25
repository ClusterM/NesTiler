using System;
using System.IO.Hashing;
using System.Linq;

namespace com.clusterrr.Famicom.NesTiler
{
    sealed record Tile : IEquatable<Tile>
    {
        public readonly byte[] Pixels;
        public const int Width = 8;
        public readonly int Height;
        private int? hash;
        private byte[] data = null;

        public Tile(byte[] data, int height)
        {
            (Pixels, Height) = (data, height);
        }

        public byte[] GetAsPatternData()
        {
            if (data != null) return data;
            data = new byte[Height * 2]; // two bits per pixel
            lock (data)
            {
                int pixel = 0; // total pixels counter
                byte bit = 7;  // bit number
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        // for each pixel
                        if ((Pixels[(y * Width) + x] & 1) != 0) // check bit 0
                            data[y / 8 * 16 + (y % 8)] |= (byte)(1 << bit);
                        if ((Pixels[(y * Width) + x] & 2) != 0) // check bit 1
                            data[y / 8 * 16 + (y % 8) + 8] |= (byte)(1 << bit);
                        pixel++;
                        bit = (byte)((byte)(bit - 1) % 8); // decrease bit number, wrap around if need
                    }
                }
            }
            return data;
        }

        public bool Equals(Tile other)
        {
            var data1 = GetAsPatternData();
            var data2 = other.GetAsPatternData();
            return Enumerable.SequenceEqual(data1, data2);
        }

        public override int GetHashCode()
        {
            if (hash != null) return hash.Value;
            var crc = new Crc32();
            crc.Append(GetAsPatternData());
            var hashBytes = crc.GetCurrentHash();
            hash = BitConverter.ToInt32(hashBytes, 0);
            return hash.Value;
        }
    }
}
