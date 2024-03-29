  ; INES header stuff
  .inesprg 1   ; 1 bank of PRG
  .ineschr 2   ; 2 banks of CHR data
  .inesmap 4   ; we use mapper 4 (MMC3)

  .rsset $0000 ; variables
COPY_SOURCE_ADDR    .rs 2
COPY_DEST_ADDR      .rs 2
CHR_BANK            .rs 1
FRAME               .rs 1
  
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

  jsr waitblank_simple

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

  ; vertical mirroring
  lda #0
  sta $A000

load_palette:
  lda #LOW(palette)
  sta <COPY_SOURCE_ADDR
  lda #HIGH(palette)
  sta <COPY_SOURCE_ADDR+1
  lda #$3F
  sta PPUADDR
  lda #$00
  sta PPUADDR
  ldy #$00
  ldx #16
.loop:
  lda [COPY_SOURCE_ADDR], y
  sta PPUDATA
  iny
  dex
  bne .loop

load_nametable:
  lda #LOW(nametable_0)
  sta <COPY_SOURCE_ADDR
  lda #HIGH(nametable_0)
  sta <COPY_SOURCE_ADDR+1
  lda #$20
  sta PPUADDR
  lda #$00
  sta PPUADDR
  ldy #$00
  ldx #$00
.loop:
  lda [COPY_SOURCE_ADDR], y
  sta PPUDATA
  iny
  bne .loop
  inc COPY_SOURCE_ADDR+1
  inx
  cpx #8
  bne .loop
  
  sta $E000      ; disable MMC3 IRQ
  lda #%10001000 ; enable NMI
  sta PPUCTRL
  jsr waitblank
  lda #%00001010 ; enable BG
  sta PPUMASK
  cli            ; enable interrupts
main_loop:
  jsr waitblank
  jmp main_loop

  ; VBlank wait subroutine
waitblank:
  bit PPUSTATUS
  lda #0
  sta PPUSCROLL
  sta PPUSCROLL
  lda FRAME
.loop:
  cmp FRAME
  beq .loop
  rts

  ; VBlank wait subroutine
waitblank_simple:
  bit PPUSTATUS
  lda #0
  sta PPUSCROLL
  sta PPUSCROLL
.loop:
  lda PPUSTATUS ; load A with value at location PPUSTATUS
  bpl .loop     ; if bit 7 is not set (not VBlank) keep checking
  rts

NMI:
  php
  pha

  ; signal VBlank
  inc FRAME

  lda FRAME
  and #$40

  beq .first
  lda #2
  jsr chr_bank_switch_left
  lda #%10001001
  sta PPUCTRL
  jmp .end

.first:
  ; select bank 0
  lda #0
  jsr chr_bank_switch_left
  lda #%10001000
  sta PPUCTRL
.end:

  ; arm IRQ
  sta $E000 ; disable scanline IRQ (and ack)
  lda #127
  sta $C000 ; scanline counter
  sta $C001 ; reload counter
  sta $E001 ; enable scanline IRQ

  pla
  plp
  rti

IRQ:
  php
  pha

  ; disable scanline IRQ (and ack)
  sta $E000

  ; delay
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop
  nop

  lda FRAME
  and #$40

  beq .first
  lda #3
  jsr chr_bank_switch_left
  lda #%10001001
  sta PPUCTRL
  jmp .end

.first:
  ; select bank 0
  lda #1
  jsr chr_bank_switch_left
  lda #%10001000
  sta PPUCTRL
.end:

  pla
  plp
  rti

chr_bank_switch_left:
  pha
  asl A
  asl A
  ldx #0
  stx $8000
  sta $8001
  inx
  ora #2
  stx $8000
  sta $8001
  pla
  rts

chr_bank_switch_right:
  pha
  asl A
  asl A
  ldx #2
  stx $8000
  sta $8001
  inx
  ora #1
  stx $8000
  sta $8001
  inx
  and #%11111110
  ora #2
  stx $8000
  sta $8001
  inx
  ora #1
  stx $8000
  sta $8001
  pla
  rts

nametable_0:
  .incbin "name_table_0.bin"
nametable_1:
  .incbin "name_table_1.bin"
attr_table_0:
  .incbin "attr_table_0.bin"
attr_table_1:
  .incbin "attr_table_1.bin"

nametable_2:
  .incbin "name_table_2.bin"
nametable_3:
  .incbin "name_table_3.bin"
attr_table_2:
  .incbin "attr_table_2.bin"
attr_table_3:
  .incbin "attr_table_3.bin"

palette:
  .incbin "palette_0.bin"
  .incbin "palette_1.bin"
  .incbin "palette_2.bin"
  .incbin "palette_3.bin"

  .bank 2
  .org $0000
  .incbin "pattern_0.bin"
  .org $1000
  .incbin "pattern_1.bin"
  .bank 3
  .org $0000
  .incbin "pattern_2.bin"
  .org $1000
  .incbin "pattern_3.bin"
