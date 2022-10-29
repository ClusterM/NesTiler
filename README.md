# NesTiler
Tool for converting pictures into NES format: pattern tables, nametables, attribute tables and palettes.

## What does it do
When developing applications and games for NES, to display images, you need to split each image into tiles, combine tiles into nametables, select colors so that they do not go beyond the limits of the NES, and then convert all this into a format understandable for the NES. This tool at least partly helps to automate this process. The application can accept multiple images as input, the main point is to use single set of palettes (and tiles if required) for all of them, so it's possible to switch CHR banks and base nametable on a certain line, in the middle of rendering process. You can't change palettes while image renders, so palette set must be the same for all images.

The sequence of actions is as follows:
* Load available NES colors from JSON or PAL file
* Load images, crop them if need
* Change every pixel of each image, so it's matches most similar color from available NES colors
* Calculate desired number of palettes to fit every image or at least trying to do it
* Generate attribute table for each image, assign palette index for each tiles set
* Change colors of every tile to match assigned palette index (if need)
* Create set of tiles, trying to fit them into 256, grouping same tiles into one
* Generate pattern table and nametable for each image

## How to use

```
Available options:
Usage: nestiler <options>

Available options:
-i<#> --in-<#> <filename>[:offset[:height]]     input filename number #, optionally cropped vertically
-c    --colors <filename>                       JSON or PAL file with the list of available colors
                                                (default - nestiler-colors.json)
-m    --mode bg|sprites8x8|sprites8x16          mode: backgrounds, 8x8 sprites or 8x16 sprites (default - bg)
-b    --bg-color <color>                        background color in HTML color format (default - auto)
-e    --enable-palettes <palettes>              zero-based comma separated list of palette numbers to use
                                                (default - 0,1,2,3)
-p<#> --palette-<#> <colors>                    comma separated list of colors to use in palette number #
                                                (default - auto)
-o<#> --pattern-offset-<#> <tile_id>            first tile ID for pattern table for file number # (default - 0)
-y<#> --attribute-table-y-offset-<#> <pixels>   vertical offset for attribute table in pixels (default - 0)
-s    --share-pattern-table                     vertical offset for attribute table in pixels (default - 0)
-l    --lossy <level>                           lossy level: 0-3, defines how many color distortion is allowed
                                                without throwing an error (default - 2)
-v<#> --out-preview-<#> <filename.png>          output filename for preview of image number #
-t<#> --out-palette-<#> <filename>              output filename for palette number #
-n<#> --out-pattern-table-<#> <filename>        output filename for pattern table of image number #
-a<#> --out-name-table-<#> <filename>           output filename for nametable of image number #
-u<#> --out-attribute-table-<#> <filename>      output filename for attribute table of image number #
-z    --out-tiles-csv <filename.csv>            output filename for tiles info in CSV format
-x    --out-palettes-csv <filename.csv>         output filename for palettes info in CSV format
-g    --out-colors-table <filename.png>         output filename for graphical table of available colors
                                                (from "--colors" option)
-q    --quiet                                   suppress console output
```

### Option -i<#>, --in-<#> \<file\>[:offset[:height]]
Option to specify input images filenames. You need to replace __#__ with image index (any number), so you can specify multiple images. Index will be used to identify output filenames.

Examples:
* nestiler -i0 image1.png -i1 image2.png -i2 image3.png ...
* nestiler --in-0 image1.png --in-1 image2.png --in-2 image3.png ...
 
Also, you can load image partically - split them horizontally, just add offset and height after colon. So if you need to split 256x240 image into two images:
* nestiler -i0 image.png:0:128 -i1 image.png:128:112

It's usefull if you need to show single image on screen but you want to split it into two 256-tiles pattern tables and switch them on specific line in the middle of rendering process.

### Option -c, --colors \<file\>
Option to specify file with available colors and indices. This file can be in JSON format (see nestiler-colors.json) or binary PAL format (used by emulators).

Examples:
* nestiler -c nestiler-colors.json ...
* nestiler --colors nestiler-colors.json ...

### Option -m, --mode bg|sprites8x8|sprites8x16
Option to specify processing mode: backgrounds, 8x8 sprites or 8x16 sprites. Default is backgrounds mode.

Examples:
* nestiler -m bg ...
* nestiler --mode sprites8x8 ...

### Option -b, --bg-color \<color\>
Background color in HTML format. Optional for background mode (will be set automatically) and required for sprite modes.

Examples:
* nestiler -b #C4C4C4 ...
* nestiler --bg-color #000000 ...

### Option -e, --enable-palettes \<palettes\>
List of palette numbers (4-color combinations) to use, zero-based, comma separated: from 0 to 3. Useful when you need to fit image into limited amount of palettes (when lossy level = 3) or get error if you can't fit in them (when lossy level < 2, see below). Default value is all - 0,1,2,3.

Examples:
* nestiler -e 0,1,2,3 ...
* nestiler --enable-palettes 0,1 ...

