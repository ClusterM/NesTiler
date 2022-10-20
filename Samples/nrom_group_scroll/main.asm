; INES header stuff
  .inesprg 1   ; 1 bank of PRG
  .ineschr 1   ; 1 bank of CHR data
  .inesmir 1   ; mirroring
  .inesmap 0   ; we use mapper 0

DELAY .equ 2

  .rsset $0000 ; variables
COPY_SOURCE_ADDR    .rs 2
COPY_DEST_ADDR      .rs 2
SCROLL_X            .rs 1
PAUSE               .rs 1
DIR                 .rs 1
  
  .bank 1
  .org $FFFA   ; reset vectors
  .dw NMI
  .dw Start
  .dw IRQ

  .bank 0
  .org $C000 ; code starts here
Start:
  ; disable interrupts
  sei
  ; reset stack
  ldx #$ff
  txs

  ; disable PPU
  lda #%00000000
  sta PPUCTRL
  lda #%00000000
  sta PPUMASK

  jsr waitblank

  ; reset sound
  lda #0
  sta $4000
  sta $4001
  sta $4002
  sta $4003
  sta $4004
  sta $4005
  sta $4006
  sta $4007
  sta $4009
  sta $400A
  sta $400C
  sta $400D
  sta $400E
  sta $400F
  sta $4010
  sta $4011
  sta $4012
  sta $4013
  lda #$0F
  sta $4015
  lda #$40
  sta $4017
  lda #0

load_palette:
  lda #LOW(palette)
  sta <COPY_SOURCE_ADDR
  lda #HIGH(palette)
  sta <COPY_SOURCE_ADDR+1
  lda #$3F
  sta $2006
  lda #$00
  sta $2006
  ldy #$00
  ldx #16
.loop:
  lda [COPY_SOURCE_ADDR], y
  sta $2007
  iny
  dex
  bne .loop

load_nametable:
  lda #LOW(nametable_0)
  sta <COPY_SOURCE_ADDR
  lda #HIGH(nametable_0)
  sta <COPY_SOURCE_ADDR+1
  lda #$20
  sta $2006
  lda #$00
  sta $2006
  ldy #$00
  ldx #$00
.loop:
  lda [COPY_SOURCE_ADDR], y
  sta $2007
  iny
  bne .loop
  inc COPY_SOURCE_ADDR+1
  inx
  cpx #8
  bne .loop

  ; enable PPU
  jsr waitblank
  lda #%00001010
  sta PPUMASK
  lda #%00000000
  sta PPUCTRL

  ; reset variables
  lda #0
  sta SCROLL_X
  sta DIR

main_loop:
  jsr waitblank
  jmp main_loop

  ; VBlank wait subroutine
waitblank:
  bit PPUSTATUS

  ldx SCROLL_X
  stx PPUSCROLL

  inc PAUSE
  lda PAUSE
  cmp #DELAY
  bne .scroll
  lda #0
  sta PAUSE

  lda DIR
  bne .left
  inx
  bne .scroll
  lda #1
  sta DIR
  dex
  jmp .scroll
.no_turn_left:
  jmp .scroll
.left:
  dex
  bne .scroll
  lda #0
  sta DIR
  
.scroll:
  stx SCROLL_X
  lda #0
  sta PPUSCROLL
.loop:
  lda PPUSTATUS ; load A with value at location PPUSTATUS
  bpl .loop ; if bit 7 is not set (not VBlank) keep checking
  rts

NMI:
  rti

IRQ:
  rti

nametable_0:
  .incbin "name_table_0.bin"
attr_table_0:
  .incbin "attr_table_0.bin"
nametable_1:
  .incbin "name_table_1.bin"
attr_table_1:
  .incbin "attr_table_1.bin"

palette:
  .incbin "palette_0.bin"
  .incbin "palette_1.bin"
  .incbin "palette_2.bin"
  .incbin "palette_3.bin"

  .bank 2
  .org $0000
  .incbin "pattern_0.bin"
