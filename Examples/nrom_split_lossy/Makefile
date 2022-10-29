NESASM?=nesasm
EMU?=fceux64
TILER?=nestiler
SOURCE=main.asm
EXECUTABLE=nrom_split_lossy.nes
HEIGHT1=128

IMAGE?=../../TestImages/Images/me.png

PATTERN_0_BIN=pattern_0.bin
PATTERN_1_BIN=pattern_1.bin
NAME_TABLE_0_BIN=name_table_0.bin
NAME_TABLE_1_BIN=name_table_1.bin
ATTR_TABLE_0_BIN=attr_table_0.bin
ATTR_TABLE_1_BIN=attr_table_1.bin
PALETTE_0_BIN=palette_0.bin
PALETTE_1_BIN=palette_1.bin
PALETTE_2_BIN=palette_2.bin
PALETTE_3_BIN=palette_3.bin
PREVIEW_1=preview1.png
PREVIEW_2=preview2.png

build: $(EXECUTABLE)

all: $(EXECUTABLE)

clean:
	rm -f *.nes *.lst *.nl *.bin preview*.png

run: $(EXECUTABLE)
	$(EMU) $(EXECUTABLE)

$(EXECUTABLE): $(SOURCE) \
$(PATTERN_0_BIN) $(PATTERN_1_BIN) \
$(NAME_TABLE_0_BIN) $(NAME_TABLE_1_BIN) \
$(ATTR_TABLE_0_BIN) $(ATTR_TABLE_1_BIN) \
$(PALETTE_0_BIN) $(PALETTE_1_BIN)
	$(NESASM) $(SOURCE) -o $(EXECUTABLE) --symbols=$(notdir $(EXECUTABLE)) -iWss

$(PATTERN_0_BIN) $(PATTERN_1_BIN) \
$(NAME_TABLE_0_BIN) $(NAME_TABLE_1_BIN) \
$(ATTR_TABLE_0_BIN) $(ATTR_TABLE_1_BIN) \
$(PALETTE_0_BIN) $(PALETTE_1_BIN) \
$(PALETTE_2_BIN) $(PALETTE_3_BIN) \
$(PREVIEW_1) $(PREVIEW_2): $(IMAGE)
	$(TILER) --mode bg --enable-palettes 0,1,2,3 \
	--in-0 $(IMAGE):0:$(HEIGHT1) --in-1 $(IMAGE):$(HEIGHT1) \
	--out-pattern-table-0 $(PATTERN_0_BIN) --out-pattern-table-1 $(PATTERN_1_BIN) \
	--out-name-table-0 $(NAME_TABLE_0_BIN) --out-name-table-1 $(NAME_TABLE_1_BIN) \
	--out-attribute-table-0 $(ATTR_TABLE_0_BIN) --out-attribute-table-1 $(ATTR_TABLE_1_BIN) \
	--out-palette-0 $(PALETTE_0_BIN) --out-palette-1 $(PALETTE_1_BIN) \
	--out-palette-2 $(PALETTE_2_BIN) --out-palette-3 $(PALETTE_3_BIN) \
    --lossy 3 --out-preview-0 $(PREVIEW_1) --out-preview-1 $(PREVIEW_2)
