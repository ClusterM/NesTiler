NESASM=nesasm
EMU=fceux64
TILER=nestiler
SOURCE=main.asm
EXECUTABLE=mmc3_split4.nes

IMAGE?=../../TestImages/Images/myatej.gif

PATTERN_0_BIN=pattern_0.bin
PATTERN_1_BIN=pattern_1.bin
PATTERN_2_BIN=pattern_2.bin
PATTERN_3_BIN=pattern_3.bin
NAME_TABLE_0_BIN=name_table_0.bin
NAME_TABLE_1_BIN=name_table_1.bin
NAME_TABLE_2_BIN=name_table_2.bin
NAME_TABLE_3_BIN=name_table_3.bin
ATTR_TABLE_0_BIN=attr_table_0.bin
ATTR_TABLE_1_BIN=attr_table_1.bin
ATTR_TABLE_2_BIN=attr_table_2.bin
ATTR_TABLE_3_BIN=attr_table_3.bin
PALETTE_0_BIN=palette_0.bin
PALETTE_1_BIN=palette_1.bin
PALETTE_2_BIN=palette_2.bin
PALETTE_3_BIN=palette_3.bin

build: $(EXECUTABLE)

all: $(EXECUTABLE)

clean:
	rm -f *.nes *.lst *.nl *.bin

run: $(EXECUTABLE)
	$(EMU) $(EXECUTABLE)

$(EXECUTABLE): $(SOURCE) \
$(PATTERN_0_BIN) $(PATTERN_1_BIN) \
$(NAME_TABLE_0_BIN) $(NAME_TABLE_1_BIN) \
$(ATTR_TABLE_0_BIN) $(ATTR_TABLE_1_BIN) \
$(PALETTE_0_BIN) $(PALETTE_1_BIN)
	rm -f $(EXECUTABLE)
	$(NESASM) $(SOURCE) -o $(EXECUTABLE) --symbols=$(notdir $(EXECUTABLE)) -iWss

$(PATTERN_0_BIN) $(PATTERN_1_BIN) $(PATTERN_2_BIN) $(PATTERN_3_BIN) \
$(NAME_TABLE_0_BIN) $(NAME_TABLE_1_BIN) $(NAME_TABLE_2_BIN) $(NAME_TABLE_3_BIN) \
$(ATTR_TABLE_0_BIN) $(ATTR_TABLE_1_BIN) $(ATTR_TABLE_2_BIN) $(ATTR_TABLE_3_BIN) \
$(PALETTE_0_BIN) $(PALETTE_1_BIN) $(PALETTE_2_BIN) $(PALETTE_3_BIN): $(IMAGE)
	$(TILER) --mode bg --enable-palettes 0,1,2,3 \
	--in-0 $(IMAGE):0:64 --in-1 $(IMAGE):64:64 --in-2 $(IMAGE):128:64 --in-3 $(IMAGE):192:48 \
	--out-pattern-table-0 $(PATTERN_0_BIN) --out-pattern-table-1 $(PATTERN_1_BIN) --out-pattern-table-2 $(PATTERN_2_BIN) --out-pattern-table-3 $(PATTERN_3_BIN) \
	--out-name-table-0 $(NAME_TABLE_0_BIN) --out-name-table-1 $(NAME_TABLE_1_BIN) --out-name-table-2 $(NAME_TABLE_2_BIN) --out-name-table-3 $(NAME_TABLE_3_BIN) \
	--out-attribute-table-0 $(ATTR_TABLE_0_BIN) --out-attribute-table-1 $(ATTR_TABLE_1_BIN) --out-attribute-table-2 $(ATTR_TABLE_2_BIN) --out-attribute-table-3 $(ATTR_TABLE_3_BIN) \
	--out-palette-0 $(PALETTE_0_BIN) --out-palette-1 $(PALETTE_1_BIN) --out-palette-2 $(PALETTE_2_BIN) --out-palette-3 $(PALETTE_3_BIN)
