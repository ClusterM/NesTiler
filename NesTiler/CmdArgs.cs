namespace com.clusterrr.Famicom.NesTiler
{
    interface IArg
    {
        public string Short { get; }
        public string Long { get; }
        public string? Params { get; }
        public string Description { get; }
        public bool HasIndex { get; }

        public static IArg[] Args = new IArg[]
        {
            new ArgIn(),
            new ArgColors(),
            new ArgMode(),
            new ArgBgColor(),
            new ArgEnablePalettes(),
            new ArgPalette(),
            new ArgPatternOffset(),
            new ArgAttributeTableYOffset(),
            new ArgSharePatternTable(),
            new ArgLossy(),
            new ArgOutPreview(),
            new ArgOutPalette(),
            new ArgOutPatternTable(),
            new ArgOutNameTable(),
            new ArgOutAttributeTable(),
            new ArgOutTilesCsv(),
            new ArgOutPalettesCsv(),
            new ArgOutColorsTable(),
            new ArgQuiet()
        };
    }

    class ArgIn : IArg
    {
        public const string S = "i";
        public const string L = "in";
        public string? Params { get; } = "<filename>[:offset[:height]]";
        public string Description { get; } = "input filename number #, optionally cropped vertically";
        public bool HasIndex { get; } = true;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgColors : IArg
    {
        public const string S = "c";
        public const string L = "colors";
        public string? Params { get; } = "<filename>";
        public string Description { get; } = $"JSON or PAL file with the list of available colors\n(default - {Config.DEFAULT_COLORS_FILE})";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgMode : IArg
    {
        public const string S = "m";
        public const string L = "mode";
        public string? Params { get; } = "bg|sprites8x8|sprites8x16";
        public string Description { get; } = "mode: backgrounds, 8x8 sprites or 8x16 sprites (default - bg)";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgBgColor : IArg
    {
        public const string S = "b";
        public const string L = "bg-color";
        public string? Params { get; } = "<color>";
        public string Description { get; } = "background color in HTML color format (default - auto)";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgEnablePalettes : IArg
    {
        public const string S = "e";
        public const string L = "enable-palettes";
        public string? Params { get; } = "<palettes>";
        public string Description { get; } = "zero-based comma separated list of palette numbers to use\n(default - 0,1,2,3)";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgPalette : IArg
    {
        public const string S = "p";
        public const string L = "palette";
        public string? Params { get; } = "<colors>";
        public string Description { get; } = "comma separated list of colors to use in palette number #\n(default - auto)";
        public bool HasIndex { get; } = true;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgPatternOffset : IArg
    {
        public const string S = "o";
        public const string L = "pattern-offset";
        public string? Params { get; } = "<tile_index>";
        public string Description { get; } = "first tile index for pattern table for file number # (default - 0)";
        public bool HasIndex { get; } = true;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgAttributeTableYOffset : IArg
    {
        public const string S = "y";
        public const string L = "attribute-table-y-offset";
        public string? Params { get; } = "<pixels>";
        public string Description { get; } = "vertical offset for attribute table in pixels (default - 0)";
        public bool HasIndex { get; } = true;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgSharePatternTable : IArg
    {
        public const string S = "s";
        public const string L = "share-pattern-table";
        public string? Params { get; } = null;
        public string Description { get; } = "share pattern table between input images";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgLossy : IArg
    {
        public const string S = "l";
        public const string L = "lossy";
        public string? Params { get; } = "<level>";
        public string Description { get; } = "lossy level: 0-3, defines how many color distortion is allowed\nwithout throwing an error (default - 2)";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgOutPreview : IArg
    {
        public const string S = "v";
        public const string L = "out-preview";
        public string? Params { get; } = "<filename.png>";
        public string Description { get; } = "output filename for preview of image number #";
        public bool HasIndex { get; } = true;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgOutPalette : IArg
    {
        public const string S = "t";
        public const string L = "out-palette";
        public string? Params { get; } = "<filename>";
        public string Description { get; } = "output filename for palette number #";
        public bool HasIndex { get; } = true;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgOutPatternTable : IArg
    {
        public const string S = "n";
        public const string L = "out-pattern-table";
        public string? Params { get; } = "<filename>";
        public string Description { get; } = "output filename for pattern table of image number #";
        public bool HasIndex { get; } = true;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgOutNameTable : IArg
    {
        public const string S = "a";
        public const string L = "out-name-table";
        public string? Params { get; } = "<filename>";
        public string Description { get; } = "output filename for nametable of image number #";
        public bool HasIndex { get; } = true;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgOutAttributeTable : IArg
    {
        public const string S = "u";
        public const string L = "out-attribute-table";
        public string? Params { get; } = "<filename>";
        public string Description { get; } = "output filename for attribute table of image number #";
        public bool HasIndex { get; } = true;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgOutTilesCsv : IArg
    {
        public const string S = "z";
        public const string L = "out-tiles-csv";
        public string? Params { get; } = "<filename.csv>";
        public string Description { get; } = "output filename for tiles info in CSV format";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgOutPalettesCsv : IArg
    {
        public const string S = "x";
        public const string L = "out-palettes-csv";
        public string? Params { get; } = "<filename.csv>";
        public string Description { get; } = "output filename for palettes info in CSV format";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgOutColorsTable : IArg
    {
        public const string S = "g";
        public const string L = "out-colors-table";
        public string? Params { get; } = "<filename.png>";
        public string Description { get; } = "output filename for graphical table of available colors\n(from \"--colors\" option)";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }

    class ArgQuiet : IArg
    {
        public const string S = "q";
        public const string L = "quiet";
        public string? Params { get; } = null;
        public string Description { get; } = "suppress console output";
        public bool HasIndex { get; } = false;
        public string Short { get; } = S;
        public string Long { get; } = L;
    }
}
