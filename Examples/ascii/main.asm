  ; INES header stuff
  .inesprg 1   ; 1 bank of PRG
  .ineschr 1   ; 1 bank of CHR data
  .inesmir 0   ; mirroring
  .inesmap 0   ; we use mapper 0

  .rsset $0000 ; variables
COPY_SOURCE_ADDR    .rs 2
COPY_DEST_ADDR      .rs 2
  
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
  sta PPUADDR
  lda #$00
  sta PPUADDR
  ldy #$00
  ldx #4
.loop:
  lda [COPY_SOURCE_ADDR], y
  sta PPUDATA
  iny
  dex
  bne .loop

  ; loading blank nametable
clear_nametable:
  lda PPUSTATUS
  lda #$20
  sta PPUADDR
  lda #$00
  sta PPUADDR
  ldy #0
  ldx #0
  lda #$20
.loop:
  cpy #$C0
  bne .noat
  cpx #$03
  bne .noat
  lda #$00
.noat:
  sta PPUDATA
  iny
  bne .loop
  inx
  cpx #$04
  bne .loop
.end:

  ; write HELLO WORLD
load_text:
  lda PPUSTATUS
  lda #$20
  sta PPUADDR
  lda #$41
  sta PPUADDR
  lda #LOW(.hello_world)
  sta COPY_SOURCE_ADDR
  lda #HIGH(.hello_world)
  sta COPY_SOURCE_ADDR+1
  ldy #0
.loop:
  lda [COPY_SOURCE_ADDR], y
  beq .end  
  sta PPUDATA
  iny
  jmp .loop
.hello_world:
  .db "HELLO WORLD!", 0
.end:

  ; enable PPU
  jsr waitblank
  lda #%00001010
  sta PPUMASK
  lda #%00000000
  sta PPUCTRL
main_loop:
  jsr waitblank
  jmp main_loop

  ; VBlank wait subroutine
waitblank:
  bit PPUSTATUS
  lda #0
  sta PPUSCROLL
  sta PPUSCROLL
.loop:
  lda PPUSTATUS ; load A with value at location PPUSTATUS
  bpl .loop     ; if bit 7 is not set (not VBlank) keep checking
  rts

NMI:
  rti

IRQ:
  rti

palette:
  .incbin "palette_0.bin"

  .bank 2
  .org $0200
  .incbin "pattern_0.bin"
