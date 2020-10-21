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

        public bool Equals(Tile other)
        {
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    if (pixels[x, y] != other.pixels[x, y]) return false;
            return true;
        }
    }
}
