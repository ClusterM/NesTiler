using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace com.clusterrr.Famicom.NesTiler
{
    class Program
    {
        public enum TilesMode
        {
            Backgrounds,
            Sprites,
            Sprites8x16
        }

        static int Main(string[] args)
        {
            try
            {
                string colorsFile = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), @"colors.json");
                var imageFiles = new Dictionary<int, string>();
                Color? bgColor = null;
                var paletteEnabled = new bool[4] { true, true, true, true };
                var fixedPalettes = new Palette[4] { null, null, null, null };
                var mode = TilesMode.Backgrounds;
                int tilePalWidth = 16;
                int tilePalHeight = 16;
                var imagesOriginal = new Dictionary<int, FastBitmap>();
                var imagesRecolored = new Dictionary<int, FastBitmap>();
                var palleteIndexes = new Dictionary<int, byte[,]>();
                var patternTableStartOffsets = new Dictionary<int, int>();
                var patternTables = new Dictionary<int, Dictionary<int, Tile>>();
                var nameTables = new Dictionary<int, List<int>>();
                bool ignoreTilesRange = false;

                // Filenames
                var outPreview = new Dictionary<int, string>();
                var outPalette = new Dictionary<int, string>();
                var outPatternTable = new Dictionary<int, string>();
                var outNameTable = new Dictionary<int, string>();
                var outAttributeTable = new Dictionary<int, string>();

                var paramRegex = new Regex(@"--*(?<param>[a-zA-Z-]+)(?<index>[0-9]*)");
                for (int i = 0; i < args.Length; i++)
                {
                    var match = paramRegex.Match(args[i]);
                    string param = match.Groups["param"].Value;
                    string indexStr = match.Groups["index"].Value;
                    int indexNum = 0;
                    int.TryParse(indexStr, out indexNum);
                    string value = i < args.Length - 1 ? args[i + 1] : "";
                    switch (param)
                    {
                        case "mode":
                            switch (value.ToLower())
                            {
                                case "sprite":
                                case "sprites":
                                    mode = TilesMode.Sprites;
                                    tilePalWidth = 8;
                                    tilePalHeight = 8;
                                    break;
                                case "sprite8x16":
                                case "sprites8x16":
                                    mode = TilesMode.Sprites;
                                    tilePalWidth = 8;
                                    tilePalHeight = 16;
                                    break;
                                case "bg":
                                case "background":
                                case "backgrounds":
                                    mode = TilesMode.Backgrounds;
                                    tilePalWidth = 16;
                                    tilePalHeight = 16;
                                    break;
                                default:
                                    throw new ArgumentException("Invalid mode", "mode");
                            }
                            i++;
                            break;
                        case "i":
                        case "input":
                            imageFiles[indexNum] = value;
                            i++;
                            break;
                        case "pattern-offset":
                            patternTableStartOffsets[indexNum] = int.Parse(value);
                            i++;
                            break;
                        case "colors":
                            colorsFile = value;
                            i++;
                            break;
                        case "bgcolor":
                            bgColor = ColorTranslator.FromHtml(value);
                            i++;
                            break;
                        case "enable-palettes":
                            {
                                var enabled = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                for (int p = 0; p < paletteEnabled.Length; p++)
                                    paletteEnabled[p] = enabled.Contains($"{p}");
                            }
                            i++;
                            break;
                        case "palette":
                            {
                                var colors = value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(c => ColorTranslator.FromHtml(c));
                                fixedPalettes[indexNum] = new Palette(colors);
                            }
                            i++;
                            break;
                        case "out-preview":
                            outPreview[indexNum] = value;
                            i++;
                            break;
                        case "out-palette":
                            outPalette[indexNum] = value;
                            i++;
                            break;
                        case "out-pattern-table":
                            outPatternTable[indexNum] = value;
                            i++;
                            break;
                        case "out-name-table":
                            outNameTable[indexNum] = value;
                            i++;
                            break;
                        case "out-attribute-table":
                            outAttributeTable[indexNum] = value;
                            i++;
                            break;
                        case "ignoretilesrange":
                        case "ignore-tiles-range":
                            ignoreTilesRange = true;
                            break;
                        default:
                            throw new ArgumentException($"Unknown argement: {args[i]}");
                    }
                }

                // Loading and parsing palette JSON
                var paletteJson = File.ReadAllText(colorsFile);
                var nesColorsStr = JsonSerializer.Deserialize<Dictionary<string, string>>(paletteJson);
                var nesColors = nesColorsStr.Select(kv => new KeyValuePair<byte, Color>(
                        kv.Key.ToLower().StartsWith("0x") ? (byte)Convert.ToInt32(kv.Key.Substring(2), 16) : byte.Parse(kv.Key),
                        ColorTranslator.FromHtml(kv.Value)
                    )).ToDictionary(kv => kv.Key, kv => kv.Value);

                /*
                var img = new Bitmap(16*64, 4*64, PixelFormat.Format32bppPArgb);
                var cnv = Graphics.FromImage(img);
                for (int x = 0; x < 16; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        if (nesColors.ContainsKey((byte)(y * 16 + x)))
                        {
                            var brush = new SolidBrush(nesColors[(byte)(y * 16 + x)]);
                            cnv.FillRectangle(brush, x * 64, y * 64, 64, 64);
                        } else
                        {
                            var brush = new SolidBrush(Color.White);
                            cnv.FillRectangle(brush, x * 64, y * 64, 64, 64);
                            var red = new SolidBrush(Color.Red);
                            var pen = new Pen(red, 3);
                            cnv.DrawLine(pen, x * 64, y * 64, x * 64 + 63, y * 64 + 63);
                            cnv.DrawLine(pen, x * 64 + 63, y * 64, x * 64, y * 64 + 63);
                        }
                    }
                cnv.Flush();
                img.Save(@"E:\palette.png", ImageFormat.Png);
                return 0;
                */
                
                foreach (var image in imageFiles)
                {
                    Console.WriteLine($"Loading {Path.GetFileName(image.Value)}...");
                    imagesOriginal[image.Key] = FastBitmap.FromFile(image.Value);
                    if ((imagesOriginal[image.Key].Width % tilePalWidth != 0) || (imagesOriginal[image.Key].Height % tilePalHeight != 0))
                        throw new InvalidDataException("Invalid image size");
                }
                if (bgColor.HasValue)
                    bgColor = nesColors[findSimilarColor(nesColors, bgColor.Value)];

                //var patternTables = new List<PatternTableEntry>[2] { new List<PatternTableEntry>(), new List<PatternTableEntry>() };
                //var paletteGroups = new List<List<Palette>>();

                // Счётчик использования цветов
                var colorCounter = new Dictionary<Color, int>();
                foreach (var imageNum in imagesOriginal.Keys)
                {
                    Console.WriteLine($"Adjusting colors for {imageFiles[imageNum]}...");
                    var image = new FastBitmap(imagesOriginal[imageNum].GetBitmap());
                    imagesRecolored[imageNum] = image;
                    // Приводим все цвета к NES палитре
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var color = image.GetPixel(x, y);
                            var similarColor = nesColors[findSimilarColor(nesColors, color)];
                            image.SetPixel(x, y, similarColor);
                            if (!colorCounter.ContainsKey(similarColor))
                                colorCounter[similarColor] = 0;
                            colorCounter[similarColor]++;
                        }
                    }
                }

                // Определяем самый популярный цвет, если надо
                if (bgColor == null)
                {
                    Console.Write($"Background color autotect... ");
                    // Счётчик использования цветов
                    Dictionary<Color, int> colorPerTileCounter = new Dictionary<Color, int>();
                    foreach (var imageNum in imagesRecolored.Keys)
                    {
                        var image = imagesRecolored[imageNum];
                        // Перебираем все тайлы 16*16 или 8*8 для спрайтов
                        for (int tileY = 0; tileY < image.Height / tilePalHeight; tileY++)
                        {
                            for (int tileX = 0; tileX < image.Width / tilePalWidth; tileX++)
                            {
                                var colorsInTile = new List<Color>();
                                for (int y = 0; y < tilePalHeight; y++)
                                {
                                    for (int x = 0; x < tilePalWidth; x++)
                                    {
                                        var color = image.GetPixel(tileX * tilePalWidth + x, tileY * tilePalHeight + y);
                                        if (!colorsInTile.Contains(color))
                                            colorsInTile.Add(color);
                                    }
                                }

                                // Count each color only once per tile
                                foreach (var color in colorsInTile)
                                {
                                    if (!colorPerTileCounter.ContainsKey(color))
                                        colorPerTileCounter[color] = 0;
                                    colorPerTileCounter[color]++;
                                }
                            }
                        }
                    }
                    bgColor = colorPerTileCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).FirstOrDefault();
                    Console.WriteLine(ColorTranslator.ToHtml(bgColor.Value));
                }

                // Счётчик использования палитр
                Dictionary<Palette, int> paletteCounter = new Dictionary<Palette, int>();
                foreach (var imageNum in imagesOriginal.Keys)
                {
                    Console.WriteLine($"Creating palettes for {imageFiles[imageNum]}...");
                    var image = imagesRecolored[imageNum];
                    // Перебираем все тайлы 16*16 или 8*8 для спрайтов
                    for (int tileY = 0; tileY < image.Height / tilePalHeight; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / tilePalWidth; tileX++)
                        {
                            // Создаём палитру
                            var palette = new Palette(
                                image, tileX * tilePalWidth, tileY * tilePalHeight,
                                tilePalWidth, tilePalHeight, bgColor.Value);

                            // Сразу отсеиваем те, которые входят в фиксированные
                            if (fixedPalettes.Where(p => p != null && p.Contains(palette)).Any())
                                // Считаем количество использования таких палитр
                                continue;

                            // Сотируем палитры по популярности
                            if (!paletteCounter.ContainsKey(palette))
                                paletteCounter[palette] = 0;
                            paletteCounter[palette]++;
                        }
                    }
                }

                Console.WriteLine($"Calculating final palette list...");
                var sortedKeys = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();
                // Группируем палитры. Некоторые из них могут содержать все цвета других
                foreach (var palette2 in sortedKeys)
                    foreach (var palette1 in sortedKeys)
                    {
                        if ((palette2 != palette1) && (palette2.Count >= palette1.Count) && palette2.Contains(palette1))
                        {
                            paletteCounter[palette2] += paletteCounter[palette1];
                            paletteCounter[palette1] = 0;
                        }
                    }

                // Убираем те, что больше не используются
                paletteCounter = paletteCounter.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
                // Снова сортируем палитры по популярности
                sortedKeys = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

                // Список топовых палитр
                var top4 = sortedKeys.Take(4).ToList();
                // Если есть свободные места в топовых палитрах, добиваем их менее топовыми
                foreach (var t in top4)
                {
                    if (t.Count < 3)
                    {
                        foreach (var p in sortedKeys)
                        {
                            var newColors = p.Where(c => !t.Contains(c));
                            if (p != t && (paletteCounter[p] > 0) && (newColors.Count() + t.Count <= 3))
                            {
                                var count1 = paletteCounter[t];
                                var count2 = paletteCounter[p];
                                paletteCounter[t] = 0;
                                paletteCounter[p] = 0;
                                foreach (var c in newColors) t.Add(c);
                                paletteCounter[t] = count1 + count2;
                            }
                        }
                    }
                }

                // Убираем те, что больше не используются
                paletteCounter = paletteCounter.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
                // Снова сортируем палитры по популярности
                sortedKeys = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

                // Снова группируем палитры. Некоторые из них могут содержать все цвета других
                foreach (var palette2 in sortedKeys)
                    foreach (var palette1 in sortedKeys)
                    {
                        if ((palette2 != palette1) && (palette2.Count >= palette1.Count) && palette2.Contains(palette1))
                        {
                            paletteCounter[palette2] += paletteCounter[palette1];
                            paletteCounter[palette1] = 0;
                        }
                    }


                // Убираем те, что больше не используются
                paletteCounter = paletteCounter.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
                // Снова сортируем палитры по популярности
                sortedKeys = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

                // Обновляем список топовых
                top4 = sortedKeys.Take(4).ToList();
                // Выбираем итоговые палитры
                var palettes = new Palette[4] { null, null, null, null };
                for (var i = 0; i < palettes.Length; i++)
                {
                    if (paletteEnabled[i])
                    {
                        if (fixedPalettes[i] != null)
                        {
                            palettes[i] = fixedPalettes[i];
                        }
                        else if (top4.Any())
                        {
                            if (top4.Any())
                            {
                                palettes[i] = top4.First();
                                top4.RemoveAt(0);
                            }
                            else
                            {
                                palettes[i] = new Palette();
                            }
                        }

                        if (palettes[i] != null)
                            Console.WriteLine($"Palette #{i}: {ColorTranslator.ToHtml(bgColor.Value)}(BG) {string.Join(" ", palettes[i].Select(p => ColorTranslator.ToHtml(p)))}");
                    }
                }

                // Палитра в сыром виде
                var bgColorId = findSimilarColor(nesColors, bgColor.Value);
                for (int p = 0; p < palettes.Length; p++)
                {
                    if (paletteEnabled[p] && outPalette.ContainsKey(p))
                    {
                        var paletteRaw = new byte[4];
                        paletteRaw[0] = bgColorId;
                        for (int c = 1; c <= 3; c++)
                        {
                            if (palettes[p] == null)
                                paletteRaw[c] = 0;
                            else if (palettes[p][c].HasValue)
                                paletteRaw[c] = findSimilarColor(nesColors, palettes[p][c].Value);
                        }
                        File.WriteAllBytes(outPalette[p], paletteRaw);
                        Console.WriteLine($"Palette #{p} saved to {outPalette[p]}");
                    }
                }
                
                // Сохраняем и применяем палитры
                imagesRecolored.Clear();
                foreach (var imageNum in imagesOriginal.Keys)
                {
                    Console.WriteLine($"Mapping palettes for #{imageNum} {imageFiles[imageNum]}...");
                    var image = imagesOriginal[imageNum];
                    var imageRecolored = new FastBitmap(image.GetBitmap());
                    imagesRecolored[imageNum] = imageRecolored;
                    // Перебираем все тайлы 16*16 или спрайты 8*8 (8*16)
                    palleteIndexes[imageNum] = new byte[image.Width / tilePalWidth, image.Height / tilePalHeight];
                    for (int tileY = 0; tileY < image.Height / tilePalHeight; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / tilePalWidth; tileX++)
                        {
                            double minDelta = double.MaxValue;
                            byte bestPaletteIndex = 0;
                            // Пробуем каждую палитру
                            for (byte paletteIndex = 0; paletteIndex < palettes.Length; paletteIndex++)
                            {
                                if (palettes[paletteIndex] == null) continue;
                                double delta = palettes[paletteIndex].GetTileDelta(
                                    image, tileX * tilePalWidth, tileY * tilePalHeight,
                                    tilePalWidth, tilePalHeight, bgColor.Value);
                                // Ищем палитру, которая встанет с минимумом изменений
                                if (delta < minDelta)
                                {
                                    minDelta = delta;
                                    bestPaletteIndex = paletteIndex;
                                }
                            }
                            Palette bestPalette = palettes[bestPaletteIndex];
                            palleteIndexes[imageNum][tileX, tileY] = bestPaletteIndex;

                            // В итоге применяем эту палитру к тайлу
                            for (int y = 0; y < tilePalHeight; y++)
                            {
                                for (int x = 0; x < tilePalWidth; x++)
                                {
                                    var color = image.GetPixel(tileX * tilePalWidth + x, tileY * tilePalHeight + y);
                                    var similarColor = findSimilarColor(Enumerable.Concat(bestPalette, new Color[] { bgColor.Value }), color);
                                    imageRecolored.SetPixel(tileX * tilePalWidth + x, tileY * tilePalHeight + y, similarColor);
                                }
                            }
                        } // tileX
                    } // tile Y

                    if (outPreview.ContainsKey(imageNum))
                    {
                        imageRecolored.Save(outPreview[imageNum], ImageFormat.Png);
                        Console.WriteLine($"Preview #{imageNum} saved to {outPreview[imageNum]}");
                    }
                }

                // Осталось составить базу тайлов, теперь уже размером 8 на 8
                foreach (var imageNum in imagesRecolored.Keys)
                {
                    Console.WriteLine($"Creating pattern table for #{imageNum} {Path.GetFileName(imageFiles[imageNum])}...");
                    var image = imagesRecolored[imageNum];
                    if (!patternTables.ContainsKey(imageNum)) patternTables[imageNum] = new Dictionary<int, Tile>();
                    var patternTable = patternTables[imageNum];
                    if (!nameTables.ContainsKey(imageNum)) nameTables[imageNum] = new List<int>();
                    var nameTable = nameTables[imageNum];
                    if (!patternTableStartOffsets.ContainsKey(imageNum))
                        patternTableStartOffsets[imageNum] = 0;
                    var tileID = patternTableStartOffsets[imageNum];

                    for (int tileY = 0; tileY < image.Height / 8; tileY += (mode == TilesMode.Sprites8x16 ? 2 : 1))
                    {
                        for (int tileX = 0; tileX < image.Width / 8; tileX++)
                        {
                            for (int tileYS = 0; tileYS < (mode == TilesMode.Sprites8x16 ? 2 : 1); tileYS++)
                            {
                                var tileData = new byte[8, 8];
                                for (int y = 0; y < 8; y++)      // И применяем к каждому пикселю
                                    for (int x = 0; x < 8; x++)
                                    {
                                        var color = image.GetPixel(tileX * 8 + x, (tileY + tileYS) * 8 + y);
                                        var palette = palettes[palleteIndexes[imageNum][tileX / (tilePalWidth / 8), (tileY + tileYS) / (tilePalHeight / 8)]];
                                        byte colorIndex = 0;
                                        if (color != bgColor)
                                        {
                                            colorIndex = 1;
                                            while (palette[colorIndex] != color) colorIndex++;
                                        }
                                        tileData[x, y] = colorIndex;
                                    }
                                // Создаём тайл на основе массива с номерами цветов (палитра при этом не важна)
                                var tile = new Tile(tileData);
                                // Добавляем его в список, если его там ещё нет
                                var existsTile = patternTable.Where(kv => kv.Value.Equals(tile));
                                if (existsTile.Any())
                                {
                                    var id = existsTile.First().Key;
                                    nameTable.Add(id);
                                }
                                else
                                {
                                    patternTable[tileID] = tile;
                                    nameTable.Add(tileID);
                                    tileID++;
                                }
                            }
                        }
                    }
                    if (tileID > patternTableStartOffsets[imageNum])
                        Console.WriteLine($"#{imageNum} tiles range: {patternTableStartOffsets[imageNum]}-{tileID - 1}");
                    else
                        Console.WriteLine($"Pattern table is empty");
                    if (tileID > 256 && !ignoreTilesRange) 
                        throw new ArgumentOutOfRangeException("Tiles out of range");

                    // Saving to file
                    if (outPatternTable.ContainsKey(imageNum))
                    {
                        var patternTableRaw = new List<byte>();
                        for (int t = patternTableStartOffsets[imageNum]; t < tileID; t++)
                        {
                            var raw = new byte[16];
                            var pixels = patternTable[t].pixels;
                            for (int y = 0; y < 8; y++)
                            {
                                raw[y] = 0;
                                raw[y + 8] = 0;
                                for (int x = 0; x < 8; x++)
                                {
                                    if ((pixels[x, y] & 1) != 0)
                                        raw[y] |= (byte)(1 << (7 - x));
                                    if ((pixels[x, y] & 2) != 0)
                                        raw[y + 8] |= (byte)(1 << (7 - x));
                                }
                            }
                            patternTableRaw.AddRange(raw);
                        }
                        File.WriteAllBytes(outPatternTable[imageNum], patternTableRaw.ToArray());
                        Console.WriteLine($"Pattern table #{imageNum} saved to {outPatternTable[imageNum]}");
                    }

                    if (outNameTable.ContainsKey(imageNum))
                    {
                        File.WriteAllBytes(outNameTable[imageNum], nameTable.Select(i => (byte)i).ToArray());
                        Console.WriteLine($"Name table #{imageNum} saved to {outPatternTable[imageNum]}");
                    }
                }

                // Attribute tables
                // Осталось составить базу тайлов, теперь уже размером 8 на 8
                foreach (var imageNum in outAttributeTable.Keys)
                {
                    if (mode != TilesMode.Backgrounds)
                        throw new InvalidOperationException("Attribute table generation available for backgrounds mode only");
                    Console.WriteLine($"Creating attribute table for #{imageNum} {Path.GetFileName(imageFiles[imageNum])}...");
                    var image = imagesOriginal[imageNum];
                    var attributeTableRaw = new List<byte>();
                    for (int ptileY = 0; ptileY < Math.Ceiling(image.Height / 32.0); ptileY++)
                    {
                        for (int ptileX = 0; ptileX < Math.Ceiling(image.Width / 32.0); ptileX++)
                        {
                            byte topLeft = 0;
                            byte topRight = 0;
                            byte bottomLeft = 0;
                            byte bottomRight = 0;

                            try
                            {
                                topLeft = palleteIndexes[imageNum][ptileX * 2, ptileY * 2];
                            }
                            catch (IndexOutOfRangeException) { }
                            try
                            {
                                topRight = palleteIndexes[imageNum][ptileX * 2 + 1, ptileY * 2];
                            }
                            catch (IndexOutOfRangeException) { }
                            try
                            {
                                bottomLeft = palleteIndexes[imageNum][ptileX * 2, ptileY * 2 + 1];
                            }
                            catch (IndexOutOfRangeException) { }
                            try
                            {
                                bottomRight = palleteIndexes[imageNum][ptileX * 2 + 1, ptileY * 2 + 1];
                            }
                            catch (IndexOutOfRangeException) { }

                            var atv = (byte)
                                (topLeft // top left
                                | (topRight << 2) // top right
                                | (bottomLeft << 4) // bottom left
                                | (bottomRight << 6)); // bottom right
                            attributeTableRaw.Add(atv);
                        }
                    }

                    // Saving to file
                    if (outAttributeTable.ContainsKey(imageNum))
                    {
                        File.WriteAllBytes(outAttributeTable[imageNum], attributeTableRaw.ToArray());
                        Console.WriteLine($"Attribute table #{imageNum} saved to {outAttributeTable[imageNum]}");
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.GetType()}: " + ex.Message + ex.StackTrace);
                return 1;
            }
        }

        static byte findSimilarColor(Dictionary<byte, Color> colors, Color color)
        {
            byte result = byte.MaxValue;
            double minDelta = double.MaxValue;
            foreach (var index in colors.Keys)
            {
                var delta = color.GetDelta(colors[index]);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    result = index;
                }
            }
            if (result == byte.MaxValue)
                throw new KeyNotFoundException("Invalid color: " + color.ToString());
            return result;
        }

        static Color findSimilarColor(IEnumerable<Color> colors, Color color)
        {
            Color result = Color.Black;
            double minDelta = double.MaxValue;
            foreach (var c in colors)
            {
                var delta = color.GetDelta(c);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    result = c;
                }
            }
            return result;
        }
    }
}
