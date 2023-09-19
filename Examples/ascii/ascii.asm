  ; INES header stuff
  .inesprg 1   ; 1 bank of PRG
  .ineschr 2   ; 2 banks of CHR data
  .inesmap 4   ; we use mapper 4 (MMC3)

  .rsset $0000 ; variables
COPY_SOURCE_ADDR    .rs 2
COPY_DEST_ADDR      .rs 2

  .org $DFFA  ; start at $FFFA
  .dw NMI
  .dw Start
  .dw IRQ

  .org $C000  ; code starts at $E000
  
Start:
  ;sei ; no interrupts

  ; disable PPU
  lda #%00000000
  sta PPUCTRL
  sta PPUMASK
  ; warm-up
  jsr waitblank
  jsr waitblank  

  ; clean memory
  lda #$00
  sta <COPY_SOURCE_ADDR
  lda #$02
  sta <COPY_SOURCE_ADDR + 1
  lda #$00
  ldy #$00
  ldx #$06
.loop:
  sta [COPY_SOURCE_ADDR], y
  iny
  bne .loop
  inc <COPY_SOURCE_ADDR+1
  dex
  bne .loop

  ; use IRQ at $DFFE
  lda #$C0
  sta IRQ_ACTION
  ; return to BIOS on reset
  lda #$00
  sta RESET_FLAG
  sta RESET_TYPE

  ; loading palette
load_palette:
  jsr waitblank
  lda #$3F
  sta PPUADDR
  lda #$00
  sta PPUADDR
  ldx #$00
.loop:
  lda palette, x
  sta PPUDATA
  inx
  cpx #4
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

load_text:
  lda PPUSTATUS
  lda #$20
  sta PPUADDR
  lda #$41
  sta PPUADDR
  lda #LOW(hello_world)
  sta COPY_SOURCE_ADDR
  lda #HIGH(hello_world)
  sta COPY_SOURCE_ADDR+1
  ldy #0
.loop:
  lda [COPY_SOURCE_ADDR], y
  beq .end  
  sta PPUDATA
  iny
  bne .loop
.end:

  ; enable PPU
  jsr waitblank
  ; show background
  lda #%00001010
  sta PPUMASK

  ; main loop        
infin:
  jsr waitblank
  jsr ReadOrDownPads
  jmp infin

waitblank:
  jsr fix_scroll
  pha
  bit PPUSTATUS
.loop:
  bit PPUSTATUS  ; load A with value at location PPUSTATUS
  bpl .loop  ; if bit 7 is not set (not VBlank) keep checking
  pla
  rts

fix_scroll:
  ; fix scrolling
  pha
  bit PPUSTATUS
  lda #0
  sta PPUSCROLL
  sta PPUSCROLL
  pla
  rts

IRQ:  
  rti

NMI:
  ; 
  lda #$35
  sta $102
  lda #$AC
  sta $103
  jmp [$FFFC]

nametable:
;  .incbin "bg_name_table.bin"
;  .org nametable + $3C0
;  .incbin "bg_attr_table.bin"
palette: 
  .incbin "palette0.bin"
;  .incbin "palette1.bin"

hello_world:
  .db "HELLO WORLD!", 0

  .org $0000
  .incbin "pattern_2.bin"
