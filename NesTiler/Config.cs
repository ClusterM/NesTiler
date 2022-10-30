using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace com.clusterrr.Famicom.NesTiler
{
    class Config
    {
        public const string DEFAULT_COLORS_FILE = @"nestiler-colors.json";

        public enum TilesMode
        {
            Backgrounds,
            Sprites8x8,
            Sprites8x16
        }

        public string ColorsFile { get; private set; }
        public Dictionary<int, string> ImageFiles { get; private set; } = new Dictionary<int, string>();
        public SKColor? BgColor { get; private set; } = null;
        public bool[] PaletteEnabled { get; private set; } = new bool[4] { true, true, true, true };
        public Palette?[] FixedPalettes { get; private set; } = new Palette?[4] { null, null, null, null };
        public TilesMode Mode { get; private set; } = TilesMode.Backgrounds;
        public int TileWidth { get; private set; } = 8;
        public int TileHeight { get; private set; } = 8;
        public int TilePalWidth { get; private set; } = 16;
        public int TilePalHeight { get; private set; } = 16;
        public bool SharePatternTable { get; private set; } = false;
        public int LossyLevel { get; private set; } = 2;
        public int PatternTableStartOffsetShared { get; private set; } = 0;
        public Dictionary<int, int> PatternTableStartOffsets { get; private set; } = new Dictionary<int, int>();
        public Dictionary<int, int> PattributeTableYOffsets { get; private set; } = new Dictionary<int, int>();
        public bool Quiet { get; private set; } = false;

        // Filenames
        public Dictionary<int, string> OutPreview { get; private set; } = new Dictionary<int, string>();
        public Dictionary<int, string> OutPalette { get; private set; } = new Dictionary<int, string>();
        public Dictionary<int, string> OutPatternTable { get; private set; } = new Dictionary<int, string>();
        public Dictionary<int, string> OutNameTable { get; private set; } = new Dictionary<int, string>();
        public Dictionary<int, string> OutAttributeTable { get; private set; } = new Dictionary<int, string>();
        public string? OutPatternTableShared { get; private set; } = null;
        public string? OutTilesCsv { get; private set; } = null;
        public string? OutPalettesCsv { get; private set; } = null;
        public string? OutColorsTable { get; private set; } = null;

        private Config()
        {
            ColorsFile = Path.Combine(AppContext.BaseDirectory, DEFAULT_COLORS_FILE);
            if (!File.Exists(ColorsFile))
                ColorsFile = Path.Combine(Directory.GetCurrentDirectory(), DEFAULT_COLORS_FILE);
            if (!File.Exists(ColorsFile) && !OperatingSystem.IsWindows())
                ColorsFile = Path.Combine("/etc", DEFAULT_COLORS_FILE);
        }

        public static Config Parse(string[] args)
        {
            Config config = new Config();
            var paramRegex = new Regex(@"^--?(?<param>[a-zA-Z-]+?)-?(?<index>[0-9]*)$");
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
                    case ArgIn.S:
                    case ArgIn.L:
                        config.ImageFiles[indexNum] = value;
                        i++;
                        break;
                    case ArgColors.S:
                    case ArgColors.L:
                        config.ColorsFile = value;
                        i++;
                        break;
                    case ArgMode.S:
                    case ArgMode.L:
                        switch (value.ToLower())
                        {
                            case "sprite":
                            case "sprites":
                            case "sprites8x8":
                                config.Mode = TilesMode.Sprites8x8;
                                config.TileWidth = 8;
                                config.TileHeight = 8;
                                config.TilePalWidth = 8;
                                config.TilePalHeight = 8;
                                break;
                            case "sprite8x16":
                            case "sprites8x16":
                                config.Mode = TilesMode.Sprites8x16;
                                config.TileWidth = 8;
                                config.TileHeight = 16;
                                config.TilePalWidth = 8;
                                config.TilePalHeight = 16;
                                break;
                            case "bg":
                            case "background":
                            case "backgrounds":
                                config.Mode = TilesMode.Backgrounds;
                                config.TileWidth = 8;
                                config.TileHeight = 8;
                                config.TilePalWidth = 16;
                                config.TilePalHeight = 16;
                                break;
                            default:
                                throw new ArgumentException($"{value} - invalid mode.", param);
                        }
                        i++;
                        break;
                    case ArgBgColor.S:
                    case ArgBgColor.L:
                        if (value != "auto")
                        {
                            try
                            {
                                config.BgColor = ColorTranslator.FromHtml(value).ToSKColor();
                            }
                            catch (FormatException)
                            {
                                throw new ArgumentException($"{value} - invalid color.", param);
                            }
                        }
                        i++;
                        break;
                    case ArgEnablePalettes.S:
                    case ArgEnablePalettes.L:
                        {
                            var paletteNumbersStr = value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int pal = 0; pal < config.PaletteEnabled.Length; pal++)
                                config.PaletteEnabled[pal] = false; // disable all palettes
                            foreach (var palNumStr in paletteNumbersStr)
                            {
                                if (!int.TryParse(palNumStr, out valueInt))
                                    throw new ArgumentException($"\"{palNumStr}\" is not valid integer value.", param);
                                if (valueInt < 0 || valueInt > 3)
                                    throw new ArgumentException($"Palette index must be between 0 and 3.", param);
                                config.PaletteEnabled[valueInt] = true;
                            }
                            if (!config.PaletteEnabled.Where(p => p).Any()) // will never be executed?
                                throw new ArgumentException($"You need to enable at least one palette.", param);
                        }
                        i++;
                        break;
                    case ArgPalette.S:
                    case ArgPalette.L:
                        {
                            var colors = value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(c =>
                            {
                                try
                                {
                                    return ColorTranslator.FromHtml(c).ToSKColor();
                                }
                                catch (FormatException)
                                {
                                    throw new ArgumentException($"{c} - invalid color.", param);
                                }
                            });
                            config.FixedPalettes[indexNum] = new Palette(colors);
                        }
                        i++;
                        break;
                    case ArgPatternOffset.S:
                    case ArgPatternOffset.L:
                        if (!int.TryParse(value, out valueInt))
                            throw new ArgumentException($"\"{value}\" is not valid integer value.", param);
                        if (valueInt < 0 || valueInt >= 256)
                            throw new ArgumentException($"Value ({valueInt}) must be between 0 and 255.", param);
                        config.PatternTableStartOffsets[indexNum] = valueInt;
                        config.PatternTableStartOffsetShared = config.PatternTableStartOffsets[indexNum];
                        i++;
                        break;
                    case ArgAttributeTableYOffset.S:
                    case ArgAttributeTableYOffset.L:
                        if (!int.TryParse(value, out valueInt))
                            throw new ArgumentException($"\"{value}\" is not valid integer value.", param);
                        if (valueInt % 8 != 0)
                            throw new ArgumentException($"Value ({valueInt}) must be divisible by 8.", param);
                        if (valueInt < 0 || valueInt >= 256)
                            throw new ArgumentException($"Value ({valueInt}) must be between 0 and 255.", param);
                        config.PattributeTableYOffsets[indexNum] = valueInt;
                        i++;
                        break;
                    case ArgSharePatternTable.S:
                    case ArgSharePatternTable.L:
                        config.SharePatternTable = true;
                        break;
                    case ArgLossy.S:
                    case ArgLossy.L:
                        if (!int.TryParse(value, out valueInt))
                            throw new ArgumentException($"\"{value}\" is not valid integer value.", param);
                        if (valueInt < 0 || valueInt > 3)
                            throw new ArgumentException($"Value ({valueInt}) must be between 0 and 3.", param);
                        config.LossyLevel = valueInt;
                        i++;
                        break;
                    case ArgOutPreview.S:
                    case ArgOutPreview.L:
                        config.OutPreview[indexNum] = value;
                        i++;
                        break;
                    case ArgOutPalette.S:
                    case ArgOutPalette.L:
                        if (indexNum < 0 || indexNum > 3)
                            throw new ArgumentException($"Palette index must be between 0 and 3.", param);
                        config.OutPalette[indexNum] = value;
                        i++;
                        break;
                    case ArgOutPatternTable.S:
                    case ArgOutPatternTable.L:
                        config.OutPatternTable[indexNum] = value;
                        config.OutPatternTableShared = value;
                        i++;
                        break;
                    case ArgOutNameTable.S:
                    case ArgOutNameTable.L:
                        config.OutNameTable[indexNum] = value;
                        i++;
                        break;
                    case ArgOutAttributeTable.S:
                    case ArgOutAttributeTable.L:
                        config.OutAttributeTable[indexNum] = value;
                        i++;
                        break;
                    case ArgOutTilesCsv.S:
                    case ArgOutTilesCsv.L:
                        config.OutTilesCsv = value;
                        i++;
                        break;
                    case ArgOutPalettesCsv.S:
                    case ArgOutPalettesCsv.L:
                        config.OutPalettesCsv = value;
                        i++;
                        break;
                    case ArgOutColorsTable.S:
                    case ArgOutColorsTable.L:
                        config.OutColorsTable = value;
                        i++;
                        break;
                    case ArgQuiet.S:
                    case ArgQuiet.L:
                        config.Quiet = true;
                        break;
                    default:
                        throw new ArgumentException($"Unknown argument.", args[i]);
                }
            }

            // Some input data checks
            switch (config.Mode)
            {
                case TilesMode.Sprites8x8:
                case TilesMode.Sprites8x16:
                    if (!config.BgColor.HasValue) throw new InvalidDataException("You must specify background color for sprites mode.");
                    break;
            }
            // Check output files
            foreach (var c in new Dictionary<int, string>[] { config.OutPreview, config.OutPatternTable, config.OutNameTable, config.OutAttributeTable })
                foreach (var f in c)
                    if (!config.ImageFiles.ContainsKey(f.Key))
                        throw new ArgumentException($"Can't write {f.Value} - there is no input image with index {f.Key}.");

            return config;
        }
    }
}
