/*
 *  Конвертер изображений в NES формат
 * 
 *  Автор: Авдюхин Алексей / clusterrr@clusterrr.com / http://clusterrr.com
 *  Специально для BBDO Group
 * 
 */



using System;

namespace ManyTilesConverter
{
    partial class Program
    {
        class PatternTableEntry : IEquatable<PatternTableEntry>
        {
            public byte[,] pixels;

            public PatternTableEntry(byte[,] data)
            {
                pixels = data;
            }

            public bool Equals(PatternTableEntry other)
            {
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++)
                        if (pixels[x, y] != other.pixels[x, y]) return false;
                return true;
            }
        }
    }
}
