# NesTiler
Tool for converting pictures into NES format: pattern tables, nametables, attribute tables and palettes.

# What does it do
When developing applications and games for NES, to display images, you need to split each image into tiles, combine tiles into nametables, select colors so that they do not go beyond the limits of the NES, and then convert all this into a format understandable for the NES. This tool at least partly helps to automate this process. The application can accept multiple images as input, the main point is to use single set of palettes (and tiles if required) for all of them, so it's possible to switch CHR banks and base nametable on a certain line, in the middle of rendering process. You can't change palettes while image renders, so palette set must be the same for all images.

The sequence of actions is as follows:
* Loading available NES colors from JSON or PAL file
* Load images, crop them if need
* Changing every pixel of each image, so it's matches most similar color from available colors
* Calculating desired number of palettes to fit every image or at least trying to do this
* Generating attribute table for each image, assign palette index for each tiles set
* Changing colors of every tile to match assigned palette index (if need)
* Creating set of tiles, trying to them into 256, grouping same tiles into one
* Generating pattern table and nametable for each image
