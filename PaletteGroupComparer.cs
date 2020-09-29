/*
 *  Конвертер изображений в NES формат
 * 
 *  Автор: Авдюхин Алексей / clusterrr@clusterrr.com / http://clusterrr.com
 *  Специально для BBDO Group
 * 
 */



using System.Collections.Generic;
using System.Text;

namespace ManyTilesConverter
{
    partial class Program
    {
        class PaletteGroupComparer : IEqualityComparer<Palette>
        {
            public bool Equals(Palette x, Palette y)
            {
                if (x.Colors.Count != y.Colors.Count) return false;
                for (int i = 0; i < x.Colors.Count; i++)
                    if (x.Colors[i] != y.Colors[i]) return false;
                return true;
            }

            public int GetHashCode(Palette obj)
            {
                StringBuilder r = new StringBuilder();
                foreach (var color in obj.Colors)
                {
                    r.Append(color.ToArgb());
                }
                return r.ToString().GetHashCode();
            }
        }
    }
}
