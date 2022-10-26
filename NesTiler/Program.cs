using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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

        enum TilesMode
        {
            Backgrounds,
            Sprites8x8,
            Sprites8x16
        }

        static void PrintAppInfo()
        {
            Console.WriteLine($"NesTiler v{Assembly.GetExecutingAssembly()?.GetName()?.Version?.Major}.{Assembly.GetExecutingAssembly()?.GetName()?.Version?.Minor}");
            Console.WriteLine($"  Commit {Properties.Resources.gitCommit} @ {REPO_PATH}");
#if DEBUG
            Console.WriteLine($"  Debug version, build time: {BUILD_TIME.ToLocalTime()}");
#endif
            Console.WriteLine("  (c) Alexey 'Cluster' Avdyukhin / https://clusterrr.com / clusterrr@clusterrr.com");
            Console.WriteLine("");
        }

        static void PrintHelp()
        {
            Console.WriteLine($"Usage: {Path.GetFileName(Process.GetCurrentProcess()?.MainModule?.FileName)} <options>");
            Console.WriteLine();
            Console.WriteLine("Available options:");
            // TODO: move options to constants
            Console.WriteLine("{0,-4} {1,-40}{2}", "-i#,", "--in-<#> <file>[:offset[:height]]", "input file number #, optionally cropped vertically");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-c,",  "--colors <file>", $"JSON or PAL file with the list of available colors (default - {DEFAULT_COLORS_FILE})");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-m,",  "--mode bg|sprites8x8|sprites8x16", "mode: backgrounds, 8x8 sprites or 8x16 sprites (default - bg)");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-b,",  "--bg-color <color>", "background color in HTML color format (default - autodetected)");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-e,",  "--enable-palettes <palettes>", "zero-based comma separated list of palette numbers to use (default - 0,1,2,3)");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-p#,", "--palette-<#>", "comma separated list of colors to use in palette number # (default - auto)");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-o#,", "--pattern-offset-<#>", "first tile ID for pattern table for file number # (default - 0)");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-y#,", "--attribute-table-y-offset-#", "vertical offset for attribute table in pixels");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-s,",  "--share-pattern-table", "use one pattern table for all images");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-r,",  "--ignore-tiles-range", "option to disable tile ID overflow check");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-l,",  "--lossy", "option to ignore palettes loss, produces distorted image if there are too many colors");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-v#,", "--out-preview-<#> <file.png>", "output filename for preview of image number #");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-t#,", "--out-palette-<#> <file>", "output filename for palette number #");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-n#,", "--out-pattern-table-<#> <file>", "output filename for pattern table of image number #");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-a#,", "--out-name-table-<#> <file>", "output filename for nametable of image number #");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-u#,", "--out-attribute-table-<#> <file>", "output filename for attribute table of image number #");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-z,",  "--out-tiles-csv <file.csv>", "output filename for tiles info in CSV format");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-x,",  "--out-palettes-csv <file.csv>", "output filename for palettes info in CSV format");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-g,",  "--out-colors-table <file.png>", "output filename for graphical table of available colors (from \"--colors\" option)");
            Console.WriteLine("{0,-4} {1,-40}{2}", "-q,",  "--quiet", "suppress console output");
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
                bool[] paletteEnabled = new bool[4] { true, true, true, true };
                Palette?[] fixedPalettes = new Palette?[4] { null, null, null, null };
                var mode = TilesMode.Backgrounds;
                int tileWidth = 8;
                int tileHeight = 8;
                int tilePalWidth = 16;
                int tilePalHeight = 16;
                bool sharePatternTable = false;
                bool ignoreTilesRange = false;
                bool lossy = false;
                int patternTableStartOffsetShared = 0;
                var patternTableStartOffsets = new Dictionary<int, int>();
                var attributeTableYOffsets = new Dictionary<int, int>();
                bool quiet = false;

                // Filenames
                var outPreview = new Dictionary<int, string>();
                var outPalette = new Dictionary<int, string>();
                var outPatternTable = new Dictionary<int, string>();
                string? outPatternTableShared = null;
                var outNameTable = new Dictionary<int, string>();
                var outAttributeTable = new Dictionary<int, string>();
                string? outTilesCsv = null;
                string? outPalettesCsv = null;
                string? outColorsTable = null;
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
                        throw new ArgumentException($"Invalid argument.", args[i]);
                    string param = match.Groups["param"].Value;
                    string indexStr = match.Groups["index"].Value;
                    int indexNum = 0;
                    if (!string.IsNullOrEmpty(indexStr))
                        indexNum = int.Parse(indexStr);
                    string value = i < args.Length - 1 ? args[i + 1] : "";
                    int valueInt;
                    switch (param)
                    {
                        case "i":
                        case "in":
                        case "input":
                            imageFiles[indexNum] = value;
                            i++;
                            nothingToDo = false;
                            break;
                        case "c":
                        case "colors":
                            colorsFile = value;
                            i++;
                            break;
                        case "m":
                        case "mode":
                            switch (value.ToLower())
                            {
                                case "sprite":
                                case "sprites":
                                case "sprites8x8":
                                    mode = TilesMode.Sprites8x8;
                                    tileWidth = 8;
                                    tileHeight = 8;
                                    tilePalWidth = 8;
                                    tilePalHeight = 8;
                                    break;
                                case "sprite8x16":
                                case "sprites8x16":
                                    mode = TilesMode.Sprites8x16;
                                    tileWidth = 8;
                                    tileHeight = 16;
                                    tilePalWidth = 8;
                                    tilePalHeight = 16;
                                    break;
                                case "bg":
                                case "background":
                                case "backgrounds":
                                    mode = TilesMode.Backgrounds;
                                    tileWidth = 8;
                                    tileHeight = 8;
                                    tilePalWidth = 16;
                                    tilePalHeight = 16;
                                    break;
                                default:
                                    throw new ArgumentException($"{value} - invalid mode.", param);
                            }
                            i++;
                            break;
                        case "b":
                        case "bgcolor":
                        case "bg-color":
                        case "background-color":
                            if (value != "auto")
                            {
                                try
                                {
                                    bgColor = ColorTranslator.FromHtml(value);
                                }
                                catch (FormatException)
                                {
                                    throw new ArgumentException($"{value} - invalid color.", param);
                                }
                            }
                            i++;
                            break;
                        case "e":
                        case "enable-palettes":
                            {
                                var paletteNumbersStr = value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                for (int pal = 0; pal < paletteEnabled.Length; pal++)
                                    paletteEnabled[pal] = false; // disable all palettes
                                foreach (var palNumStr in paletteNumbersStr)
                                {
                                    if (!int.TryParse(palNumStr, out valueInt))
                                        throw new ArgumentException($"\"{palNumStr}\" is not valid integer value.", param);
                                    if (valueInt < 0 || valueInt > 3)
                                        throw new ArgumentException($"Palette index must be between 0 and 3.", param);
                                    paletteEnabled[valueInt] = true;
                                }
                                if (!paletteEnabled.Where(p => p).Any()) // will never be executed?
                                    throw new ArgumentException($"You need to enable at least one palette.", param);
                            }
                            i++;
                            break;
                        case "p":
                        case "palette":
                            {
                                var colors = value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(c => ColorTranslator.FromHtml(c));
                                fixedPalettes[indexNum] = new Palette(colors);
                            }
                            i++;
                            break;
                        case "o":
                        case "pattern-offset":
                            if (!int.TryParse(value, out valueInt))
                                throw new ArgumentException($"\"{valueInt}\" is not valid integer value.", param);
                            if (valueInt < 0 || valueInt >= 256)
                                throw new ArgumentException($"Value ({valueInt}) must be between 0 and 255.", param);
                            patternTableStartOffsets[indexNum] = valueInt;
                            patternTableStartOffsetShared = patternTableStartOffsets[indexNum];
                            i++;
                            break;
                        case "y":
                        case "attribute-table-y-offset":
                            if (!int.TryParse(value, out valueInt))
                                throw new ArgumentException($"\"{valueInt}\" is not valid integer value.", param);
                            if (valueInt % 8 != 0)
                                throw new ArgumentException($"Value ({valueInt}) must be divisible by 8.", param);
                            if (valueInt < 0 || valueInt >= 256)
                                throw new ArgumentException($"Value ({valueInt}) must be between 0 and 255.", param);
                            attributeTableYOffsets[indexNum] = valueInt;
                            i++;
                            break;
                        case "s":
                        case "share-pattern-table":
                            sharePatternTable = true;
                            break;
                        case "r":
                        case "ignoretilesrange":
                        case "ignore-tiles-range":
                            ignoreTilesRange = true;
                            break;
                        case "l":
                        case "lossy":
                            lossy = true;
                            break;
                        case "v":
                        case "out-preview":
                        case "output-preview":
                            outPreview[indexNum] = value;
                            i++;
                            break;
                        case "t":
                        case "out-palette":
                        case "output-palette":
                            if (indexNum < 0 || indexNum > 3)
                                throw new ArgumentException($"Palette index must be between 0 and 3.", param);
                            outPalette[indexNum] = value;
                            i++;
                            break;
                        case "n":
                        case "out-pattern-table":
                        case "output-pattern-table":
                            outPatternTable[indexNum] = value;
                            outPatternTableShared = value;
                            i++;
                            break;
                        case "a":
                        case "out-name-table":
                        case "output-name-table":
                        case "out-nametable":
                        case "output-nametable":
                            outNameTable[indexNum] = value;
                            i++;
                            break;
                        case "u":
                        case "out-attribute-table":
                        case "output-attribute-table":
                            outAttributeTable[indexNum] = value;
                            i++;
                            break;
                        case "z":
                        case "out-tiles-csv":
                            outTilesCsv = value;
                            i++;
                            break;
                        case "x":
                        case "out-palettes-csv":
                            outPalettesCsv = value;
                            i++;
                            break;
                        case "g":
                        case "out-colors-table":
                            outColorsTable = value;
                            i++;
                            nothingToDo = false;
                            break;
                        case "q":
                        case "quiet":
                            quiet = true;
                            break;
                        default:
                            throw new ArgumentException($"Unknown argument.", args[i]);
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

                if (!quiet)
                {
                    PrintAppInfo();
                    Trace.Listeners.Clear();
                    Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                    Trace.AutoFlush = true;
                }

                // Some input data checks
                switch (mode)
                {
                    case TilesMode.Sprites8x8:
                    case TilesMode.Sprites8x16:
                        if (!bgColor.HasValue) throw new InvalidDataException("You must specify background color for sprites.");
                        break;
                }
                // TODO: more input checks

                // Loading and parsing palette JSON
                var nesColors = LoadColors(colorsFile);
                var outTilesCsvLines = !string.IsNullOrEmpty(outTilesCsv) ? new List<string>() : null;
                var outPalettesCsvLines = !string.IsNullOrEmpty(outPalettesCsv) ? new List<string>() : null;

                if (outColorsTable != null)
                {
                    Trace.WriteLine($"Writing color tables to {outColorsTable}...");
                    WriteColorsTable(nesColors, outColorsTable);
                }

                // Stop if there are no images
                if (!imageFiles.Any()) return 0;

                // Change the fixed palettes to colors from the NES palette
                for (int i = 0; i < fixedPalettes.Length; i++)
                {
                    if (fixedPalettes[i] == null) continue;
                    var colorsInPalette = fixedPalettes[i]!.ToArray();
                    for (int j = 0; j < colorsInPalette.Length; j++)
                        colorsInPalette[j] = nesColors[FindSimilarColor(nesColors, colorsInPalette[j], nesColorsCache)];
                    fixedPalettes[i] = new Palette(colorsInPalette);
                }

                // Loading images
                foreach (var imageFile in imageFiles)
                {
                    Trace.WriteLine($"Loading image #{imageFile.Key} - {Path.GetFileName(imageFile.Value)}...");
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
                    if (!File.Exists(filename)) throw new FileNotFoundException($"Could not find file '{filename}'.", filename);
                    var image = FastBitmap.Decode(filename, offset, height);
                    if (image == null) throw new InvalidDataException($"Can't load {filename}.");
                    images[imageFile.Key] = image;

                    if (mode == TilesMode.Backgrounds && image.Width != 256) throw new ArgumentException("Image width must be 256 for backgrounds mode.", filename);
                    if (image.Width % tileWidth != 0) throw new ArgumentException($"Image width must be divisible by {tileWidth}.", filename);
                    if (image.Height % tileHeight != 0) throw new ArgumentException($"Image height must be divisible by {tileHeight}.", filename);
                }

                // Change all colors in the images to colors from the NES palette
                foreach (var imageNum in images.Keys)
                {
                    Trace.WriteLine($"Adjusting colors for image #{imageNum}...");
                    var image = images[imageNum];
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var color = image.GetPixelColor(x, y);
                            if (color.A >= 0x80 || mode == TilesMode.Backgrounds)
                            {
                                var similarColor = nesColors[FindSimilarColor(nesColors, color, nesColorsCache)];
                                image.SetPixelColor(x, y, similarColor);
                            }
                            else
                            {
                                if (!bgColor.HasValue) throw new InvalidDataException("You must specify background color for images with transparency.");
                                image.SetPixelColor(x, y, bgColor.Value);
                            }
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
                    calculatedPalettes = CalculatePalettes(images, paletteEnabled, fixedPalettes, attributeTableYOffsets, tilePalWidth, tilePalHeight, bgColor.Value).ToList();
                }
                else
                {
                    // Autodetect most used color
                    Trace.Write($"Background color autodetect... ");
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
                        calcResults[candidates[i]] = CalculatePalettes(images, paletteEnabled, fixedPalettes, attributeTableYOffsets, tilePalWidth, tilePalHeight, candidates[i]);
                    }
                    // Select background color which uses minimum palettes
                    var kv = calcResults.OrderBy(kv => kv.Value.Length).First();
                    (bgColor, calculatedPalettes) = (kv.Key, kv.Value.ToList());
                    Trace.WriteLine(ColorTranslator.ToHtml(bgColor.Value));
                }

                if (calculatedPalettes.Count > maxCalculatedPaletteCount && !lossy)
                {
                    throw new ArgumentOutOfRangeException($"Can't fit {calculatedPalettes.Count} palettes, {maxCalculatedPaletteCount} is maximum.");
                }

                // Select palettes
                var palettes = new Palette?[4] { null, null, null, null };
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
                            Trace.WriteLine($"Palette #{i}: {ColorTranslator.ToHtml(bgColor.Value)}(BG) {string.Join(" ", palettes[i]!.Select(p => ColorTranslator.ToHtml(p)))}");
                            // Write CSV if required
                            outPalettesCsvLines?.Add($"{i},{ColorTranslator.ToHtml(bgColor.Value)},{string.Join(",", Enumerable.Range(1, 3).Select(c => (palettes[i]![c] != null ? ColorTranslator.ToHtml(palettes[i]![c]!.Value) : "")))}");
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
                            else if (palettes[p]![c].HasValue)
                                paletteRaw[c] = FindSimilarColor(nesColors, palettes[p]![c]!.Value, nesColorsCache);
                        }
                        File.WriteAllBytes(outPalette[p], paletteRaw);
                        Trace.WriteLine($"Palette #{p} saved to {outPalette[p]}");
                    }
                }

                // Select palette for each tile/sprite and recolorize using them
                foreach (var imageNum in images.Keys)
                {
                    Trace.WriteLine($"Mapping palettes for image #{imageNum}...");
                    var image = images[imageNum];
                    int attributeTableOffset;
                    attributeTableYOffsets.TryGetValue(imageNum, out attributeTableOffset);
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
                                double delta = palettes[paletteIndex]!.GetTileDelta(
                                    image, tilePalX * tilePalWidth, (tilePalY * tilePalHeight) - attributeTableOffset,
                                    tilePalWidth, tilePalHeight, bgColor.Value);
                                // Find palette with most similar colors
                                if (delta < minDelta)
                                {
                                    minDelta = delta;
                                    bestPaletteIndex = paletteIndex;
                                }
                            }
                            Palette bestPalette = palettes[bestPaletteIndex]!; // at least one palette enabled, so can't be null here
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
                        Trace.WriteLine($"Preview #{imageNum} saved to {outPreview[imageNum]}");
                    }
                }

                // Generate attribute tables
                foreach (var imageNum in outAttributeTable.Keys)
                {
                    if (mode != TilesMode.Backgrounds)
                        throw new InvalidOperationException("Attribute table generation available for backgrounds mode only.");
                    Trace.WriteLine($"Creating attribute table for image #{imageNum}...");
                    var image = images[imageNum];
                    int attributeTableOffset;
                    attributeTableYOffsets.TryGetValue(imageNum, out attributeTableOffset);
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
                        Trace.WriteLine($"Attribute table #{imageNum} saved to {outAttributeTable[imageNum]}");
                    }
                }

                // Generate pattern tables and nametables
                outTilesCsvLines?.Add("image_id,image_file,line,column,tile_x,tile_y,tile_width,tile_height,tile_id,palette_id");
                foreach (var imageNum in images.Keys)
                {
                    Trace.WriteLine($"Creating pattern table for image #{imageNum}...");
                    var image = images[imageNum];
                    int attributeTableOffset;
                    attributeTableYOffsets.TryGetValue(imageNum, out attributeTableOffset);
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

                    for (int tileY = 0; tileY < image.Height / tileHeight; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / tileWidth; tileX++)
                        {
                            var tileData = new byte[tileWidth * tileHeight];
                            byte paletteID = 0;
                            for (int y = 0; y < tileHeight; y++)
                                for (int x = 0; x < tileWidth; x++)
                                {
                                    var color = image.GetPixelColor((tileX * tileWidth) + x, (tileY * tileHeight) + y);
                                    paletteID = paletteIndexes[imageNum][tileX / (tilePalWidth / tileWidth), (tileY + (attributeTableOffset / tileHeight)) / (tilePalHeight / tileHeight)];
                                    var palette = palettes[paletteID];
                                    byte colorIndex = 0;
                                    if (color != bgColor)
                                    {
                                        colorIndex = 1;
                                        while (palette![colorIndex] != color) colorIndex++;
                                    }
                                    tileData[(y * tileWidth) + x] = colorIndex;
                                }
                            var tile = new Tile(tileData, tileHeight);
                            int currentTileID, id;
                            if (patternTable.TryGetValue(tile, out id))
                            {
                                if (mode == TilesMode.Backgrounds) nameTable.Add(id);
                                currentTileID = id;
                            }
                            else
                            {
                                patternTable[tile] = tileID;
                                if (mode == TilesMode.Backgrounds) nameTable.Add(tileID);
                                currentTileID = tileID;
                                tileID++;
                            }
                            currentTileID = ((currentTileID & 0x7F) << 1) | ((currentTileID & 0x80) >> 7);

                            // Write CSV if required
                            outTilesCsvLines?.Add($"{imageNum},{imageFiles[imageNum]},{tileY},{tileX},{tileX * tileWidth},{tileY * tileHeight},{tileWidth},{tileHeight},{currentTileID},{paletteID}");
                        }
                    }
                    if (sharePatternTable && tileID > patternTableStartOffsetShared)
                        Trace.WriteLine($"#{imageNum} tiles range: {patternTableStartOffsetShared}-{tileID - 1}");
                    else if (tileID > patternTableStartOffsets[imageNum])
                        Trace.WriteLine($"#{imageNum} tiles range: {patternTableStartOffsets[imageNum]}-{tileID - 1}");
                    else
                        Trace.WriteLine($"Pattern table is empty.");
                    if (tileID > 256 && !ignoreTilesRange)
                        throw new ArgumentOutOfRangeException("Tiles out of range.");

                    // Save pattern table to file
                    if (outPatternTable.ContainsKey(imageNum) && !sharePatternTable)
                    {
                        var patternTableReversed = patternTable.ToDictionary(kv => kv.Value, kv => kv.Key);
                        var patternTableRaw = new List<byte>();
                        for (int t = patternTableStartOffsets[imageNum]; t < tileID; t++)
                        {
                            var raw = patternTableReversed[t].GetAsPatternData();
                            patternTableRaw.AddRange(raw);
                        }
                        File.WriteAllBytes(outPatternTable[imageNum], patternTableRaw.ToArray());
                        Trace.WriteLine($"Pattern table #{imageNum} saved to {outPatternTable[imageNum]}");
                    }

                    // Save nametable to file
                    if (outNameTable.ContainsKey(imageNum))
                    {
                        if (mode != TilesMode.Backgrounds)
                            throw new InvalidOperationException("Nametable table generation available for backgrounds mode only.");
                        File.WriteAllBytes(outNameTable[imageNum], nameTable.Select(i => (byte)i).ToArray());
                        Trace.WriteLine($"Nametable #{imageNum} saved to {outNameTable[imageNum]}");
                    }
                }

                // Save shared pattern table to file
                if (sharePatternTable && outPatternTableShared != null)
                {
                    var patternTableReversed = patternTables[0].ToDictionary(kv => kv.Value, kv => kv.Key);
                    var patternTableRaw = new List<byte>();
                    for (int t = patternTableStartOffsetShared; t < tileID; t++)
                    {
                        var raw = patternTableReversed[t].GetAsPatternData();
                        patternTableRaw.AddRange(raw);
                    }
                    File.WriteAllBytes(outPatternTableShared, patternTableRaw.ToArray());
                    Trace.WriteLine($"Pattern table saved to {outPatternTableShared}");
                }

                // Save CSV tiles report
                if (outTilesCsv != null && outTilesCsvLines != null)
                {
                    File.WriteAllLines(outTilesCsv, outTilesCsvLines);
                }
                // Save CSV palettes report
                if (outPalettesCsv != null && outPalettesCsvLines != null)
                {
                    File.WriteAllLines(outPalettesCsv, outPalettesCsvLines);
                }

                return 0;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"Error. {ex.Message}");
                return 1;
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Can't parse JSON: {ex.Message}");
                return 1;
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is InvalidOperationException || ex is ArgumentOutOfRangeException || ex is FileNotFoundException)
            {
                Console.Error.WriteLine($"Error. {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error {ex.GetType()}: {ex.Message}{ex.StackTrace}");
                return 1;
            }
        }

        private static Dictionary<byte, Color> LoadColors(string filename)
        {
            Trace.WriteLine($"Loading colors from {filename}...");
            if (!File.Exists(filename)) throw new FileNotFoundException($"Could not find file '{filename}'.", filename);
            var data = File.ReadAllBytes(filename);
            Dictionary<byte, Color> nesColors;
            // Detect file type
            if ((Path.GetExtension(filename) == ".pal") || ((data.Length == 192 || data.Length == 1536) && data.Where(b => b >= 128).Any()))
            {
                // Binary file
                nesColors = new Dictionary<byte, Color>();
                for (byte c = 0; c < 64; c++)
                {
                    var color = Color.FromArgb(data[c * 3], data[c * 3 + 1], data[c * 3 + 2]);
                    nesColors[c] = color;
                }
            }
            else
            {
                var paletteJson = File.ReadAllText(filename);
                var nesColorsStr = JsonSerializer.Deserialize<Dictionary<string, string>>(paletteJson);
                if (nesColorsStr == null) throw new InvalidDataException($"Can't parse {filename}");
                nesColors = nesColorsStr.ToDictionary(
                    kv =>
                    {
                        try
                        {
                            var index = kv.Key.ToLower().StartsWith("0x") ? Convert.ToByte(kv.Key.Substring(2), 16) : byte.Parse(kv.Key);
                            if (FORBIDDEN_COLORS.Contains(index))
                                Trace.WriteLine($"WARNING! color #{kv.Key} is forbidden color, it will be ignored.");
                            if (index > 0x3D) throw new ArgumentException($"{kv.Key} - invalid color index.", filename);
                            return index;
                        }
                        catch (Exception ex) when (ex is FormatException || ex is OverflowException)
                        {
                            throw new ArgumentException($"{kv.Key} - invalid color index.", filename);
                        }
                    },
                    kv =>
                    {
                        try
                        {
                            return ColorTranslator.FromHtml(kv.Value);
                        }
                        catch (FormatException)
                        {
                            throw new ArgumentException($"{kv.Value} - invalid color.", filename);
                        }
                    }
                );
            }
            // filter out invalid colors;
            nesColors = nesColors.Where(kv => !FORBIDDEN_COLORS.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            return nesColors;
        }

        static void WriteColorsTable(Dictionary<byte, Color> nesColors, string filename)
        {
            // Export colors to nice table image
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

        static Palette[] CalculatePalettes(Dictionary<int, FastBitmap> images, bool[] paletteEnabled, Palette?[] fixedPalettes, Dictionary<int, int> attributeTableOffsets, int tilePalWidth, int tilePalHeight, Color bgColor)
        {
            var required = Enumerable.Range(0, 4).Select(i => paletteEnabled[i] && fixedPalettes[i] == null);
            // Creating and counting the palettes
            var paletteCounter = new Dictionary<Palette, int>();
            foreach (var imageNum in images.Keys)
            {
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

        static byte FindSimilarColor(Dictionary<byte, Color> colors, Color color, Dictionary<Color, byte>? cache = null)
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
                throw new KeyNotFoundException($"Invalid color: {color}.");
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
