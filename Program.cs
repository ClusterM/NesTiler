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
        private const string REPO_PATH = "https://github.com/ClusterM/nestiler";
        const string DEFAULT_COLORS_FILE = @"nestiler-colors.json";
        private static DateTime BUILD_TIME = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(Properties.Resources.buildtime.Trim()));

        public enum TilesMode
        {
            Backgrounds,
            Sprites,
            Sprites8x16
        }

        static void PrintHelp()
        {
            Console.WriteLine($"Usage: {Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)} <options>");
            Console.WriteLine();
            Console.WriteLine("Available options:");
            Console.WriteLine(" {0,-40}{1}", "--input-<#> <file>[:offset[:height]]", "input file number #, optionally cropped vertically");
            Console.WriteLine(" {0,-40}{1}", "--colors <file>", $"JSON file with list of available colors (defaukt - {DEFAULT_COLORS_FILE})");
            Console.WriteLine(" {0,-40}{1}", "--mode bg|sprite8x8|sprite8x16", "mode: backgrounds, 8x8 sprites or 8x16 sprites (default - bg)");
            Console.WriteLine(" {0,-40}{1}", "--bg-color <color>", "background color in HTML color format (default - autodetected)");
            Console.WriteLine(" {0,-40}{1}", "--enable-palettes <palettes>", "zero-based comma separated list of palette numbers to use (default - 0,1,2,3)");
            Console.WriteLine(" {0,-40}{1}", "--palette-<#>", "comma separated list of colors to use in palette number # (default - auto)");
            Console.WriteLine(" {0,-40}{1}", "--pattern-offset-<#>", "first tile ID for pattern table for file number # (default - 0)");
            Console.WriteLine(" {0,-40}{1}", "--ignore-tiles-range", "option to disable tile ID overflow check");
            Console.WriteLine(" {0,-40}{1}", "--out-preview-<#> <file.png>", "output filename for preview of image number #");
            Console.WriteLine(" {0,-40}{1}", "--out-palette-<#> <file>", "output filename for palette number #");
            Console.WriteLine(" {0,-40}{1}", "--out-pattern-table-<#> <file>", "output filename for pattern table of image number #");
            Console.WriteLine(" {0,-40}{1}", "--out-name-table-<#> <file>", "output filename for nametable of image number #");
            Console.WriteLine(" {0,-40}{1}", "--out-attribute-table-<#> <file>", "output filename for attribute table of image number #");
        }

        static int Main(string[] args)
        {
            Console.WriteLine($"NesTiler v{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}");
            Console.WriteLine($"  Commit {Properties.Resources.gitCommit} @ {REPO_PATH}");
#if DEBUG
            Console.WriteLine($"  Debug version, build time: {BUILD_TIME.ToLocalTime()}");
#endif
            Console.WriteLine("  (c) Alexey 'Cluster' Avdyukhin / https://clusterrr.com / clusterrr@clusterrr.com");
            Console.WriteLine("");
            try
            {
                if (args.Length == 0 || args.Contains("help") || args.Contains("--help"))
                {
                    PrintHelp();
                    return 0;
                }

                string colorsFile = Path.Combine(AppContext.BaseDirectory, DEFAULT_COLORS_FILE);
                if (!File.Exists(colorsFile) && !OperatingSystem.IsWindows())
                    colorsFile = Path.Combine("/etc", DEFAULT_COLORS_FILE);
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

                var paramRegex = new Regex(@"^--?(?<param>[a-zA-Z-]+)(?<index>[0-9]*)$");
                for (int i = 0; i < args.Length; i++)
                {
                    var match = paramRegex.Match(args[i]);
                    if (!match.Success)
                        throw new ArgumentException($"Unknown argument: {args[i]}");
                    string param = match.Groups["param"].Value;
                    if (param[^1] == '-')
                        param = param[0..^1];
                    string indexStr = match.Groups["index"].Value;
                    int indexNum = 0;
                    if (!string.IsNullOrEmpty(indexStr))
                        indexNum = int.Parse(indexStr);
                    string value = i < args.Length - 1 ? args[i + 1] : "";
                    switch (param)
                    {
                        case "i":
                        case "input":
                            imageFiles[indexNum] = value;
                            i++;
                            break;
                        case "colors":
                            colorsFile = value;
                            i++;
                            break;
                        case "mode":
                            switch (value.ToLower())
                            {
                                case "sprite":
                                case "sprites":
                                case "sprites8x8":
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
                        case "bgcolor":
                        case "bg-color":
                        case "background-color":
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
                        case "pattern-offset":
                            patternTableStartOffsets[indexNum] = int.Parse(value);
                            i++;
                            break;
                        case "out-preview":
                        case "output-preview":
                            outPreview[indexNum] = value;
                            i++;
                            break;
                        case "out-palette":
                        case "output-palette":
                            outPalette[indexNum] = value;
                            i++;
                            break;
                        case "out-pattern-table":
                        case "output-pattern-table":
                            outPatternTable[indexNum] = value;
                            i++;
                            break;
                        case "out-name-table":
                        case "output-name-table":
                        case "out-nametable":
                        case "output-nametable":
                            outNameTable[indexNum] = value;
                            i++;
                            break;
                        case "out-attribute-table":
                        case "output-attribute-table":
                            outAttributeTable[indexNum] = value;
                            i++;
                            break;
                        case "ignoretilesrange":
                        case "ignore-tiles-range":
                            ignoreTilesRange = true;
                            break;
                        default:
                            throw new ArgumentException($"Unknown argument: {args[i]}");
                    }
                }

                if (!imageFiles.Any())
                    throw new ArgumentException($"Unknown argument: at least one input file required");

                // Loading and parsing palette JSON
                var paletteJson = File.ReadAllText(colorsFile);
                var nesColorsStr = JsonSerializer.Deserialize<Dictionary<string, string>>(paletteJson);
                var nesColors = nesColorsStr.Select(kv => new KeyValuePair<byte, Color>(
                        kv.Key.ToLower().StartsWith("0x") ? (byte)Convert.ToInt32(kv.Key.Substring(2), 16) : byte.Parse(kv.Key),
                        ColorTranslator.FromHtml(kv.Value)
                    )).ToDictionary(kv => kv.Key, kv => kv.Value);

                // Change the fixed palettes to colors from the NES palette
                for (int i = 0; i < fixedPalettes.Length; i++)
                {
                    if (fixedPalettes[i] == null) continue;
                    var colorsInPalette = fixedPalettes[i].ToArray();
                    for (int j = 0; j < colorsInPalette.Length; j++)
                        colorsInPalette[j] = nesColors[FindSimilarColor(nesColors, colorsInPalette[j])];
                    fixedPalettes[i] = new Palette(colorsInPalette);
                }

                // Loading images
                foreach (var image in imageFiles)
                {
                    Console.WriteLine($"Loading file #{image.Key} - {Path.GetFileName(image.Value)}...");
                    var offsetRegex = new Regex(@"^(?<filename>.*?)(:(?<offset>[0-9]+)(:(?<height>[0-9]+))?)?$");
                    var match = offsetRegex.Match(image.Value);
                    var filename = match.Groups["filename"].Value;
                    imagesOriginal[image.Key] = FastBitmap.FromFile(filename);
                    var offsetS = match.Groups["offset"].Value;
                    var heightS = match.Groups["height"].Value;
                    // Crop it if need
                    if (!string.IsNullOrEmpty(offsetS))
                    {
                        int offset = int.Parse(offsetS);
                        int height = imagesOriginal[image.Key].Height - offset;
                        if (!string.IsNullOrEmpty(heightS))
                            height = int.Parse(heightS);
                        Console.WriteLine($"Cropping it to {offset}:{height}...");
                        var cropped = new Bitmap(imagesOriginal[image.Key].Width, height);
                        var gr = Graphics.FromImage(cropped);
                        gr.DrawImageUnscaledAndClipped(imagesOriginal[image.Key].GetBitmap(),
                            new Rectangle(0, -offset, imagesOriginal[image.Key].Width, imagesOriginal[image.Key].Height));
                        gr.Flush();
                        imagesOriginal[image.Key].Dispose();
                        imagesOriginal[image.Key] = new FastBitmap(cropped);
                    }
                    if ((imagesOriginal[image.Key].Width % tilePalWidth != 0) || (imagesOriginal[image.Key].Height % tilePalHeight != 0))
                        throw new InvalidDataException("Invalid image size");
                    // TODO: more image size checks
                }

                // Change all colors in the images to colors from the NES palette
                foreach (var imageNum in imagesOriginal.Keys)
                {
                    Console.WriteLine($"Adjusting colors for file #{imageNum} - {imageFiles[imageNum]}...");
                    var image = new FastBitmap(imagesOriginal[imageNum].GetBitmap());
                    imagesRecolored[imageNum] = image;
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var color = image.GetPixel(x, y);
                            var similarColor = nesColors[FindSimilarColor(nesColors, color)];
                            image.SetPixel(x, y, similarColor);
                        }
                    }
                }

                // Detect background color
                if (bgColor.HasValue)
                {
                    // Manually
                    bgColor = nesColors[FindSimilarColor(nesColors, bgColor.Value)];
                }
                else
                {
                    // Autodetect most used color
                    Console.Write($"Background color autotect... ");
                    Dictionary<Color, int> colorPerTileCounter = new Dictionary<Color, int>();
                    foreach (var imageNum in imagesRecolored.Keys)
                    {
                        var image = imagesRecolored[imageNum];
                        for (int tileY = 0; tileY < image.Height / tilePalHeight; tileY++)
                        {
                            for (int tileX = 0; tileX < image.Width / tilePalWidth; tileX++)
                            {
                                // Count each color only once per tile/sprite
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

                var top4 = new List<Palette>();
                // Calculate palettes if required
                if ((new int[] { 0, 1, 2, 3 }).Select(i => paletteEnabled[i] && fixedPalettes[i] == null).Any())
                {
                    // Creating and counting the palettes
                    Dictionary<Palette, int> paletteCounter = new Dictionary<Palette, int>();
                    foreach (var imageNum in imagesOriginal.Keys)
                    {
                        Console.WriteLine($"Creating palettes for file #{imageNum} - {imageFiles[imageNum]}...");
                        var image = imagesRecolored[imageNum];
                        // For each tile/sprite
                        for (int tileY = 0; tileY < image.Height / tilePalHeight; tileY++)
                        {
                            for (int tileX = 0; tileX < image.Width / tilePalWidth; tileX++)
                            {
                                // Create palette using up to three most popular colors
                                var palette = new Palette(
                                    image, tileX * tilePalWidth, tileY * tilePalHeight,
                                    tilePalWidth, tilePalHeight, bgColor.Value);

                                // Skip tiles with only background color
                                if (!palette.Any()) continue;

                                // Do not count predefined palettes
                                if (fixedPalettes.Where(p => p != null && p.Contains(palette)).Any())
                                    // Считаем количество использования таких палитр
                                    continue;

                                // Count palette usage
                                if (!paletteCounter.ContainsKey(palette))
                                    paletteCounter[palette] = 0;
                                paletteCounter[palette]++;
                            }
                        }
                    }

                    // Group palettes
                    Console.WriteLine($"Calculating final palette list...");
                    // From most popular to less popular
                    var sortedKeys = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();
                    // Some palettes can contain all colors from other palettes, so we need to combine them
                    foreach (var palette2 in sortedKeys)
                        foreach (var palette1 in sortedKeys)
                        {
                            if ((palette2 != palette1) && (palette2.Count >= palette1.Count) && palette2.Contains(palette1))
                            {
                                // Move counter
                                paletteCounter[palette2] += paletteCounter[palette1];
                                paletteCounter[palette1] = 0;
                            }
                        }

                    // Remove unsed palettes
                    paletteCounter = paletteCounter.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);

                    // Sort them again
                    sortedKeys = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

                    // Get 4 most popular palettes
                    top4 = sortedKeys.Take(4).ToList();
                    // Use free colors in palettes to store less popular palettes
                    foreach (var t in top4)
                    {
                        if (t.Count < 3)
                        {
                            foreach (var p in sortedKeys)
                            {
                                var newColors = p.Where(c => !t.Contains(c));
                                if (p != t && (paletteCounter[t] > 0) && (paletteCounter[p] > 0)
                                    && (newColors.Count() + t.Count <= 3))
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

                    // Remove unsed palettes
                    paletteCounter = paletteCounter.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);

                    // Sort them again
                    sortedKeys = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

                    // Combine palettes again
                    foreach (var palette2 in sortedKeys)
                        foreach (var palette1 in sortedKeys)
                        {
                            if ((palette2 != palette1) && (palette2.Count >= palette1.Count) && palette2.Contains(palette1))
                            {
                                // Move counter
                                paletteCounter[palette2] += paletteCounter[palette1];
                                paletteCounter[palette1] = 0;
                            }
                        }


                    // Remove unsed palettes
                    paletteCounter = paletteCounter.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);

                    // Sort them again
                    sortedKeys = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

                    // Renew list of top 4 palettes
                    top4 = sortedKeys.Take(4).ToList();
                }

                // Select palettes
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

                // Calculate palette as color indices and save them to files
                var bgColorId = FindSimilarColor(nesColors, bgColor.Value);
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
                                paletteRaw[c] = FindSimilarColor(nesColors, palettes[p][c].Value);
                        }
                        File.WriteAllBytes(outPalette[p], paletteRaw);
                        Console.WriteLine($"Palette #{p} saved to {outPalette[p]}");
                    }
                }

                // Select palette for each tile/sprite and recolorize using them
                imagesRecolored.Clear();
                foreach (var imageNum in imagesOriginal.Keys)
                {
                    Console.WriteLine($"Mapping palettes for file #{imageNum} - {imageFiles[imageNum]}...");
                    var image = imagesOriginal[imageNum];
                    var imageRecolored = new FastBitmap(image.GetBitmap());
                    imagesRecolored[imageNum] = imageRecolored;
                    palleteIndexes[imageNum] = new byte[image.Width / tilePalWidth, image.Height / tilePalHeight];
                    // For each tile/sprite
                    for (int tileY = 0; tileY < image.Height / tilePalHeight; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / tilePalWidth; tileX++)
                        {
                            double minDelta = double.MaxValue;
                            byte bestPaletteIndex = 0;
                            // Try each palette
                            for (byte paletteIndex = 0; paletteIndex < palettes.Length; paletteIndex++)
                            {
                                if (palettes[paletteIndex] == null) continue;
                                double delta = palettes[paletteIndex].Ge tTileDelta(
                                    image, tileX * tilePalWidth, tileY * tilePalHeight,
                                    tilePalWidth, tilePalHeight, bgColor.Value);
                                // Find palette with most similar colors
                                if (delta < minDelta)
                                {
                                    minDelta = delta;
                                    bestPaletteIndex = paletteIndex;
                                }
                            }
                            Palette bestPalette = palettes[bestPaletteIndex];
                            // Remember palette index
                            palleteIndexes[imageNum][tileX, tileY] = bestPaletteIndex;

                            // Change tile colors to colors from the palette
                            for (int y = 0; y < tilePalHeight; y++)
                            {
                                for (int x = 0; x < tilePalWidth; x++)
                                {
                                    var color = image.GetPixel(tileX * tilePalWidth + x, tileY * tilePalHeight + y);
                                    var similarColor = FindSimilarColor(Enumerable.Concat(
                                            bestPalette,
                                            new Color[] { bgColor.Value }
                                        ), color);
                                    imageRecolored.SetPixel(
                                        tileX * tilePalWidth + x,
                                        tileY * tilePalHeight + y,
                                        similarColor);
                                }
                            }
                        } // tile X
                    } // tile Y

                    // Save preview if required
                    if (outPreview.ContainsKey(imageNum))
                    {
                        imageRecolored.Save(outPreview[imageNum], ImageFormat.Png);
                        Console.WriteLine($"Preview #{imageNum} saved to {outPreview[imageNum]}");
                    }
                }


                // Generate attribute tables
                foreach (var imageNum in outAttributeTable.Keys)
                {
                    if (mode != TilesMode.Backgrounds)
                        throw new InvalidOperationException("Attribute table generation available for backgrounds mode only");
                    Console.WriteLine($"Creating attribute table for file #{imageNum} - {Path.GetFileName(imageFiles[imageNum])}...");
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

                    // Save to file
                    if (outAttributeTable.ContainsKey(imageNum))
                    {
                        File.WriteAllBytes(outAttributeTable[imageNum], attributeTableRaw.ToArray());
                        Console.WriteLine($"Attribute table #{imageNum} saved to {outAttributeTable[imageNum]}");
                    }
                }

                // Generate pattern tables and nametables
                foreach (var imageNum in imagesRecolored.Keys)
                {
                    Console.WriteLine($"Creating pattern table for file #{imageNum} - {Path.GetFileName(imageFiles[imageNum])}...");
                    var image = imagesRecolored[imageNum];
                    if (!patternTables.ContainsKey(imageNum)) patternTables[imageNum] = new Dictionary<int, Tile>();
                    var patternTable = patternTables[imageNum];
                    if (!nameTables.ContainsKey(imageNum)) nameTables[imageNum] = new List<int>();
                    var nameTable = nameTables[imageNum];
                    if (!patternTableStartOffsets.ContainsKey(imageNum))
                        patternTableStartOffsets[imageNum] = 0;
                    var tileID = patternTableStartOffsets[imageNum];

                    var tileWidth = 8;
                    var tileHeight = (mode == TilesMode.Sprites8x16 ? 16 : 8);

                    for (int tileY = 0; tileY < image.Height / tileHeight; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / tileWidth; tileX++)
                        {
                            var tileData = new byte[tileWidth, tileHeight];
                            for (int y = 0; y < tileHeight; y++)
                                for (int x = 0; x < tileWidth; x++)
                                {
                                    var color = image.GetPixel(tileX * tileWidth + x, tileY * tileHeight + y);
                                    var palette = palettes[palleteIndexes[imageNum][tileX / (tilePalWidth / tileWidth), tileY / (tilePalHeight / tileHeight)]];
                                    byte colorIndex = 0;
                                    if (color != bgColor)
                                    {
                                        colorIndex = 1;
                                        while (palette[colorIndex] != color) colorIndex++;
                                    }
                                    tileData[x, y] = colorIndex;
                                }
                            var tile = new Tile(tileData);
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
                    if (tileID > patternTableStartOffsets[imageNum])
                        Console.WriteLine($"#{imageNum} tiles range: {patternTableStartOffsets[imageNum]}-{tileID - 1}");
                    else
                        Console.WriteLine($"Pattern table is empty");
                    if (tileID > (mode == TilesMode.Sprites8x16 ? 128 : 256) && !ignoreTilesRange)
                        throw new ArgumentOutOfRangeException("Tiles out of range");

                    // Save pattern table to file
                    if (outPatternTable.ContainsKey(imageNum))
                    {
                        var patternTableRaw = new List<byte>();
                        for (int t = patternTableStartOffsets[imageNum]; t < tileID; t++)
                        {
                            var raw = patternTable[t].GetRawData();
                            patternTableRaw.AddRange(raw);
                        }
                        File.WriteAllBytes(outPatternTable[imageNum], patternTableRaw.ToArray());
                        Console.WriteLine($"Pattern table #{imageNum} saved to {outPatternTable[imageNum]}");
                    }

                    // Save nametable to file
                    if (outNameTable.ContainsKey(imageNum))
                    {
                        File.WriteAllBytes(outNameTable[imageNum], nameTable.Select(i => (byte)i).ToArray());
                        Console.WriteLine($"Name table #{imageNum} saved to {outPatternTable[imageNum]}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"Error: {ex.GetType()}: {ex.Message}{ex.StackTrace}");
#else
                Console.WriteLine($"Error: {ex.GetType()}: {ex.Message}");
#endif
                return 1;
            }
        }

        static byte FindSimilarColor(Dictionary<byte, Color> colors, Color color)
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

        static Color FindSimilarColor(IEnumerable<Color> colors, Color color)
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
