/*
 *  Конвертер изображений в NES формат
 * 
 *  Автор: Авдюхин Алексей / clusterrr@clusterrr.com / http://clusterrr.com
 *  Специально для BBDO Group
 * 
 */



using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ManyTilesConverter
{
    partial class Program
    {
        class Palette : IEquatable<Palette>
        {
            public List<Color> Colors;
            public Palette(IEnumerable<Color> colors)
            {
                Colors = new List<Color>();
                var bgColor = colors.First<Color>();
                var colorsList = new List<Color>();
                colorsList.AddRange(colors);
                colorsList.Sort((x, y) => (x.ToArgb() == y.ToArgb() ? 0 : (x.ToArgb() > y.ToArgb() ? 1 : -1)));
                colorsList.Remove(bgColor);
                colorsList.Insert(0, bgColor);
                foreach (var color in colorsList)
                {
                    Colors.Add(color);
                    if (Colors.Count == 4) break;
                }
            }

            public bool Equals(Palette other)
            {
                if (Colors.Count != other.Colors.Count) return false;
                for (int i = 0; i < Colors.Count; i++)
                    if (Colors[i] != other.Colors[i]) return false;
                return true;
            }

            public bool Contains(Palette other)
            {
                foreach (var color in other.Colors)
                {
                    if (!Colors.Contains(color))
                        return false;
                }
                return true;
            }
        }
    }
}
