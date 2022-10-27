# NesTiler
Tool for converting pictures into NES format: pattern tables, nametables, attribute tables and palettes.

## What does it do
When developing applications and games for NES, to display images, you need to split each image into tiles, combine tiles into nametables, select colors so that they do not go beyond the limits of the NES, and then convert all this into a format understandable for the NES. This tool at least partly helps to automate this process. The application can accept multiple images as input, the main point is to use single set of palettes (and tiles if required) for all of them, so it's possible to switch CHR banks and base nametable on a certain line, in the middle of rendering process. You can't change palettes while image renders, so palette set must be the same for all images.

The sequence of actions is as follows:
* Load available NES colors from JSON or PAL file
* Load images, crop them if need
* Change every pixel of each image, so it's matches most similar color from available colors
* Calculate desired number of palettes to fit every image or at least trying to do this
* Generate attribute table for each image, assign palette index for each tiles set
* Change colors of every tile to match assigned palette index (if need)
* Create set of tiles, trying to them into 256, grouping same tiles into one
* Generate pattern table and nametable for each image

## How to use

```
Usage: nestiler.exe <options>

Available options:
-i<#> --in-<#> <file>[:offset[:height]]         input file number #, optionally cropped vertically
-c    --colors <file>                           JSON or PAL file with the list of available colors
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
-r    --ignore-tiles-range                      option to disable tile ID overflow check
-l    --lossy                                   option to ignore palettes loss, produces distorted image
                                                if there are too many colors
-v<#> --out-preview-<#> <file.png>              output filename for preview of image number #
-t<#> --out-palette-<#> <file>                  output filename for palette number #
-n<#> --out-pattern-table-<#> <file>            output filename for pattern table of image number #
-a<#> --out-name-table-<#> <file>               output filename for nametable of image number #
-u<#> --out-attribute-table-<#> <file>          output filename for attribute table of image number #
-z    --out-tiles-csv <file.csv>                output filename for tiles info in CSV format
-x    --out-palettes-csv <file.csv>             output filename for palettes info in CSV format
-g    --out-colors-table <file.png>             output filename for graphical table of available colors
                                                (from "--colors" option)
-q    --quiet                                   suppress console output
```

### Option -i<#>, --in-<#> \<file\>[:offset[:height]]
Option to specify input images. You need to add image index (any number) after option name, so you can specify multiple images. Index will be used to identify output filenames. Examples:
* nestiler -i0 image1.png -i1 image2.png -i2 image3.png ...
* nestiler --in-0 image1.png --in-1 image2.png --in-2 image3.png ...
 
Also, you can load image partically - split them horizontally, just add offset and height after colon. So if you need to split 256x240 image into two images:
* nestiler -i0 image.png:0:128 -i1 image.png:128:112

It's usefull if you need to show single image on screen but you want to split it into two 256-tiles pattern tables and switch them on specific line in the middle of rendering process.

### Option -c, --colors \<file\>

Option to specify file with available colors and indexes. This file can be in JSON format (see nestiler-colors.json) or binary PAL format (used by emulators).

Examples:
* nestiler -c nestiler-colors.json ...
* nestiler --colors nestiler-colors.json ...

### Option -m, --mode bg|sprites8x8|sprites8x16
Option to specify processing mode: backgrounds, 8x8 sprites or 8x16 sprites. Default is backgrounds mode.
Examples:
* nestiler -m bg ...
* nestiler --mode sprites8x8 ...
