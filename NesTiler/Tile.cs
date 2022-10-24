using System;
using System.IO.Hashing;
using System.Linq;

namespace com.clusterrr.Famicom.NesTiler
{
    sealed record Tile : IEquatable<Tile>
    {
        public readonly byte[] Pixels;
        public readonly int Width;
        public readonly int Height;
        private int? hash;
        private byte[] data = null;

        public Tile(byte[] data, int width, int height)
        {
            (Pixels, Width, Height) = (data, width, height);
        }

        public byte[] GetAsTileData()
        {
            if (data != null) return data;
            data = new byte[Width * Height / 8 * 2];
            lock (data)
            {
                int pixel = 0;
                byte bit = 7;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if ((Pixels[(y * Width) + x] & 1) != 0)
                            data[(pixel / 64 * 2) + y] |= (byte)(1 << bit);
                        if ((Pixels[(y * Width) + x] & 2) != 0)
                            data[(pixel / 64 * 2) + y + 8] |= (byte)(1 << bit);
                        pixel++;
                        bit = (byte)((byte)(bit - 1) % 8);
                    }
                }
            }
            return data;
        }

        public bool Equals(Tile other)
        {
            var data1 = GetAsTileData();
            var data2 = other.GetAsTileData();
            return Enumerable.SequenceEqual(data1, data2);
        }

        public override int GetHashCode()
        {
            if (hash != null) return hash.Value;
            var crc = new Crc32();
            crc.Append(GetAsTileData());
            var hashBytes = crc.GetCurrentHash();
            hash = BitConverter.ToInt32(hashBytes, 0);
            return hash.Value;
        }
    }
}
