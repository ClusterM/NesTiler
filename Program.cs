/*
 *  Конвертер изображений в NES формат
 * 
 *  Автор: Авдюхин Алексей / clusterrr@clusterrr.com / http://clusterrr.com
 *  Специально для BBDO Group
 
 *  2018г.
 * 
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ManyTilesConverter
{
    partial class Program
    {
        //static Dictionary<byte, Color> NesPalette = new Dictionary<byte, Color>();

        static void Main(string[] args)
        {
            string paletteFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"palette.json");
            var imageFiles = new string[] { "opening.png", "0.png", "1.png", "2.png", "3.png", "4.png", "clouds.png" };

            /*
            NesPalette[0x00] = Color.FromArgb(0x48, 0x48, 0x48);
            NesPalette[0x01] = Color.FromArgb(0x00, 0x08, 0x58);
            NesPalette[0x02] = Color.FromArgb(0x00, 0x08, 0x78);
            NesPalette[0x03] = Color.FromArgb(0x00, 0x08, 0x70);
            NesPalette[0x04] = Color.FromArgb(0x38, 0x00, 0x50);
            NesPalette[0x05] = Color.FromArgb(0x58, 0x00, 0x10);
            NesPalette[0x06] = Color.FromArgb(0x58, 0x00, 0x00);
            NesPalette[0x07] = Color.FromArgb(0x40, 0x00, 0x00);
            NesPalette[0x08] = Color.FromArgb(0x10, 0x00, 0x00);
            NesPalette[0x09] = Color.FromArgb(0x00, 0x18, 0x00);
            NesPalette[0x0A] = Color.FromArgb(0x00, 0x1E, 0x00);
            NesPalette[0x0B] = Color.FromArgb(0x00, 0x1E, 0x00);
            NesPalette[0x0C] = Color.FromArgb(0x00, 0x18, 0x20);
            NesPalette[0x0D] = Color.FromArgb(0x00, 0x00, 0x00);
            NesPalette[0x0E] = Color.FromArgb(0x00, 0x00, 0x00);
            NesPalette[0x0F] = Color.FromArgb(0x00, 0x00, 0x00);

            NesPalette[0x10] = Color.FromArgb(0xA0, 0xA0, 0xA0);
            NesPalette[0x11] = Color.FromArgb(0x00, 0x48, 0xB8);
            NesPalette[0x12] = Color.FromArgb(0x08, 0x30, 0xE0);
            NesPalette[0x13] = Color.FromArgb(0x58, 0x18, 0xD8);
            NesPalette[0x14] = Color.FromArgb(0xA0, 0x08, 0xA8);
            NesPalette[0x15] = Color.FromArgb(0xD0, 0x00, 0x58);
            NesPalette[0x16] = Color.FromArgb(0xD0, 0x10, 0x00);
            NesPalette[0x17] = Color.FromArgb(0xA0, 0x20, 0x00);
            NesPalette[0x18] = Color.FromArgb(0x60, 0x40, 0x00);
            NesPalette[0x19] = Color.FromArgb(0x08, 0x58, 0x00);
            NesPalette[0x1A] = Color.FromArgb(0x00, 0x68, 0x00);
            NesPalette[0x1B] = Color.FromArgb(0x00, 0x68, 0x10);
            NesPalette[0x1C] = Color.FromArgb(0x00, 0x60, 0x70);
            //NesPalette[0x1D] = Color.FromArgb(0x00, 0x00, 0x00);
            //NesPalette[0x1E] = Color.FromArgb(0x00, 0x00, 0x00);
            //NesPalette[0x1F] = Color.FromArgb(0x00, 0x00, 0x00);

            //NesPalette[0x20] = Color.FromArgb(0xF8, 0xF8, 0xF8);
            NesPalette[0x21] = Color.FromArgb(0x20, 0xA0, 0xF8);
            NesPalette[0x22] = Color.FromArgb(0x50, 0x78, 0xF8);
            NesPalette[0x23] = Color.FromArgb(0x98, 0x68, 0xF8);
            NesPalette[0x24] = Color.FromArgb(0xF8, 0x68, 0xF8);
            NesPalette[0x25] = Color.FromArgb(0xF8, 0x70, 0xB0);
            NesPalette[0x26] = Color.FromArgb(0xF8, 0x70, 0x68);
            NesPalette[0x27] = Color.FromArgb(0xF8, 0x80, 0x18);
            NesPalette[0x28] = Color.FromArgb(0xC0, 0x98, 0x00);
            NesPalette[0x29] = Color.FromArgb(0x70, 0xB0, 0x00);
            NesPalette[0x2A] = Color.FromArgb(0x28, 0xC0, 0x20);
            NesPalette[0x2B] = Color.FromArgb(0x00, 0xC8, 0x70);
            NesPalette[0x2C] = Color.FromArgb(0x00, 0xC0, 0xD0);
            NesPalette[0x2D] = Color.FromArgb(0x28, 0x28, 0x28);
            NesPalette[0x2E] = Color.FromArgb(0x00, 0x00, 0x00);
            //NesPalette[0x2F] = Color.FromArgb(0x00, 0x00, 0x00);

            NesPalette[0x30] = Color.FromArgb(0xF8, 0xF8, 0xF8);
            NesPalette[0x31] = Color.FromArgb(0xA0, 0xD8, 0xF8);
            NesPalette[0x32] = Color.FromArgb(0xB0, 0xC0, 0xF8);
            NesPalette[0x33] = Color.FromArgb(0xD0, 0xB8, 0xF8);
            NesPalette[0x34] = Color.FromArgb(0xF8, 0xC0, 0xF8);
            NesPalette[0x35] = Color.FromArgb(0xF8, 0xC0, 0xE0);
            NesPalette[0x36] = Color.FromArgb(0xF8, 0xC0, 0xC0);
            NesPalette[0x37] = Color.FromArgb(0xF8, 0xC8, 0xA0);
            NesPalette[0x38] = Color.FromArgb(0xE8, 0xD8, 0x88);
            NesPalette[0x39] = Color.FromArgb(0xC8, 0xE0, 0x90);
            NesPalette[0x3A] = Color.FromArgb(0xA8, 0xE8, 0xA0);
            NesPalette[0x3B] = Color.FromArgb(0x90, 0xE8, 0xC8);
            NesPalette[0x3C] = Color.FromArgb(0x90, 0xE0, 0xE8);
            NesPalette[0x3D] = Color.FromArgb(0xA8, 0xA8, 0xA8);
            //NesPalette[0x3E] = Color.FromArgb(0x00, 0x00, 0x00);
            //NesPalette[0x3F] = Color.FromArgb(0x00, 0x00, 0x00);
            */

            var paletteJson = File.ReadAllText(paletteFile);
            var nesPaletteStr = JsonConvert.DeserializeObject<Dictionary<string, string>>(paletteJson);
            var NesPalette = nesPaletteStr.Select(kv => new KeyValuePair<byte, Color>(
                    kv.Key.ToLower().StartsWith("0x") ? (byte)Convert.ToInt32(kv.Key.Substring(2), 16) : byte.Parse(kv.Key),
                    ColorTranslator.FromHtml(kv.Value)
                )).ToDictionary(kv => kv.Key, kv => kv.Value);

            try
            {

                var images = new List<Image>();
                foreach (var image in imageFiles)
                {
                    Console.WriteLine($"Loading {image}...");
                    images.Add(Image.FromFile(image));
                }

                var patternTables = new List<PatternTableEntry>[2] { new List<PatternTableEntry>(), new List<PatternTableEntry>() };
                var paletteGroups = new List<List<Palette>>();
                for (int imageNum = 0; imageNum < images.Count; imageNum++)
                {
                    Console.WriteLine($"Creating palettes for {imageFiles[imageNum]}...");
                    var image = images[imageNum];

                    // Приводим все цвета к NES палитре
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var color = ((Bitmap)image).GetPixel(x, y);
                            var similarColor = NesPalette[findSimilarColor(NesPalette, color)];
                            ((Bitmap)image).SetPixel(x, y, similarColor);
                        }
                    }

                    // Счётчик использования палитр
                    Dictionary<Palette, int> paletteGroupCounter = new Dictionary<Palette, int>(new PaletteGroupComparer());
                    Color bgColor = ((Bitmap)image).GetPixel(0, 0);

                    // Перебираем все тайлы 16*16
                    for (int tileY = 0; tileY < image.Height / 16; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / 16; tileX++)
                        {
                            // Создаём палитру
                            var palette = createPalette(image, tileX, tileY, bgColor);

                            // Считаем количество использования таких палитр
                            if (!paletteGroupCounter.ContainsKey(palette))
                                paletteGroupCounter[palette] = 0;
                            paletteGroupCounter[palette]++;
                        }
                    }

                    // Группируем палитры. Некоторые из них могут содержать все цвета других
                    var paletteGroupGrouped = new Dictionary<Palette, int>(paletteGroupCounter, new PaletteGroupComparer());
                    foreach (var palette2 in paletteGroupCounter.Keys)
                        foreach (var palette1 in paletteGroupCounter.Keys)
                        {
                            if (!palette1.Equals(palette2) && paletteGroupGrouped.ContainsKey(palette2) && palette1.Contains(palette2))
                            {
                                if (!paletteGroupGrouped.ContainsKey(palette1))
                                    paletteGroupGrouped[palette1] = 0;
                                paletteGroupGrouped[palette1] += paletteGroupGrouped[palette2];
                                paletteGroupGrouped.Remove(palette2);
                            }
                        }

                    // Смотрим, какие палитры можно объединить
                    var paletteGroupMerged = new Dictionary<Palette, int>(paletteGroupGrouped, new PaletteGroupComparer());
                    foreach (var palette2 in paletteGroupGrouped.Keys)
                        foreach (var palette1 in paletteGroupGrouped.Keys)
                        {
                            if (!palette1.Equals(palette2) && palette1.Colors.Count < 4 && palette2.Colors.Count < 4)
                            {
                                var grcol = new List<Color>();
                                foreach (var color in palette1.Colors)
                                    if (!grcol.Contains(color))
                                        grcol.Add(color);
                                foreach (var color in palette2.Colors)
                                    if (!grcol.Contains(color))
                                        grcol.Add(color);
                                if (grcol.Count <= 4) // Успешно поместились
                                {
                                    paletteGroupMerged[new Palette(grcol)] =
                                        paletteGroupGrouped[palette1] + paletteGroupGrouped[palette2];
                                    paletteGroupMerged.Remove(palette1);
                                    paletteGroupMerged.Remove(palette2);
                                }
                            }
                        }

                    // Здесь можно убрать лишние палитры, если их много, не зря же мы их считали. Хотя в нашем случае зря.
                    var paletteGroup = new List<Palette>((from palette in paletteGroupMerged.Keys
                                                          orderby paletteGroupMerged[palette] descending
                                                          select palette).Take(4).ToArray());

                    /*
                    foreach (var pal in paletteGroup)
                    {
                        foreach (var col in pal.Colors)
                            Console.Write(col + " ");
                        Console.WriteLine();
                    }
                    */

                    paletteGroups.Add(paletteGroup);
                }

                // Перераспределяем и поглощаем палитры
                for (int i = 1; i < paletteGroups.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        var paletteGroup1 = paletteGroups[j];
                        var paletteGroup2 = paletteGroups[i];
                        for (int p2 = 0; p2 < paletteGroup2.Count; p2++)
                        {
                            for (int p1 = 0; p1 < paletteGroup1.Count; p1++)
                            {
                                var palette1 = paletteGroup1[p1];
                                var palette2 = paletteGroup2[p2];

                                if (palette1.Contains(palette2) && (p1 <= paletteGroup2.Count))
                                {
                                    paletteGroup2.RemoveAt(p2);
                                    paletteGroup2.Insert(p1, palette1);
                                }
                                else if (palette2.Contains(palette1) && (p1 <= paletteGroup2.Count))
                                {
                                    paletteGroup2.RemoveAt(p2);
                                    paletteGroup2.Insert(p1, palette2);
                                    paletteGroup1.RemoveAt(p1);
                                    paletteGroup1.Insert(p1, palette2);
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < paletteGroups.Count; i++)
                {
                    Console.WriteLine($"Palettes for {imageFiles[i]}:");
                    var paletteGroup = paletteGroups[i];
                    foreach (var pal in paletteGroup)
                    {
                        foreach (var col in pal.Colors)
                            Console.Write(col + " ");
                        Console.WriteLine();
                    }
                }

                // Сохраняем и применяем палитры
                for (int imageNum = 0; imageNum < images.Count; imageNum++)
                {
                    Console.WriteLine($"Mapping colors for {imageFiles[imageNum]}...");
                    var image = images[imageNum];
                    // Палитра
                    var palettes = paletteGroups[imageNum];
                    var paletteRaw = new byte[16];
                    paletteRaw[0] = paletteRaw[4] = paletteRaw[8] = paletteRaw[12] =
                        //paletteRaw[16] = paletteRaw[20] = paletteRaw[24] = paletteRaw[28] =
                        (byte)findSimilarColor(NesPalette, palettes[0].Colors[0]);
                    for (int p = 0; p < palettes.Count; p++)
                    {
                        if (palettes[p] != null)
                            for (int c = 1; c < palettes[p].Colors.Count; c++)
                            {
                                paletteRaw[p * 4 + c] = (byte)findSimilarColor(NesPalette, palettes[p].Colors[c]);
                            }
                    }
                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(imageFiles[imageNum]) + "_palette.bin", paletteRaw);

                    // Перебираем все тайлы 16*16
                    var palleteIndexes = new byte[image.Width / 16, image.Height / 16];
                    for (int tileY = 0; tileY < image.Height / 16; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / 16; tileX++)
                        {
                            double minDifference = double.MaxValue;
                            Palette bestPalette = null;
                            byte bestPaletteIndex = 0;
                            // Пробуем каждую палитру
                            for (byte paletteIndex = 0; paletteIndex < palettes.Count; paletteIndex++)
                            {
                                if (palettes[paletteIndex] == null) continue;
                                double difference = 0;
                                for (int y = 0; y < 16; y++)      // И применяем к каждому пикселю
                                    for (int x = 0; x < 16; x++)
                                    {
                                        var color = ((Bitmap)image).GetPixel(tileX * 16 + x, tileY * 16 + y);
                                        var similarColor = findSimilarColor(palettes[paletteIndex].Colors, color);
                                        // Вычисляем разницу в цвете с макисмально похожим цветом
                                        var delta = getColorDifference(color, similarColor);
                                        // И суммируем
                                        difference += delta;
                                    }
                                // Ищем палитру, которая встанет с минимумом изменений
                                if (difference < minDifference)
                                {
                                    minDifference = difference;
                                    bestPalette = palettes[paletteIndex];
                                    bestPaletteIndex = paletteIndex;
                                }
                            }
                            if (minDifference > 0)
                                throw new Exception("Can't match color"); // На всякий случай
                                                                          // Запоминаем номер палитры
                            palleteIndexes[tileX, tileY] = bestPaletteIndex;

                            // В итоге применяем эту палитру к тайлу
                            for (int y = 0; y < 16; y++)
                            {
                                for (int x = 0; x < 16; x++)
                                {
                                    var color = ((Bitmap)image).GetPixel(tileX * 16 + x, tileY * 16 + y);
                                    var similarColor = findSimilarColor(bestPalette.Colors, color);
                                    ((Bitmap)image).SetPixel(tileX * 16 + x, tileY * 16 + y, similarColor);
                                }
                            }
                        } // tileX
                    } // tile Y

                    // Осталось составить базу тайлов, теперь уже размером 8 на 8
                    var nameTable = new byte[32 * 30];
                    for (int tileY = 0; tileY < image.Height / 8; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / 8; tileX++)
                        {
                            var tileData = new byte[8, 8];
                            for (int y = 0; y < 8; y++)      // И применяем к каждому пикселю
                                for (int x = 0; x < 8; x++)
                                {
                                    var color = ((Bitmap)image).GetPixel(tileX * 8 + x, tileY * 8 + y);
                                    var palette = palettes[palleteIndexes[tileX / 2, tileY / 2]];
                                    var colorIndex = (byte)palette.Colors.FindIndex(c => c == color);
                                    tileData[x, y] = colorIndex;
                                }
                            // Создаём тайл на основе массива с номерами цветов (палитра при этом не важна)
                            var tile = new PatternTableEntry(tileData);
                            // Добавляем его в список, если его там ещё нет
                            // Выбираем первый набор для первых трёх картинок и нижней/верхней части, второй - для остального
                            if ((imageNum == 0 || (imageNum < 4 && tileY < 20) || tileY <= 8) && (imageNum != 6))
                            {
                                if (!patternTables[0].Contains(tile))
                                    patternTables[0].Add(tile);
                                // Запоминаем номер тайла
                                nameTable[tileX + tileY * 32] = (byte)patternTables[0].FindIndex(t => t.Equals(tile));
                            }
                            else
                            {
                                if (!patternTables[1].Contains(tile))
                                    patternTables[1].Add(tile);
                                // Запоминаем номер тайла
                                nameTable[tileX + tileY * 32] = (byte)patternTables[1].FindIndex(t => t.Equals(tile));
                            }
                        }
                    }

                    // Ну и nametable
                    var nametableRaw = new byte[1024];
                    Array.Copy(nameTable, nametableRaw, 30 * 32);
                    // В которой ещё attribute table
                    for (int tileY = 0; tileY <= image.Height / 32; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / 32; tileX++)
                        {
                            var topLeft = palleteIndexes[tileX * 2, tileY * 2];
                            var topRight = palleteIndexes[tileX * 2 + 1, tileY * 2];
                            var bottomLeft = tileY < 7 ? palleteIndexes[tileX * 2, tileY * 2 + 1] : 0;
                            var bottomRight = tileY < 7 ? palleteIndexes[tileX * 2 + 1, tileY * 2 + 1] : 0;

                            nametableRaw[0x3C0 + tileY * 8 + tileX] = (byte)
                                (topLeft // top left
                                | (topRight << 2) // top right
                                | (bottomLeft << 4) // bottom left
                                | (bottomRight << 6)); // bottom right
                        }
                    }
                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(imageFiles[imageNum]) + "_nametable.bin", nametableRaw);
                }

                // Сохраняем паттерны
                for (int i = 0; i < 2; i++)
                    Console.WriteLine($"Tiles count {i + 1}: {patternTables[i].Count}");
                for (int i = 0; i < 2; i++)
                {
                    if (patternTables[i].Count > 256) throw new Exception($"Too many tiles {i + 1}: {patternTables[i].Count}");

                    // Сами тайлы
                    var patternTableRaw = new byte[0x1000];
                    for (int p = 0; p < patternTables[i].Count; p++)
                    {
                        var pixels = patternTables[i][p].pixels;

                        for (int y = 0; y < 8; y++)
                        {
                            patternTableRaw[p * 16 + y] = 0;
                            patternTableRaw[p * 16 + y + 8] = 0;
                            for (int x = 0; x < 8; x++)
                            {
                                if ((pixels[x, y] & 1) != 0)
                                    patternTableRaw[p * 16 + y] |= (byte)(1 << (7 - x));
                                if ((pixels[x, y] & 2) != 0)
                                    patternTableRaw[p * 16 + y + 8] |= (byte)(1 << (7 - x));
                            }
                        }
                    }
                    File.WriteAllBytes($"pattern{i}.bin", patternTableRaw);
                }

                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + ex.StackTrace);
                Environment.Exit(1);
            }
        }

        static byte findSimilarColor(Dictionary<byte, Color> colors, Color color)
        {
            byte result = byte.MaxValue;
            int minDelta = int.MaxValue;
            if (color.R == 0xFF && color.G == 0xFF && color.B == 0xFF)
                color = Color.FromArgb(0xF8, 0xF8, 0xF8);
            foreach (var index in colors.Keys)
            {
                var color1 = color;
                var color2 = colors[index];
                var deltaR = Math.Abs((int)color1.R - (int)color2.R);
                var deltaG = Math.Abs((int)color1.G - (int)color2.G);
                var deltaB = Math.Abs((int)color1.B - (int)color2.B);
                var delta = deltaR + deltaG + deltaB;
                if (delta == 0 && delta < minDelta)
                {
                    minDelta = delta;
                    result = index;
                }
            }
            if (result == byte.MaxValue)
                throw new KeyNotFoundException("Invalid color: " + color.ToString());
            return result;
        }

        static Palette createPalette(Image image, int tileX, int tileY, Color bgColor)
        {
            Dictionary<Color, int> colorCounter = new Dictionary<Color, int>();
            colorCounter[bgColor] = 0;
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    var color = ((Bitmap)image).GetPixel(tileX * 16 + x, tileY * 16 + y);
                    if (!colorCounter.ContainsKey(color)) colorCounter[color] = 0;
                    colorCounter[color]++;
                }

            if (colorCounter.Count > 4)
                throw new Exception("Too many colors");

            // Создаём палитру
            var paletteGroup = new Palette(colorCounter.Keys);
            return paletteGroup;
        }

        static Color findSimilarColor(IEnumerable<Color> colors, Color color)
        {
            Color result = Color.Black;
            double minDelta = int.MaxValue;
            foreach (var c in colors)
            {
                var color1 = color;
                var color2 = c;
                var delta = getColorDifference(color1, color2);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    result = c;
                }
            }
            return result;
        }

        static double getColorDifference(Color color1, Color color2)
        {
            /*
            var deltaR = Math.Abs((int)color1.R - (int)color2.R);
            var deltaG = Math.Abs((int)color1.G - (int)color2.G);
            var deltaB = Math.Abs((int)color1.B - (int)color2.B);
            var delta = deltaR + deltaG + deltaB;
            */
            var delta = Math.Sqrt(Math.Pow(color1.R - color2.R, 2) + Math.Pow(color1.G - color2.G, 2) + Math.Pow(color1.B - color2.B, 2));
            return delta;
        }
    }
}
