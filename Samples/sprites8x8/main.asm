  ; INES header stuff
  .inesprg 1   ; 1 bank of PRG
  .ineschr 1   ; 1 bank of CHR data
  .inesmir 0   ; mirroring
  .inesmap 0   ; we use mapper 0

  .rsset $0000 ; variables
COPY_SOURCE_ADDR    .rs 2
COPY_DEST_ADDR      .rs 2
  .rsset $0200
SPRITES             .rs 256
  
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
  ; load single palette for sprites
  lda #$3F
  sta PPUADDR
  lda #$10
  sta PPUADDR
  lda palette
  sta PPUDATA
  lda palette+1
  sta PPUDATA
  lda palette+2
  sta PPUDATA
  lda palette+3
  sta PPUDATA

reset_sprites:
  lda #$FF
  ldx 0
.loop:
  sta SPRITES, x
  inx
  bne .loop

set_sprites:
  ldx 0
.loop:
  lda sprites_init_data, x
  sta SPRITES, x
  inx
  cpx #(4 * 12)
  bne .loop  

  ; enable PPU
  jsr waitblank
  lda #%00000000
  sta PPUCTRL
  lda #%00010100
  sta PPUMASK

main_loop:
  jsr waitblank

  ldx 0
.move_sprites_loop:
  inc SPRITES, x
  inx
  inx
  inx
  inx
  cpx #(4 * 12 + 3)
  bne .move_sprites_loop

  lda #0
  sta OAMADDR
  lda #HIGH(SPRITES)
  sta OAMDMA
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
sprites_init_data:
  .db 0, 0, 0, 10
  .db 20, 1, 0, 30
  .db 40, 2, 0, 50
  .db 60, 3, 0, 70
  .db 80, 4, 0, 90
  .db 100, 5, 0, 110
  .db 120, 6, 0, 130
  .db 140, 7, 0, 150
  .db 160, 8, 0, 170
  .db 180, 9, 0, 190
  .db 200, 10, 0, 210
  .db 220, 11, 0, 230

  .bank 2
  .org $0000
  .incbin "pattern_0.bin"
