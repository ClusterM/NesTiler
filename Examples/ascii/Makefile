NESASM?=nesasm
EMU?=fceux64
TILER?=nestiler
SOURCE=main.asm
EXECUTABLE=ascii.nes

IMAGE?=../../TestImages/Images/ascii.png

PATTERN_0_BIN=pattern_0.bin
PALETTE_0_BIN=palette_0.bin

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
	$(NESASM) $(SOURCE) -o $(EXECUTABLE) --symbols=$(notdir $(EXECUTABLE)) -iWss

$(PATTERN_0_BIN) \
$(NAME_TABLE_0_BIN) \
$(ATTR_TABLE_0_BIN) \
$(PALETTE_0_BIN) $(PALETTE_1_BIN) \
$(PALETTE_2_BIN) $(PALETTE_3_BIN): $(IMAGE)
	$(TILER) --mode bg --enable-palettes 0 \
	--in-0 $(IMAGE) \
	--out-pattern-table-0 $(PATTERN_0_BIN) \
	--out-palette-0 $(PALETTE_0_BIN) \
	--no-group-tiles-0
