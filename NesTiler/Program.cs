using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
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
        public const string REPO_PATH = "https://github.com/ClusterM/NesTiler";
        public static DateTime BUILD_TIME = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(Properties.Resources.buildtime.Trim()));
        public const int MAX_BG_COLOR_AUTODETECT_ITERATIONS = 5;

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
            foreach (var arg in IArg.Args)
            {
                var s = "-" + (arg.HasIndex ? (arg.Short + "<#>") : arg.Short);
                var l = "--" + (arg.HasIndex ? (arg.Long + "-<#>") : arg.Long) + (arg.Params != null ? " " + arg.Params : "");
                var description = arg.Description.Replace("\n", "\n" + String.Join("", Enumerable.Repeat(" ", 48)));
                Console.WriteLine("{0,-5} {1,-42}{2}", s, l, description);
            }
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

                var c = Config.Parse(args);
                Trace.Listeners.Clear();
                if (!c.quiet)
                {
                    PrintAppInfo();
                    Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                }
                if (!c.ImageFiles.Any() && string.IsNullOrEmpty(c.OutColorsTable))
                {
                    Console.WriteLine("Nothing to do.");
                    Console.WriteLine();
                    PrintHelp();
                    return 1;
                }

                // Data
                var images = new Dictionary<int, FastBitmap>();
                var paletteIndexes = new Dictionary<int, byte[,]>();
                var patternTables = new Dictionary<int, Dictionary<Tile, int>>();
                var nameTables = new Dictionary<int, List<int>>();
                int tileID = 0;

                // Loading and parsing palette JSON
                var nesColors = new ColorsFinder(c.ColorsFile);

                // CSV output
                var outTilesCsvLines = !string.IsNullOrEmpty(c.OutTilesCsv) ? new List<string>() : null;
                var outPalettesCsvLines = !string.IsNullOrEmpty(c.OutPalettesCsv) ? new List<string>() : null;

                if (c.OutColorsTable != null)
                {
                    Trace.WriteLine($"Writing color tables to {c.OutColorsTable}...");
                    nesColors.WriteColorsTable(c.OutColorsTable);
                }

                // Stop if there are no images
                if (!c.ImageFiles.Any()) return 0;

                // Change the fixed palettes to colors from the NES palette
                for (int i = 0; i < c.FixedPalettes.Length; i++)
                {
                    if (c.FixedPalettes[i] == null) continue;
                    var colorsInPalette = c.FixedPalettes[i]!.ToArray();
                    for (int j = 0; j < colorsInPalette.Length; j++)
                        colorsInPalette[j] = nesColors.FindSimilarColor(colorsInPalette[j]);
                    c.FixedPalettes[i] = new Palette(colorsInPalette);
                }

                // Loading images
                foreach (var imageFile in c.ImageFiles)
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

                    if (c.Mode == Config.TilesMode.Backgrounds && image.Width != 256) throw new ArgumentException("Image width must be 256 for backgrounds mode.", filename);
                    if (image.Width % c.TileWidth != 0) throw new ArgumentException($"Image width must be divisible by {c.TileWidth}.", filename);
                    if (image.Height % c.TileHeight != 0) throw new ArgumentException($"Image height must be divisible by {c.TileHeight}.", filename);
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
                            if (color.A >= 0x80 || c.Mode == Config.TilesMode.Backgrounds)
                            {
                                var similarColor = nesColors.FindSimilarColor(color);
                                image.SetPixelColor(x, y, similarColor);
                            }
                            else
                            {
                                if (!c.BgColor.HasValue) throw new InvalidDataException("You must specify background color for images with transparency.");
                                image.SetPixelColor(x, y, c.BgColor.Value);
                            }
                        }
                    }
                }

                List<Palette> calculatedPalettes;
                var maxCalculatedPaletteCount = Enumerable.Range(0, 4)
                    .Select(i => c.PaletteEnabled[i] && c.FixedPalettes[i] == null).Count();
                Color bgColor;
                // Detect background color
                if (c.BgColor.HasValue)
                {
                    // Manually
                    bgColor = nesColors.FindSimilarColor(c.BgColor.Value);
                    calculatedPalettes = CalculatePalettes(images,
                                                           c.PaletteEnabled,
                                                           c.FixedPalettes,
                                                           c.PattributeTableYOffsets,
                                                           c.TilePalWidth,
                                                           c.TilePalHeight,
                                                           c.BgColor.Value).ToList();
                }
                else
                {
                    // Autodetect most used color
                    Trace.Write($"Background color autodetect... ");
                    Dictionary<Color, int> colorPerTileCounter = new Dictionary<Color, int>();
                    foreach (var imageNum in images.Keys)
                    {
                        var image = images[imageNum];
                        for (int tileY = 0; tileY < image.Height / c.TilePalHeight; tileY++)
                        {
                            for (int tileX = 0; tileX < image.Width / c.TilePalWidth; tileX++)
                            {
                                // Count each color only once per tile/sprite
                                var colorsInTile = new List<Color>();
                                for (int y = 0; y < c.TilePalHeight; y++)
                                {
                                    for (int x = 0; x < c.TilePalWidth; x++)
                                    {
                                        var color = image.GetPixelColor((tileX * c.TilePalWidth) + x, (tileY * c.TilePalHeight) + y);
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
                        calcResults[candidates[i]] = CalculatePalettes(images,
                                                                       c.PaletteEnabled,
                                                                       c.FixedPalettes,
                                                                       c.PattributeTableYOffsets,
                                                                       c.TilePalWidth,
                                                                       c.TilePalHeight,
                                                                       candidates[i]);
                    }
                    // Select background color which uses minimum palettes
                    var kv = calcResults.OrderBy(kv => kv.Value.Length).First();
                    (bgColor, calculatedPalettes) = (kv.Key, kv.Value.ToList());
                    Trace.WriteLine(ColorTranslator.ToHtml(bgColor));
                }

                if (calculatedPalettes.Count > maxCalculatedPaletteCount && !c.Lossy)
                {
                    throw new ArgumentOutOfRangeException($"Can't fit {calculatedPalettes.Count} palettes, {maxCalculatedPaletteCount} is maximum.");
                }

                // Select palettes
                var palettes = new Palette?[4] { null, null, null, null };
                outPalettesCsvLines?.Add("palette_id,color0,color1,color2,color3");
                for (var i = 0; i < palettes.Length; i++)
                {
                    if (c.PaletteEnabled[i])
                    {
                        if (c.FixedPalettes[i] != null)
                        {
                            palettes[i] = c.FixedPalettes[i];
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
                            Trace.WriteLine($"Palette #{i}: {ColorTranslator.ToHtml(bgColor)}(BG) {string.Join(" ", palettes[i]!.Select(p => ColorTranslator.ToHtml(p)))}");
                            // Write CSV if required
                            outPalettesCsvLines?.Add($"{i},{ColorTranslator.ToHtml(bgColor)},{string.Join(",", Enumerable.Range(1, 3).Select(c => (palettes[i]![c] != null ? ColorTranslator.ToHtml(palettes[i]![c]!.Value) : "")))}");
                        }
                    }
                }

                // Calculate palette as color indices and save them to files
                var bgColorIndex = nesColors.FindSimilarColorIndex(bgColor);
                for (int p = 0; p < palettes.Length; p++)
                {
                    if (c.PaletteEnabled[p] && c.OutPalette.ContainsKey(p))
                    {
                        var paletteRaw = new byte[4];
                        paletteRaw[0] = bgColorIndex;
                        for (int colorIndex = 1; colorIndex <= 3; colorIndex++)
                        {
                            if (palettes[p] == null)
                                paletteRaw[colorIndex] = 0;
                            else if (palettes[p]![colorIndex].HasValue)
                                paletteRaw[colorIndex] = nesColors.FindSimilarColorIndex(palettes[p]![colorIndex]!.Value);
                        }
                        File.WriteAllBytes(c.OutPalette[p], paletteRaw);
                        Trace.WriteLine($"Palette #{p} saved to {c.OutPalette[p]}");
                    }
                }

                // Select palette for each tile/sprite and recolorize using them
                foreach (var imageNum in images.Keys)
                {
                    Trace.WriteLine($"Mapping palettes for image #{imageNum}...");
                    var image = images[imageNum];
                    int attributeTableOffset;
                    c.PattributeTableYOffsets.TryGetValue(imageNum, out attributeTableOffset);
                    paletteIndexes[imageNum] = new byte[image.Width / c.TilePalWidth, (int)Math.Ceiling((image.Height + attributeTableOffset) / (float)c.TilePalHeight)];
                    // For each tile/sprite
                    for (int tilePalY = 0; tilePalY < (int)Math.Ceiling((image.Height + attributeTableOffset) / (float)c.TilePalHeight); tilePalY++)
                    {
                        for (int tilePalX = 0; tilePalX < image.Width / c.TilePalWidth; tilePalX++)
                        {
                            double minDelta = double.MaxValue;
                            byte bestPaletteIndex = 0;
                            // Try each palette
                            for (byte paletteIndex = 0; paletteIndex < palettes.Length; paletteIndex++)
                            {
                                if (palettes[paletteIndex] == null) continue;
                                double delta = palettes[paletteIndex]!.GetTileDelta(
                                    image, tilePalX * c.TilePalWidth, (tilePalY * c.TilePalHeight) - attributeTableOffset,
                                    c.TilePalWidth, c.TilePalHeight, bgColor);
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
                            for (int y = 0; y < c.TilePalHeight; y++)
                            {
                                for (int x = 0; x < c.TilePalWidth; x++)
                                {
                                    var cy = (tilePalY * c.TilePalHeight) + y - attributeTableOffset;
                                    if (cy < 0) continue;
                                    var color = image.GetPixelColor((tilePalX * c.TilePalWidth) + x, cy);
                                    var similarColor = nesColors.FindSimilarColor(Enumerable.Concat(
                                            bestPalette,
                                            new Color[] { bgColor }
                                        ), color);
                                    image.SetPixelColor(
                                        (tilePalX * c.TilePalWidth) + x,
                                        cy,
                                        similarColor);
                                }
                            }
                        } // tile X
                    } // tile Y

                    // Save preview if required
                    if (c.OutPreview.ContainsKey(imageNum))
                    {
                        File.WriteAllBytes(c.OutPreview[imageNum], image.Encode(SKEncodedImageFormat.Png, 0));
                        Trace.WriteLine($"Preview #{imageNum} saved to {c.OutPreview[imageNum]}");
                    }
                }

                // Generate attribute tables
                foreach (var imageNum in c.OutAttributeTable.Keys)
                {
                    if (c.Mode != Config.TilesMode.Backgrounds)
                        throw new InvalidOperationException("Attribute table generation available for backgrounds mode only.");
                    Trace.WriteLine($"Creating attribute table for image #{imageNum}...");
                    var image = images[imageNum];
                    int attributeTableOffset;
                    c.PattributeTableYOffsets.TryGetValue(imageNum, out attributeTableOffset);
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
                    if (c.OutAttributeTable.ContainsKey(imageNum))
                    {
                        File.WriteAllBytes(c.OutAttributeTable[imageNum], attributeTableRaw.ToArray());
                        Trace.WriteLine($"Attribute table #{imageNum} saved to {c.OutAttributeTable[imageNum]}");
                    }
                }

                // Generate pattern tables and nametables
                outTilesCsvLines?.Add("image_id,image_file,line,column,tile_x,tile_y,tile_width,tile_height,tile_id,palette_id");
                foreach (var imageNum in images.Keys)
                {
                    Trace.WriteLine($"Creating pattern table for image #{imageNum}...");
                    var image = images[imageNum];
                    int attributeTableOffset;
                    c.PattributeTableYOffsets.TryGetValue(imageNum, out attributeTableOffset);
                    if (!patternTables.ContainsKey(!c.SharePatternTable ? imageNum : 0)) patternTables[!c.SharePatternTable ? imageNum : 0] = new Dictionary<Tile, int>();
                    var patternTable = patternTables[!c.SharePatternTable ? imageNum : 0];
                    if (!nameTables.ContainsKey(imageNum)) nameTables[imageNum] = new List<int>();
                    var nameTable = nameTables[imageNum];
                    if (!c.SharePatternTable)
                    {
                        if (!c.PatternTableStartOffsets.ContainsKey(imageNum))
                            c.PatternTableStartOffsets[imageNum] = 0;
                        tileID = c.PatternTableStartOffsets[imageNum];
                    }
                    else
                    {
                        tileID = Math.Max(tileID, c.PatternTableStartOffsetShared);
                        c.PatternTableStartOffsets[imageNum] = tileID;
                    }

                    for (int tileY = 0; tileY < image.Height / c.TileHeight; tileY++)
                    {
                        for (int tileX = 0; tileX < image.Width / c.TileWidth; tileX++)
                        {
                            var tileData = new byte[c.TileWidth * c.TileHeight];
                            byte paletteID = 0;
                            for (int y = 0; y < c.TileHeight; y++)
                                for (int x = 0; x < c.TileWidth; x++)
                                {
                                    var color = image.GetPixelColor((tileX * c.TileWidth) + x, (tileY * c.TileHeight) + y);
                                    paletteID = paletteIndexes[imageNum][tileX / (c.TilePalWidth / c.TileWidth), (tileY + (attributeTableOffset / c.TileHeight)) / (c.TilePalHeight / c.TileHeight)];
                                    var palette = palettes[paletteID];
                                    byte colorIndex = 0;
                                    if (color != bgColor)
                                    {
                                        colorIndex = 1;
                                        while (palette![colorIndex] != color) colorIndex++;
                                    }
                                    tileData[(y * c.TileWidth) + x] = colorIndex;
                                }
                            var tile = new Tile(tileData, c.TileHeight);
                            int currentTileID, id;
                            if (patternTable.TryGetValue(tile, out id))
                            {
                                if (c.Mode == Config.TilesMode.Backgrounds) nameTable.Add(id);
                                currentTileID = id;
                            }
                            else
                            {
                                patternTable[tile] = tileID;
                                if (c.Mode == Config.TilesMode.Backgrounds) nameTable.Add(tileID);
                                currentTileID = tileID;
                                tileID++;
                            }
                            currentTileID = ((currentTileID & 0x7F) << 1) | ((currentTileID & 0x80) >> 7);

                            // Write CSV if required
                            outTilesCsvLines?.Add($"{imageNum},{c.ImageFiles[imageNum]},{tileY},{tileX},{tileX * c.TileWidth},{tileY * c.TileHeight},{c.TileWidth},{c.TileHeight},{currentTileID},{paletteID}");
                        }
                    }
                    if (c.SharePatternTable && tileID > c.PatternTableStartOffsetShared)
                        Trace.WriteLine($"#{imageNum} tiles range: {c.PatternTableStartOffsetShared}-{tileID - 1}");
                    else if (tileID > c.PatternTableStartOffsets[imageNum])
                        Trace.WriteLine($"#{imageNum} tiles range: {c.PatternTableStartOffsets[imageNum]}-{tileID - 1}");
                    else
                        Trace.WriteLine($"Pattern table is empty.");
                    if (tileID > 256 && !c.IgnoreTilesRange)
                        throw new ArgumentOutOfRangeException("Tiles out of range.");

                    // Save pattern table to file
                    if (c.OutPatternTable.ContainsKey(imageNum) && !c.SharePatternTable)
                    {
                        var patternTableReversed = patternTable.ToDictionary(kv => kv.Value, kv => kv.Key);
                        var patternTableRaw = new List<byte>();
                        for (int t = c.PatternTableStartOffsets[imageNum]; t < tileID; t++)
                        {
                            var raw = patternTableReversed[t].GetAsPatternData();
                            patternTableRaw.AddRange(raw);
                        }
                        File.WriteAllBytes(c.OutPatternTable[imageNum], patternTableRaw.ToArray());
                        Trace.WriteLine($"Pattern table #{imageNum} saved to {c.OutPatternTable[imageNum]}");
                    }

                    // Save nametable to file
                    if (c.OutNameTable.ContainsKey(imageNum))
                    {
                        if (c.Mode != Config.TilesMode.Backgrounds)
                            throw new InvalidOperationException("Nametable table generation available for backgrounds mode only.");
                        File.WriteAllBytes(c.OutNameTable[imageNum], nameTable.Select(i => (byte)i).ToArray());
                        Trace.WriteLine($"Nametable #{imageNum} saved to {c.OutNameTable[imageNum]}");
                    }
                }

                // Save shared pattern table to file
                if (c.SharePatternTable && c.OutPatternTableShared != null)
                {
                    var patternTableReversed = patternTables[0].ToDictionary(kv => kv.Value, kv => kv.Key);
                    var patternTableRaw = new List<byte>();
                    for (int t = c.PatternTableStartOffsetShared; t < tileID; t++)
                    {
                        var raw = patternTableReversed[t].GetAsPatternData();
                        patternTableRaw.AddRange(raw);
                    }
                    File.WriteAllBytes(c.OutPatternTableShared, patternTableRaw.ToArray());
                    Trace.WriteLine($"Pattern table saved to {c.OutPatternTableShared}");
                }

                // Save CSV tiles report
                if (c.OutTilesCsv != null && outTilesCsvLines != null)
                {
                    File.WriteAllLines(c.OutTilesCsv, outTilesCsvLines);
                }
                // Save CSV palettes report
                if (c.OutPalettesCsv != null && outPalettesCsvLines != null)
                {
                    File.WriteAllLines(c.OutPalettesCsv, outPalettesCsvLines);
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
    }
}