### Option -p<#>, --palette-<#> \<colors\>
Comma separated list of colors to use in palette number __#__ (0-3). Using this option you can manually specify palettes to use (three color sets), HTML format, comma separated. Three colors instead of four because background color shared between all palettes. Useful if you need fixed palette for other purposes, it can be shared with your image.

Examples:
* nestiler -p0 #747474,#A40000,#004400 -p2 #8000F0,#D82800,#FCFCFC ...
* nestiler --palette-0 #5C94FC,#FC7460,#FC9838 --palette-1 #80D010,#58F898,#787878 ...

Please note that index here is palette number, not input file number.

### Option -o<#>, --pattern-offset-<#> \<tile_id\>
Using this option you can set first tile index to use with image number __#__. Useful if you need to reserve some space in the begining of pattern table. Default valus is 0.

Examples:
* nestiler -o1 32 ...
* nestiler --pattern-offset-5 100 ...

__#__ number is ignored when the --share-pattern-table option is used (see below).

### Option -y<#>, --attribute-table-y-offset-<#> \<pixels\>
One attribute table byte stores four palette indices for 16 tiles (4x4 square). It can cause problems if your image should be displayed on lines whose numbers are not divisible by 32. Using this option you can set vertical image offset for image number __#__ - amount of pixels divisible by 8. Default value is 0. Please note that you need to care about unused bites manually.

Examples:
* nestiler -y1 32 ...
* nestiler --attribute-table-y-offset-0 16 ...

### Option -s, --share-pattern-table
Use this option if you need to share single pattern table between all input images. Useful if you need to scroll screen horizontally.

Examples:
* nestiler -s ...
* nestiler --share-pattern-table ...

### Option -l, --lossy \<level\>
Lossy level: 0-3, defines how many color distortion is allowed without throwing an error.

* 0 - throw error even if any pixel of any input image is not from NES colors (from file specified by __--colors__ option)
* 1 - ignore errors from level 0 by replacing every pixel color with most similar NES color, but throw error if any input image contains tile with more than four colors
* 2 - ignore errors from levels 0 and 1 by replacing unwanted colors with most similar available, but throw error if there are more than 4 (or less if __--enable-palettes__ option is used)
* 3 - ignore all color ploblems by replacing unwanted colors with most similar available
  
Default value is 2.

Examples:
* nestiler -l 0 ...
* nestiler --lossy 3 ...
  
### Option -v<#>, --out-preview-<#> \<file.png\>
Option to save preview for input image number __#__. Stored as PNG file. Useful if you need to preview result without compiling ROM. Preview is not saved if option is not specified.

Examples:
* nestiler -v0 preview.png -v1 preview2.png ...
* nestiler --out-preview-1 image.png --out-preview-2 image2.png ...

### Option -t<#>, --out-palette-<#> \<filename\>
Option to save generated palette number __#__. Just four bytes with color indices. Not saved if option is not specified.

Examples:
* nestiler -t0 palette0.bin -t0 palette2.bin ...
* nestiler --out-palette-1 palette1.bin --out-palette-2 palette2.bin ...

Please note that index here is palette number, not input file number.

### Option -n<#>, --out-pattern-table-<#> \<filename\>
Option to save generated pattern table for image number __#__. 16 bytes per tile, 960 bytes per full screen image. Not saved if option is not specified.

Examples:
* nestiler -n0 pattern0.bin -n1 pattern1.bin ...
* nestiler --out-pattern-table-2 out.bin --out-pattern-table-3 out2.bin ...

### Option -a<#>, --out-name-table-<#> \<filename\>           
Option to save generated nametable for image number __#__. 16 bytes per tile, 960 bytes per full screen image. Not saved if option is not specified.

Examples:
* nestiler -a2 nt2.bin -a3 nt3.bin ...
* nestiler --out-name-table-1 nametable.bin --out-name-table-2 nametable2.bin ...

### Option -u<#>, --out-attribute-table-<#> \<filename\>
Option to save generated attribute table for image number __#__. 1 byte per 16 tiles. 64 bytes per full screen image. Not saved if option is not specified. Can't be used in sprite modes.

Examples:
* nestiler -u0 attr_a.bin -u1 attr_b.bin ...
* nestiler --out-attribute-table-1 attrtable1.bin --out-attribute-table-2 attrtable2.bin ...

### Option -z, --out-tiles-csv \<filename.csv\>
Option to save CSV file with tiles information for all input images: indices, used palettes, etc. Not saved if option is not specified.

Examples:
* nestiler -z tiles.csv ...
* nestiler --out-tiles-csv tiles.csv ...

### Option -x, --out-palettes-csv \<filename.csv\>
Option to save CSV file with palettes data: indices and colors.

Examples:
* nestiler -x palettes.csv ...
* nestiler --out-palettes-csv palettes.csv ...

### Option -g, --out-colors-table \<filename.png\>
Option to generate PNG file with table of available NES colors (from __--colors__ option). Useful for reference when drawing. This option can be used without any input images.

Examples:
* nestiler -g colors.png
* nestiler --out-colors-table colors.png

### Option -q, --quiet
Just option to suppress console output.

Examples:
* nestiler -q
* nestiler -quiet ...
