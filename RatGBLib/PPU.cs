using System.Diagnostics;

namespace RatGBLib;

public static class PPURegisterAddresses {
    public const ushort LCDC = 0xFF40; // FF40 - LCD Control
    public const ushort STAT = 0xFF41; // FF41 - LCD Status
    public const ushort SCY  = 0xFF42; // FF42 - Scroll Y
    public const ushort SCX  = 0xFF43; // FF43 - Scroll X
    public const ushort LY   = 0xFF44; // FF44 - LCD Y coordinate
    public const ushort LYC  = 0xFF45; // FF45 - LY Compare
    public const ushort DMA  = 0xFF46; // FF46 - OAM DMA
    public const ushort BGP  = 0xFF47; // FF47 - BG Palette
    public const ushort OBP0 = 0xFF48; // FF48 - Object Palette 0
    public const ushort OBP1 = 0xFF49; // FF49 - Object Palette 1
    public const ushort WY   = 0xFF4A; // FF4A - Window Y position
    public const ushort WX   = 0xFF4B; // FF4B - Window X position
}

public enum STATMode : byte {
    HBLANK = 0, 
    VBLANK = 1,
    OAM_SEARCH = 2,
    LCD_TRANSFER = 3
}

public class PPU {
    private int cycle_counter = 0;
    public int pixels_drawn = 0;
    
    public readonly byte[] frame_buffer = new byte[160 * 144];
    public readonly byte[] frame_buffer_offscreen = new byte[160 * 144];
    
    public bool frame_ready = false;
    
    private GameBoy gameboy;

    private List<Sprite> visible_sprites = new();
    private BGFetcher bg_fetcher;
    
    public PPU(GameBoy gameboy) {
        this.gameboy = gameboy;
        bg_fetcher = new(gameboy, this);
        Array.Fill(frame_buffer_offscreen, (byte)0x01);
        Array.Copy(frame_buffer_offscreen, frame_buffer, frame_buffer.Length);
    }

    public byte LY = 0x00;
    public byte LYC = 0x00;

    private bool LYC_interrupt_fired_this_line = false;
    
    private byte _STAT = 0x80;

    public byte SCX = 0x00;
    public byte SCY = 0x00;

    public byte LCDC = 0x00;
    
    public byte BGP = 0x00;
    public byte OBP0 = 0x00;
    public byte OBP1 = 0x00;
    public byte WY = 0x00;
    public byte WX = 0x00;
    
    public bool LCDEnabled => (LCDC & 0x80) != 0;
    public bool WindowTileMap => (LCDC & 0x40) != 0;
    public bool WindowEnabled => (LCDC & 0x20) != 0;
    public bool TileDataSelect => (LCDC & 0x10) != 0;
    public bool BGTileMap => (LCDC & 0x08) != 0;
    public int SpriteHeight => (LCDC & 0x04) != 0 ? 16 : 8;
    public bool OBJEnabled => (LCDC & 0x02) != 0;
    public bool BGAndWindowDisplayEnabled => (LCDC & 0x01) != 0;

    public byte STAT {
        get => _STAT;
        set => _STAT = (byte)((value & 0xF8) | (_STAT & 0x07) | 0x80); //protect lower hardware-controlled bits
    }
    
    private void UpdateHardwareSTATBits(STATMode mode) {
        _STAT &= 0xF8;
        _STAT |= (byte)((byte)mode & 0x03);
        if (LY == LYC) _STAT |= 0x04;
    }
    
    private bool old_stat_line = false;
    
    public STATMode Mode => mode;
    private STATMode mode = STATMode.OAM_SEARCH;
    
