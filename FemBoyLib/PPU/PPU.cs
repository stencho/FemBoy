using System.Diagnostics;
using FemBoy;

namespace FemBoy;

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

public enum PPUMode : byte {
    HBLANK_0 = 0, 
    VBLANK_1 = 1,
    OAM_SEARCH_2 = 2,
    LCD_TRANSFER_3 = 3
}

public class PPU {
    public int dot = 0;
    public int pixels_drawn = 0;
    
    public readonly byte[] frame_buffer = new byte[160 * 144];
    public readonly byte[] frame_buffer_offscreen = new byte[160 * 144];
    
    public bool frame_ready = false;
    
    private GameBoy gameboy;
    private CPU CPU => gameboy.CPU;

    public BGFetcher bg_fetcher;
    public OAMSearch oam_search;
    
    public PPU(GameBoy gameboy) {
        this.gameboy = gameboy;
        bg_fetcher = new(gameboy);
        oam_search = new OAMSearch(gameboy);
        Array.Fill(frame_buffer_offscreen, (byte)0x00);
        Array.Copy(frame_buffer_offscreen, frame_buffer, frame_buffer.Length);
    }

    public byte LY = 0x90;
    public byte LYC = 0x00;
    
    public byte SCX = 0x00;
    public byte SCY = 0x00;

    public byte LCDC = 0x00;
    
    public byte BGP = 0xFC;
    public byte OBP0 = 0xFF;
    public byte OBP1 = 0xFF;
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

    private byte _STAT = 0x80;
    public byte STAT {
        get => _STAT;
        set => _STAT = (byte)((value & 0xF8) | (_STAT & 0x07) | 0x80); //protect lower hardware-controlled bits
    }
    
    private bool old_stat_line = false;
    
    public PPUMode old_mode;
    public PPUMode mode = PPUMode.OAM_SEARCH_2;
    public PPUMode Mode => mode;

    private bool LCD_ON = true;

    private bool lcd_startup_scanline = false;
    
    public void LCDOn() {
        dot = 0;
        LY = 0;

        LCD_ON = true;
        lcd_startup_scanline = true;

        bg_fetcher.Reset();
        oam_search.Reset();
        
        mode = PPUMode.HBLANK_0;
        UpdateHardwareSTATBits(mode);
    }
    public void LCDOff() {
        dot = 0;
        LY = 0;
        
        LCD_ON = false;

        bg_fetcher.Reset();
        oam_search.Reset();
        
        mode = PPUMode.HBLANK_0;
        UpdateHardwareSTATBits(mode);
        
        Array.Fill(frame_buffer_offscreen, (byte)0x00);
        Array.Fill(frame_buffer, (byte)0x00);
    }

    private bool last_line_was_153 = false;
    
    public void Tick() {
        if (!LCD_ON) return;
        old_mode = mode;
        
        
        if (LY == 153) {
            if (dot == 4) {
                dot = 0;
                LY = 0;
                last_line_was_153 = true;
            }
        } else if (dot == 456) {
            dot = 0;
            LY++;

            if (bg_fetcher.window_active) bg_fetcher.IncrementWindowLineCounter();
            
            if (last_line_was_153) {
                LY = 0;
                last_line_was_153 = false;
                lcd_startup_scanline = false;
                mode = PPUMode.OAM_SEARCH_2;
                
            } else if (LY == 144) {
                mode = PPUMode.VBLANK_1;
                bg_fetcher.ResetWindowLineCounter();
                bg_fetcher.window_active = false;
                
                CPU.RequestInterrupt(InterruptMask.VBlank);

                if (!frame_ready) {
                    Array.Copy(frame_buffer_offscreen, frame_buffer, frame_buffer.Length);
                    frame_ready = true;
                }
            }
            else if (LY < 144) {
                mode = PPUMode.OAM_SEARCH_2;
            }
        }
        
        if (mode == PPUMode.OAM_SEARCH_2 || lcd_startup_scanline) {
            if (!lcd_startup_scanline) {
                if (dot < 80) {
                    oam_search.Tick();
                } else {
                    mode = PPUMode.LCD_TRANSFER_3;
                }
            } else {
                if (dot == 80) mode = PPUMode.LCD_TRANSFER_3;
            }
        } else if (mode == PPUMode.LCD_TRANSFER_3) {
            if (!bg_fetcher.TransferComplete) {
                bg_fetcher.Tick();
                
                if (bg_fetcher.TickAndTryPopPixel(out byte color)) {
                    if (pixels_drawn < 160 && LY < 144) {
                        int x = pixels_drawn;
                        int y = LY;

                        if (!BGAndWindowDisplayEnabled) color = 0;

                        byte shade = (byte)((BGP >> (color * 2)) & 0x03);
                        frame_buffer_offscreen[x + y * 160] = shade;

                        DrawSpritePixel(color);
                        pixels_drawn++;
                    }
                }
                
            } else {
                mode = PPUMode.HBLANK_0;
            }
        }
        
        if (mode != old_mode) {
            switch (mode) {
                case PPUMode.OAM_SEARCH_2: oam_search.Start(LY); break;
                case PPUMode.LCD_TRANSFER_3: 
                    pixels_drawn = 0;
                    bg_fetcher.Start();
                    break;
            }
        }
        
        UpdateHardwareSTATBits(mode);
        HandleSTAT();
        
        dot++;
    }

