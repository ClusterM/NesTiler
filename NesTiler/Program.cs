using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace com.clusterrr.Famicom.NesTiler
{
    public class Program
    {
        const string REPO_PATH = "https://github.com/ClusterM/nestiler";
        const string DEFAULT_COLORS_FILE = @"nestiler-colors.json";
        static DateTime BUILD_TIME = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(Properties.Resources.buildtime.Trim()));
        const int MAX_BG_COLOR_AUTODETECT_ITERATIONS = 5;
        static byte[] FORBIDDEN_COLORS = new byte[] { 0x0D, 0x0E, 0x0F, 0x1E, 0x1F, 0x2E, 0x2F, 0x3E, 0x3F };

        public enum TilesMode
        {
            Backgrounds,
            Sprites,
            Sprites8x16
        }

        static void PrintAppInfo()
        {
            Console.WriteLine($"NesTiler v{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}");
            Console.WriteLine($"  Commit {Properties.Resources.gitCommit} @ {REPO_PATH}");
#if DEBUG
            Console.WriteLine($"  Debug version, build time: {BUILD_TIME.ToLocalTime()}");
#endif
            Console.WriteLine("  (c) Alexey 'Cluster' Avdyukhin / https://clusterrr.com / clusterrr@clusterrr.com");
            Console.WriteLine("");
        }

        static void PrintHelp()
        {
            Console.WriteLine($"Usage: {Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)} <options>");
            Console.WriteLine();
            Console.WriteLine("Available options:");
            Console.WriteLine(" {0,-40}{1}", "--in-<#> <file>[:offset[:height]]", "input file number #, optionally cropped vertically");
            Console.WriteLine(" {0,-40}{1}", "--colors <file>", $"JSON file with list of available colors (default - {DEFAULT_COLORS_FILE})");
            Console.WriteLine(" {0,-40}{1}", "--mode bg|sprite8x8|sprite8x16", "mode: backgrounds, 8x8 sprites or 8x16 sprites (default - bg)");
            Console.WriteLine(" {0,-40}{1}", "--bg-color <color>", "background color in HTML color format (default - autodetected)");
            Console.WriteLine(" {0,-40}{1}", "--enable-palettes <palettes>", "zero-based comma separated list of palette numbers to use (default - 0,1,2,3)");
            Console.WriteLine(" {0,-40}{1}", "--palette-<#>", "comma separated list of colors to use in palette number # (default - auto)");
            Console.WriteLine(" {0,-40}{1}", "--pattern-offset-<#>", "first tile ID for pattern table for file number # (default - 0)");
            Console.WriteLine(" {0,-40}{1}", "--share-pattern-table", "use one pattern table for all images");
            Console.WriteLine(" {0,-40}{1}", "--ignore-tiles-range", "option to disable tile ID overflow check");
            Console.WriteLine(" {0,-40}{1}", "--lossy", "option to ignore palettes loss, produces distorted image if there are too many colors");
            Console.WriteLine(" {0,-40}{1}", "--out-preview-<#> <file.png>", "output filename for preview of image number #");
            Console.WriteLine(" {0,-40}{1}", "--out-palette-<#> <file>", "output filename for palette number #");
            Console.WriteLine(" {0,-40}{1}", "--out-pattern-table-<#> <file>", "output filename for pattern table of image number #");
            Console.WriteLine(" {0,-40}{1}", "--out-name-table-<#> <file>", "output filename for nametable of image number #");
            Console.WriteLine(" {0,-40}{1}", "--out-attribute-table-<#> <file>", "output filename for attribute table of image number #");
            Console.WriteLine(" {0,-40}{1}", "--out-tiles-csv <file.csv>", "output filename for tiles info in CSV format");
            Console.WriteLine(" {0,-40}{1}", "--out-palettes-csv <file.csv>", "output filename for palettes info in CSV format");
            Console.WriteLine(" {0,-40}{1}", "--out-colors-table <file.png>", "output filename for graphical table of available colors (from \"--colors\" option)");
            Console.WriteLine(" {0,-40}{1}", "--quiet", "suppress console output");
        }

        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0 || args.Contains("help") || args.Contains("--help"))
                {
                    PrintAppInfo();
                    PrintHelp();
                    return 0;
                }

                string colorsFile = Path.Combine(AppContext.BaseDirectory, DEFAULT_COLORS_FILE);
                if (!File.Exists(colorsFile))
                    colorsFile = Path.Combine(Directory.GetCurrentDirectory(), DEFAULT_COLORS_FILE);
                if (!File.Exists(colorsFile) && !OperatingSystem.IsWindows())
                    colorsFile = Path.Combine("/etc", DEFAULT_COLORS_FILE);
                var imageFiles = new Dictionary<int, string>();
                Color? bgColor = null;
                var paletteEnabled = new bool[4] { true, true, true, true };
                var fixedPalettes = new Palette[4] { null, null, null, null };
                var mode = TilesMode.Backgrounds;
                int tilePalWidth = 16;
                int tilePalHeight = 16;
                bool sharePatternTable = false;
                bool ignoreTilesRange = false;
                bool lossy = false;
                int patternTableStartOffsetShared = 0;
                var patternTableStartOffsets = new Dictionary<int, int>();
                var attributeTableOffsets = new Dictionary<int, int>();
                bool quiet = false;

                // Filenames
                var outPreview = new Dictionary<int, string>();
                var outPalette = new Dictionary<int, string>();
                var outPatternTable = new Dictionary<int, string>();
                string outPatternTableShared = null;
                var outNameTable = new Dictionary<int, string>();
                var outAttributeTable = new Dictionary<int, string>();
                string outTilesCsv = null;
                string outPalettesCsv = null;
                string outColorsTable = null;
                var console = (string text) => { if (!quiet) Console.WriteLine(text); };

                // Data
                var images = new Dictionary<int, FastBitmap>();
                var paletteIndexes = new Dictionary<int, byte[,]>();
                var patternTables = new Dictionary<int, Dictionary<Tile, int>>();
                var nameTables = new Dictionary<int, List<int>>();
                int tileID = 0;

                // Misc
                var nesColorsCache = new Dictionary<Color, byte>();
                var paramRegex = new Regex(@"^--?(?<param>[a-zA-Z-]+?)-?(?<index>[0-9]*)$");
                bool nothingToDo = true;

                for (int i = 0; i < args.Length; i++)
                {
                    var match = paramRegex.Match(args[i]);
                    if (!match.Success)
                        throw new ArgumentException($"Unknown argument: {args[i]}");
                    string param = match.Groups["param"].Value;
                    string indexStr = match.Groups["index"].Value;
                    int indexNum = 0;
                    if (!string.IsNullOrEmpty(indexStr))
                        indexNum = int.Parse(indexStr);
                    string value = i < args.Length - 1 ? args[i + 1] : "";
                    switch (param)
                    {
                        case "i":
                        case "in":
                        case "input":
                            imageFiles[indexNum] = value;
                            i++;
                            nothingToDo = false;
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
                            if (value != "auto") bgColor = ColorTranslator.FromHtml(value);
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
                            patternTableStartOffsetShared = patternTableStartOffsets[indexNum];
                            i++;
                            break;
                        case "attribute-table-offset":
                            attributeTableOffsets[indexNum] = int.Parse(value);
                            i++;
                            break;
                        case "share-pattern-table":
                            sharePatternTable = true;
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
                            outPatternTableShared = value;
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
                        case "lossy":
                            lossy = true;
                            break;
                        case "out-tiles-csv":
                            outTilesCsv = value;
                            i++;
                            break;
                        case "out-palettes-csv":
                            outPalettesCsv = value;
                            i++;
                            break;
                        case "out-colors-table":
                            outColorsTable = value;
                            i++;
                            nothingToDo = false;
                            break;
                        case "quiet":
                        case "q":
                            quiet = true;
                            break;
                        default:
                            throw new ArgumentException($"Unknown argument: {args[i]}");
                    }
                }

                if (nothingToDo)
                {
                    PrintAppInfo();
                    Console.WriteLine("Nothing to do.");
                    Console.WriteLine();
                    PrintHelp();
                    return 1;
                }

                if (!quiet) PrintAppInfo();

                // Loading and parsing palette JSON
                var paletteJson = File.ReadAllText(colorsFile);
                var nesColorsStr = JsonSerializer.Deserialize<Dictionary<string, string>>(paletteJson);
                var nesColors = nesColorsStr.Select(kv => new KeyValuePair<byte, Color>(
                        kv.Key.ToLower().StartsWith("0x") ? (byte)Convert.ToInt32(kv.Key.Substring(2), 16) : byte.Parse(kv.Key),
                        ColorTranslator.FromHtml(kv.Value)
                    )).Where(kv => !FORBIDDEN_COLORS.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
                var outTilesCsvLines = !string.IsNullOrEmpty(outTilesCsv) ? new List<string>() : null;
                var outPalettesCsvLines = !string.IsNullOrEmpty(outPalettesCsv) ? new List<string>() : null;

                if (outColorsTable != null)
                {
                    WriteColorsTable(nesColors, outColorsTable);
                }

                // Change the fixed palettes to colors from the NES palette
                for (int i = 0; i < fixedPalettes.Length; i++)
                {
                    if (fixedPalettes[i] == null) continue;
                    var colorsInPalette = fixedPalettes[i].ToArray();
                    for (int j = 0; j < colorsInPalette.Length; j++)
                        colorsInPalette[j] = nesColors[FindSimilarColor(nesColors, colorsInPalette[j], nesColorsCache)];
                    fixedPalettes[i] = new Palette(colorsInPalette);
                }

                // Loading images
                foreach (var imageFile in imageFiles)
                {
                    console($"Loading file #{imageFile.Key} - {Path.GetFileName(imageFile.Value)}...");
                    var offsetRegex = new Regex(@"^(?<filename>.*?)(:(?<offset>[0-9]+)(:(?<height>[0-9]+))?)?$");
                    var match = offsetRegex.Match(imageFile.Value);
                    var filename = match.Groups["filename"].Value;
                    var offsetS = match.Groups["offset"].Value;
                    var heightS = match.Groups["height"].Value;
                    // Crop it if need
                    int offset = 0;
                    int height = 0;
                    if (!string.IsNullOrEmpty(offsetS))
                    {
                        offset = int.Parse(offsetS);
                        if (!string.IsNullOrEmpty(heightS)) height = int.Parse(heightS);
                    }
                    if (!File.Exists(filename)) throw new FileNotFoundException($"File {filename} not found");
                    var image = FastBitmap.Decode(filename, offset, height);
                    if (image == null) throw new InvalidDataException($"Can't load {filename}");
                    images[imageFile.Key] = image;

                    //if ((imagesOriginal[image.Key].Width % tilePalWidth != 0) || (imagesOriginal[image.Key].Height % tilePalHeight != 0))
                    //    throw new InvalidDataException("Invalid image size");
                    // TODO: more image size checks
                }

                // Change all colors in the images to colors from the NES palette
                foreach (var imageNum in images.Keys)
                {
                    console($"Adjusting colors for file #{imageNum} - {imageFiles[imageNum]}...");
                    var image = images[imageNum];
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var color = image.GetPixelColor(x, y);
                            var similarColor = nesColors[FindSimilarColor(nesColors, color, nesColorsCache)];
                            image.SetPixelColor(x, y, similarColor);
                        }
                    }
                }

                List<Palette> calculatedPalettes;
                var maxCalculatedPaletteCount = Enumerable.Range(0, 4).Select(i => paletteEnabled[i] && fixedPalettes[i] == null).Count();
                // Detect background color
                if (bgColor.HasValue)
                {
                    // Manually
                    bgColor = nesColors[FindSimilarColor(nesColors, bgColor.Value, nesColorsCache)];
                    calculatedPalettes = CalculatePalettes(images, paletteEnabled, fixedPalettes, attributeTableOffsets, tilePalWidth, tilePalHeight, bgColor.Value).ToList();
                }
                else
                {
                    // Autodetect most used color
                    console($"Background color autodetect... ");
                    Dictionary<Color, int> colorPerTileCounter = new Dictionary<Color, int>();
                    foreach (var imageNum in images.Keys)
                    {
                        var image = images[imageNum];
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
                                        var color = image.GetPixelColor((tileX * tilePalWidth) + x, (tileY * tilePalHeight) + y);
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
                    // Most used colors
                    var candidates = colorPerTileCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();
                    // Try to calculate palettes for every background color
                    var calcResults = new Dictionary<Color, Palette[]>();
                    for (int i = 0; i < Math.Min(candidates.Length, MAX_BG_COLOR_AUTODETECT_ITERATIONS); i++)
                    {
                        calcResults[candidates[i]] = CalculatePalettes(images, paletteEnabled, fixedPalettes, attributeTableOffsets, tilePalWidth, tilePalHeight, candidates[i]);
                    }
                    // Select background color which uses minimum palettes
                    var kv = calcResults.OrderBy(kv => kv.Value.Length).First();
                    (bgColor, calculatedPalettes) = (kv.Key, kv.Value.ToList());
                    console(ColorTranslator.ToHtml(bgColor.Value));
                }

                if (calculatedPalettes.Count > maxCalculatedPaletteCount && !lossy)
                {
                    throw new ArgumentOutOfRangeException($"Can't fit {calculatedPalettes.Count} palettes - {maxCalculatedPaletteCount} is maximum");
                }

                // Select palettes
                var palettes = new Palette[4] { null, null, null, null };
                outPalettesCsvLines?.Add("palette_id,color0,color1,color2,color3");
                for (var i = 0; i < palettes.Length; i++)
                {
                    if (paletteEnabled[i])
                    {
                        if (fixedPalettes[i] != null)
                        {
                            palettes[i] = fixedPalettes[i];
                        }
                        else if (calculatedPalettes.Any())
                        {
                            if (calculatedPalettes.Any())
                            {
                                palettes[i] = calculatedPalettes.First();
                                calculatedPalettes.RemoveAt(0);
                            }
                            else
                            {
                                palettes[i] = new Palette();
                            }
                        }

                        if (palettes[i] != null)
                        {
                            console($"Palette #{i}: {ColorTranslator.ToHtml(bgColor.Value)}(BG) {string.Join(" ", palettes[i].Select(p => ColorTranslator.ToHtml(p)))}");
                            outPalettesCsvLines?.Add($"{i},{ColorTranslator.ToHtml(bgColor.Value)},{string.Join(",", Enumerable.Range(1, 3).Select(c => (palettes[i][c] != null ? ColorTranslator.ToHtml(palettes[i][c].Value) : "")))}");
                        }
                    }
                }

                // Calculate palette as color indices and save them to files
                var bgColorId = FindSimilarColor(nesColors, bgColor.Value, nesColorsCache);
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
                                paletteRaw[c] = FindSimilarColor(nesColors, palettes[p][c].Value, nesColorsCache);
                        }
                        File.WriteAllBytes(outPalette[p], paletteRaw);
                        console($"Palette #{p} saved to {outPalette[p]}");
                    }
                }

                // Select palette for each tile/sprite and recolorize using them
                foreach (var imageNum in images.Keys)
                {
                    console($"Mapping palettes for file #{imageNum} - {imageFiles[imageNum]}...");
                    var image = images[imageNum];
                    int attributeTableOffset;
                    attributeTableOffsets.TryGetValue(imageNum, out attributeTableOffset);
                    paletteIndexes[imageNum] = new byte[image.Width / tilePalWidth, (int)Math.Ceiling((image.Height + attributeTableOffset) / (float)tilePalHeight)];
                    // For each tile/sprite
                    for (int tilePalY = 0; tilePalY < (int)Math.Ceiling((image.Height + attributeTableOffset) / (float)tilePalHeight); tilePalY++)
                    {
                        for (int tilePalX = 0; tilePalX < image.Width / tilePalWidth; tilePalX++)
                        {
                            double minDelta = double.MaxValue;
                            byte bestPaletteIndex = 0;
                            // Try each palette
                            for (byte paletteIndex = 0; paletteIndex < palettes.Length; paletteIndex++)
                            {
                                if (palettes[paletteIndex] == null) continue;
                                double delta = palettes[paletteIndex].GetTileDelta(
                                    image, tilePalX * tilePalWidth, (tilePalY * tilePalHeight) - attributeTableOffset,
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
                            paletteIndexes[imageNum][tilePalX, tilePalY] = bestPaletteIndex;

                            // Change tile colors to colors from the palette
                            for (int y = 0; y < tilePalHeight; y++)
                            {
                                for (int x = 0; x < tilePalWidth; x++)
                                {
                                    var cy = (tilePalY * tilePalHeight) + y - attributeTableOffset;
                                    if (cy < 0) continue;
                                    var color = image.GetPixelColor((tilePalX * tilePalWidth) + x, cy);
                                    var similarColor = FindSimilarColor(Enumerable.Concat(
                                            bestPalette,
                                            new Color[] { bgColor.Value }
                                        ), color);
                                    image.SetPixelColor(
                                        (tilePalX * tilePalWidth) + x,
                                        cy,
                                        similarColor);
                                }
                            }
                        } // tile X
                    } // tile Y

                    // Save preview if required
                    if (outPreview.ContainsKey(imageNum))
                    {
                        File.WriteAllBytes(outPreview[imageNum], image.Encode(SKEncodedImageFormat.Png, 0));
                        console($"Preview #{imageNum} saved to {outPreview[imageNum]}");
                    }
                }

                // Generate attribute tables
                foreach (var imageNum in outAttributeTable.Keys)
                {
                    if (mode != TilesMode.Backgrounds)
                        throw new InvalidOperationException("Attribute table generation available for backgrounds mode only");
                    console($"Creating attribute table for file #{imageNum} - {Path.GetFileName(imageFiles[imageNum])}...");
                    var image = images[imageNum];
                    int attributeTableOffset;
                    attributeTableOffsets.TryGetValue(imageNum, out attributeTableOffset);
                    var attributeTableRaw = new List<byte>();
                    int width = paletteIndexes[imageNum].GetLength(0);
                    int height = paletteIndexes[imageNum].GetLength(1);
                    for (int ptileY = 0; ptileY < Math.Ceiling(height / 2.0); ptileY++)
                    {
                        for (int ptileX = 0; ptileX < Math.Ceiling(width / 2.0); ptileX++)
                        {
                            byte topLeft = 0;
                            byte topRight = 0;
                            byte bottomLeft = 0;
                            byte bottomRight = 0;

                            topLeft = paletteIndexes[imageNum][ptileX * 2, ptileY * 2];
                            topLeft = paletteIndexes[imageNum][ptileX * 2, ptileY * 2];
                            topRight = paletteIndexes[imageNum][(ptileX * 2) + 1, ptileY * 2];
                            topRight = paletteIndexes[imageNum][(ptileX * 2) + 1, ptileY * 2];
                            if ((ptileY * 2) + 1 < height)
                            {
                                bottomLeft = paletteIndexes[imageNum][ptileX * 2, (ptileY * 2) + 1];
                                bottomLeft = paletteIndexes[imageNum][ptileX * 2, (ptileY * 2) + 1];
                                bottomRight = paletteIndexes[imageNum][(ptileX * 2) + 1, (ptileY * 2) + 1];
                                bottomRight = paletteIndexes[imageNum][(ptileX * 2) + 1, (ptileY * 2) + 1];
                            }

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
                        console($"Attribute table #{imageNum} saved to {outAttributeTable[imageNum]}");
                    }
                }

                // Generate pattern tables and nametables
                foreach (var imageNum in images.Keys)
                {
                    console($"Creating pattern table for file #{imageNum} - {Path.GetFileName(imageFiles[imageNum])}...");
                    var image = images[imageNum];
                    int attributeTableOffset;
                    attributeTableOffsets.TryGetValue(imageNum, out attributeTableOffset);
                    if (!patternTables.ContainsKey(!sharePatternTable ? imageNum : 0)) patternTables[!sharePatternTable ? imageNum : 0] = new Dictionary<Tile, int>();
                    var patternTable = patternTables[!sharePatternTable ? imageNum : 0];
                    if (!nameTables.ContainsKey(imageNum)) nameTables[imageNum] = new List<int>();
                    var nameTable = nameTables[imageNum];
                    if (!sharePatternTable)
                    {
                        if (!patternTableStartOffsets.ContainsKey(imageNum))
                            patternTableStartOffsets[imageNum] = 0;
                        tileID = patternTableStartOffsets[imageNum];
                    }
                    else
                    {
                        tileID = Math.Max(tileID, patternTableStartOffsetShared);
                        patternTableStartOffsets[imageNum] = tileID;
                    }

                    var tileWidth = 8;
                    var tileHeight = mode == TilesMode.Sprites8x16 ? 16 : 8;

                    outTilesCsvLines?.Add("image_id,image_file,line,column,tile_x,tile_y,tile_width,tile_height,tile_id,palette_id");
                    for (int tileY = 0; tileY < image.Height / tileHeight; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / tileWidth; tileX++)
                        {
                            var tileData = new byte[tileWidth * tileHeight];
                            byte paletteIndex = 0;
                            for (int y = 0; y < tileHeight; y++)
                                for (int x = 0; x < tileWidth; x++)
                                {
                                    var color = image.GetPixelColor((tileX * tileWidth) + x, (tileY * tileHeight) + y);
                                    var palette = palettes[paletteIndexes[imageNum][tileX / (tilePalWidth / tileWidth), (tileY + (attributeTableOffset / tileHeight)) / (tilePalHeight / tileHeight)]];
                                    paletteIndex = 0;
                                    if (color != bgColor)
                                    {
                                        paletteIndex = 1;
                                        while (palette[paletteIndex] != color) paletteIndex++;
                                    }
                                    tileData[(y * tileWidth) + x] = paletteIndex;
                                }
                            var tile = new Tile(tileData, tileWidth, tileHeight);
                            int currentTileID, id;
                            if (patternTable.TryGetValue(tile, out id))
                            {
                                nameTable.Add(id);
                                currentTileID = id;
                            }
                            else
                            {
                                patternTable[tile] = tileID;
                                nameTable.Add(tileID);
                                currentTileID = tileID;
                                tileID++;
                            }

                            outTilesCsvLines?.Add($"{imageNum},{imageFiles[imageNum]},{tileY},{tileX},{tileX * tileWidth},{tileY * tileHeight},{tileWidth},{tileHeight},{currentTileID},{paletteIndex}");
                        }
                    }
                    if (sharePatternTable && tileID > patternTableStartOffsetShared)
                        console($"#{imageNum} tiles range: {patternTableStartOffsetShared}-{tileID - 1}");
                    else if (tileID > patternTableStartOffsets[imageNum])
                        console($"#{imageNum} tiles range: {patternTableStartOffsets[imageNum]}-{tileID - 1}");
                    else
                        console($"Pattern table is empty");
                    if (tileID > (mode == TilesMode.Sprites8x16 ? 128 : 256) && !ignoreTilesRange)
                        throw new ArgumentOutOfRangeException("Tiles out of range");

                    // Save pattern table to file
                    if (outPatternTable.ContainsKey(imageNum) && !sharePatternTable)
                    {
                        var patternTableReversed = patternTable.ToDictionary(kv => kv.Value, kv => kv.Key);
                        var patternTableRaw = new List<byte>();
                        for (int t = patternTableStartOffsets[imageNum]; t < tileID; t++)
                        {
                            var raw = patternTableReversed[t].GetAsTileData();
                            patternTableRaw.AddRange(raw);
                        }
                        File.WriteAllBytes(outPatternTable[imageNum], patternTableRaw.ToArray());
                        console($"Pattern table #{imageNum} saved to {outPatternTable[imageNum]}");
                    }

                    // Save nametable to file
                    if (outNameTable.ContainsKey(imageNum))
                    {
                        File.WriteAllBytes(outNameTable[imageNum], nameTable.Select(i => (byte)i).ToArray());
                        console($"Name table #{imageNum} saved to {outNameTable[imageNum]}");
                    }
                }

                // Save shared pattern table to file
                if (sharePatternTable)
                {
                    var patternTableReversed = patternTables[0].ToDictionary(kv => kv.Value, kv => kv.Key);
                    var patternTableRaw = new List<byte>();
                    for (int t = patternTableStartOffsetShared; t < tileID; t++)
                    {
                        var raw = patternTableReversed[t].GetAsTileData();
                        patternTableRaw.AddRange(raw);
                    }
                    File.WriteAllBytes(outPatternTableShared, patternTableRaw.ToArray());
                    console($"Pattern table saved to {outPatternTableShared}");
                }

                // Save CSV tiles report
                if (outTilesCsvLines != null)
                {
                    File.WriteAllLines(outTilesCsv, outTilesCsvLines);
                }
                // Save CSV palettes report
                if (outTilesCsvLines != null)
                {
                    File.WriteAllLines(outPalettesCsv, outPalettesCsvLines);
                }

                return 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.Error.WriteLine($"Error: {ex.GetType()}: {ex.Message}{ex.StackTrace}");
#else
                Console.Error.WriteLine($"Error: {ex.GetType()}: {ex.Message}");
#endif
                return 1;
            }
        }

        static void WriteColorsTable(Dictionary<byte, Color> nesColors, string filename)
        {
            const int colorSize = 64;
            const int colorColumns = 16;
            const int colorRows = 4;
            const int strokeWidth = 5;
            float textSize = 20;
            float textYOffset = 39;
            using var image = new SKBitmap(colorSize * colorColumns, colorSize * colorRows);
            using var canvas = new SKCanvas(image);
            for (int y = 0; y < colorRows; y++)
            {
                for (int x = 0; x < colorColumns; x++)
                {
                    Color color;
                    SKColor skColor;
                    SKPaint paint;
                    if (nesColors.TryGetValue((byte)((y * colorColumns) + x), out color))
                    {
                        skColor = new SKColor(color.R, color.G, color.B);
                        paint = new SKPaint() { Color = skColor };
                        canvas.DrawRegion(new SKRegion(new SKRectI(x * colorSize, y * colorSize, (x + 1) * colorSize, (y + 1) * colorSize)), paint);

                        skColor = new SKColor((byte)(0xFF - color.R), (byte)(0xFF - color.G), (byte)(0xFF - color.B)); // invert color
                        paint = new SKPaint()
                        {
                            Color = skColor,
                            TextAlign = SKTextAlign.Center,
                            TextSize = textSize,
                            FilterQuality = SKFilterQuality.High,
                            IsAntialias = true
                        };
                        canvas.DrawText($"{(y * colorColumns) + x:X02}", (x * colorSize) + (colorSize / 2), (y * colorSize) + textYOffset, paint);
                    }
                    else
                    {
                        paint = new SKPaint() { Color = SKColors.Black };
                        SKPath path = new SKPath();
                        canvas.DrawRegion(new SKRegion(new SKRectI(x * colorSize, y * colorSize, (x + 1) * colorSize, (y + 1) * colorSize)), paint);
                        paint = new SKPaint()
                        {
                            Color = SKColors.Red,
                            Style = SKPaintStyle.Stroke,
                            StrokeCap = SKStrokeCap.Round,
                            StrokeWidth = strokeWidth,
                            FilterQuality = SKFilterQuality.High,
                            IsAntialias = true
                        };
                        canvas.DrawLine((x * colorSize) + strokeWidth, (y * colorSize) + strokeWidth, ((x + 1) * colorSize) - strokeWidth, ((y + 1) * colorSize) - strokeWidth, paint);
                        canvas.DrawLine(((x + 1) * colorSize) - strokeWidth, (y * colorSize) + strokeWidth, (x * colorSize) + strokeWidth, ((y + 1) * colorSize) - strokeWidth, paint);
                    }
                };
            }
            File.WriteAllBytes(filename, image.Encode(SKEncodedImageFormat.Png, 0).ToArray());
        }

        static Palette[] CalculatePalettes(Dictionary<int, FastBitmap> images, bool[] paletteEnabled, Palette[] fixedPalettes, Dictionary<int, int> attributeTableOffsets, int tilePalWidth, int tilePalHeight, Color bgColor)
        {
            var required = Enumerable.Range(0, 4).Select(i => paletteEnabled[i] && fixedPalettes[i] == null);
            // Creating and counting the palettes
            var paletteCounter = new Dictionary<Palette, int>();
            foreach (var imageNum in images.Keys)
            {
                // write($"Calculating palettes for file #{imageNum} - {imageFiles[imageNum]}...");
                var image = images[imageNum];
                int attributeTableOffset;
                attributeTableOffsets.TryGetValue(imageNum, out attributeTableOffset);
                // For each tile/sprite
                for (int tileY = 0; tileY < (image.Height + attributeTableOffset) / tilePalHeight; tileY++)
                {
                    for (int tileX = 0; tileX < image.Width / tilePalWidth; tileX++)
                    {
                        // Create palette using up to three most used colors
                        var palette = new Palette(
                            image, tileX * tilePalWidth, (tileY * tilePalHeight) - attributeTableOffset,
                            tilePalWidth, tilePalHeight, bgColor);

                        // Skip tiles with only background color
                        if (!palette.Any()) continue;

                        // Do not count predefined palettes
                        if (fixedPalettes.Where(p => p != null && p.Contains(palette)).Any())
                            continue;

                        // Count palette usage
                        if (!paletteCounter.ContainsKey(palette))
                            paletteCounter[palette] = 0;
                        paletteCounter[palette]++;
                    }
                }
            }

            // Group palettes
            var result = new Palette[0];
            // Multiple iterations
            while (true)
            {
                // Remove unused palettes
                paletteCounter = paletteCounter.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
                // Sort by usage
                result = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

                // Some palettes can contain all colors from other palettes, so we need to combine them
                foreach (var palette2 in result)
                    foreach (var palette1 in result)
                    {
                        if ((palette2 != palette1) && (palette2.Count >= palette1.Count) && palette2.Contains(palette1))
                        {
                            // Move counter
                            paletteCounter[palette2] += paletteCounter[palette1];
                            paletteCounter[palette1] = 0;
                        }
                    }

                // Remove unused palettes
                paletteCounter = paletteCounter.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
                // Sort them again
                result = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

                // Get most used palettes
                var top = result.Take(required.Count()).ToList();
                // Use free colors in palettes to store less popular palettes
                bool grouped = false;
                foreach (var t in top)
                {
                    if (t.Count < 3)
                    {
                        foreach (var p in result)
                        {
                            var newColors = p.Where(c => !t.Contains(c));
                            if (p != t && (newColors.Count() + t.Count <= 3))
                            {
                                var count1 = paletteCounter[t];
                                var count2 = paletteCounter[p];
                                paletteCounter[t] = 0;
                                paletteCounter[p] = 0;
                                foreach (var c in newColors) t.Add(c);
                                paletteCounter[t] = count1 + count2;
                                grouped = true;
                            }
                        }
                    }
                }
                if (!grouped) break; // Nothing changed, stop iterations
            }

            // Remove unused palettes
            paletteCounter = paletteCounter.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
            // Sort them again
            result = paletteCounter.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

            return result;
        }

        static byte FindSimilarColor(Dictionary<byte, Color> colors, Color color, Dictionary<Color, byte> cache = null)
        {
            if (cache != null)
            {
                if (cache.ContainsKey(color))
                    return cache[color];
            }
            byte result = byte.MaxValue;
            double minDelta = double.MaxValue;
            Color c = Color.Transparent;
            foreach (var index in colors.Keys)
            {
                var delta = color.GetDelta(colors[index]);
                if (delta < minDelta)
                {
                    minDelta = delta;
                    result = index;
                    c = colors[index];
                }
            }
            if (result == byte.MaxValue)
                throw new KeyNotFoundException("Invalid color: " + color.ToString());
            if (cache != null)
                cache[color] = result;
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