    public void Execute() {
        if (!LCDEnabled) {
            
            LY = 0;
            return;
        }
        
        cycle_counter++;

        switch (mode) {
            case STATMode.HBLANK: break;
            case STATMode.VBLANK: break;
            case STATMode.OAM_SEARCH:
                // Do sprite lookups
                OAMLookup();
                break;
            case STATMode.LCD_TRANSFER:
                // Background lookup/draw single pixel
                if (!bg_fetcher.window_active 
                  && WindowEnabled 
                  && LY >= WY 
                  && pixels_drawn >= (WX - 7)) {
                    bg_fetcher.StartWindow();
                }
                
                bg_fetcher.Tick();
                
                if (bg_fetcher.TryPopPixel(out byte bg_color) && pixels_drawn < 160) {
                    int x = pixels_drawn;
                    int y = LY;

                    if (!BGAndWindowDisplayEnabled) bg_color = 0;
                    
                    byte shade = (byte)((BGP >> (bg_color * 2)) & 0x03);
                    frame_buffer_offscreen[x + y * 160] = shade;
                    
                    DrawSpritePixel(bg_color);
                    pixels_drawn++;
                } 
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        if (cycle_counter >= GameBoy.DOTS_PER_SCANLINE) {
            cycle_counter -= GameBoy.DOTS_PER_SCANLINE;
            
            if (bg_fetcher.window_active) bg_fetcher.IncrementLineCounter();
            
            LY++;
            LYC_interrupt_fired_this_line = false;
            
            if (LY == 144) {
                bg_fetcher.ResetLineCounter();
                bg_fetcher.window_active = false;
                gameboy.RequestInterrupt(CPU.InterruptMask.VBlank);
                if (!frame_ready) {
                    Array.Copy(frame_buffer_offscreen, frame_buffer, frame_buffer.Length);
                    frame_ready = true;
                }
            } 
            
            if (LY >= GameBoy.SCANLINES_PER_FRAME) LY = 0;
        }

        STATMode old_mode = mode;
        
        if (LY >= 144) mode = STATMode.VBLANK;
        else if (cycle_counter < 80) mode = STATMode.OAM_SEARCH;
        else if (cycle_counter < 252 || pixels_drawn < 160) mode = STATMode.LCD_TRANSFER;
        else mode = STATMode.HBLANK;

        
        bool hblank_int_operand = (mode == STATMode.HBLANK) && ((_STAT & 0x08) != 0);
        bool vblank_int_operand = (mode == STATMode.VBLANK) && ((_STAT & 0x10) != 0);
        bool oam_int_operand    = (mode == STATMode.OAM_SEARCH) && ((_STAT & 0x20) != 0);
        bool lyc_int_operand    = (LY == LYC) && ((_STAT & 0x40) != 0);

        bool current_stat_line = hblank_int_operand || vblank_int_operand || oam_int_operand || lyc_int_operand;

        if (!old_stat_line && current_stat_line) {
            gameboy.RequestInterrupt(CPU.InterruptMask.LCD); 
        }

        old_stat_line = current_stat_line;

        if (old_mode != STATMode.LCD_TRANSFER && mode == STATMode.LCD_TRANSFER) {
            bg_fetcher.Start();
            pixels_drawn = 0;
        }
        UpdateHardwareSTATBits(mode);
    }
    
    void OAMLookup() {
        visible_sprites.Clear();
        
        for (int i = 0; i < 40; i++) {
            ushort address = (ushort)(0xFE00 + i * 4);

            int y = gameboy.ReadOAM(address) - 16;

            if (LY >= y && LY < y + SpriteHeight && visible_sprites.Count < 10) {
                visible_sprites.Add(new Sprite(gameboy, address, i));
            }
        }
    }

    void DrawSpritePixel(byte background_color) {
        if (!OBJEnabled) return;

        Sprite? final_sprite = null;
        byte final_color = 0;
        int final_x = int.MaxValue;
        int final_oam = int.MaxValue;
        
        for (var index = 0; index < visible_sprites.Count; index++) {
            Sprite sprite = visible_sprites[index];
            
            int sprite_pixel_x = pixels_drawn - sprite.X;
            int sprite_pixel_y = LY - sprite.Y;
            
            if (sprite_pixel_x < 0 || sprite_pixel_x >= 8) continue;

            if (sprite.FlipX) sprite_pixel_x = 7 - sprite_pixel_x;
            if (sprite.FlipY) sprite_pixel_y = SpriteHeight - 1 - sprite_pixel_y;

            byte tile = sprite.tile;
            if (SpriteHeight == 16) tile &= 0xFE;
                
            ushort tile_address = (ushort)(0x8000 + tile * 16 + sprite_pixel_y * 2);
            
            byte lo = gameboy.ReadVRAM(tile_address);
            byte hi = gameboy.ReadVRAM((ushort)(tile_address + 1));

            int bit = 7 - sprite_pixel_x;
            byte color = (byte)((((hi >> bit) & 1) << 1) | ((lo >> bit) & 1));

            if (color == 0) continue;
            
            if (final_sprite == null 
                || sprite.X < final_x 
                || (sprite.X == final_sprite.X && index < final_oam)) {
                final_sprite = sprite;
                final_x = sprite.X;
                final_oam = index;
                final_color = color;
            }
        }

        if (final_sprite == null) return;
        if (final_color == 0) return;
        if (final_sprite.BGPriority && background_color != 0) return;
        
        byte palette = final_sprite.Palette1 ? OBP1 : OBP0;
        byte shade = (byte)((palette >> (final_color * 2)) & 0x03);

        frame_buffer_offscreen[pixels_drawn + LY * 160] = shade;
        
    }

}