    public void UpdateHardwareSTATBits(PPUMode mode) {
        _STAT &= 0xF8;
        _STAT |= (byte)((byte)mode & 0x03);
        if (LCDEnabled && LY == LYC) _STAT |= 0x04;
        
    }
    
    void HandleSTAT() {
        // Pull STAT line
        bool hblank_int_operand = (mode == PPUMode.HBLANK_0) && ((_STAT & 0x08) != 0);
        bool vblank_int_operand = (mode == PPUMode.VBLANK_1) && ((_STAT & 0x10) != 0);
        bool oam_int_operand    = (mode == PPUMode.OAM_SEARCH_2) && ((_STAT & 0x20) != 0);
        bool lyc_int_operand    = (LY == LYC) && ((_STAT & 0x40) != 0);

        bool current_stat_line = hblank_int_operand || vblank_int_operand || oam_int_operand || lyc_int_operand;

        // Fire LCD interrupt if the STAT line has changed
        if (!old_stat_line && current_stat_line && !lcd_startup_scanline) {
            CPU.RequestInterrupt(InterruptMask.LCD); 
        }

        // Store last STAT line
        old_stat_line = current_stat_line;
    }
    
    bool DrawSpritePixel(byte background_color) {
        if (!OBJEnabled) return false;

        Sprite? final_sprite = null;
        byte final_color = 0;
        int final_x = int.MaxValue;
        int final_oam = int.MaxValue;
        
        for (var index = 0; index < oam_search.visible_sprites.Count; index++) {
            Sprite sprite = oam_search.visible_sprites[index];
            
            int sprite_pixel_x = pixels_drawn - sprite.X;
            int sprite_pixel_y = LY - sprite.Y;
            
            if (sprite_pixel_x < 0 || sprite_pixel_x >= 8) continue;

            if (sprite.FlipX) sprite_pixel_x = 7 - sprite_pixel_x;
            if (sprite.FlipY) sprite_pixel_y = SpriteHeight - 1 - sprite_pixel_y;

            byte tile = sprite.tile;
            if (SpriteHeight == 16) tile &= 0xFE;
                
            ushort tile_address = (ushort)(0x8000 + tile * 16 + sprite_pixel_y * 2);
            
            byte lo = gameboy.ReadMemory(tile_address);
            byte hi = gameboy.ReadMemory((ushort)(tile_address + 1));

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

        if (final_sprite == null) return false;
        if (final_color == 0) return false;
        if (final_sprite.BGPriority && background_color != 0) return false;
        
        byte palette = final_sprite.Palette1 ? OBP1 : OBP0;
        byte shade = (byte)((palette >> (final_color * 2)) & 0x03);

        frame_buffer_offscreen[pixels_drawn + LY * 160] = shade;
        
        return true;
    }

}




