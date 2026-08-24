using System.Diagnostics;

namespace RatGBLib;

public enum PPURegisterAddresses : ushort {
    LCDC = 0xFF40, // FF40 - LCD Control
    STAT = 0xFF41, // FF41 - LCD Status
    SCY  = 0xFF42, // FF42 - Scroll Y
    SCX  = 0xFF43, // FF43 - Scroll X
    LY   = 0xFF44, // FF44 - LCD Y coordinate
    LYC  = 0xFF45, // FF45 - LY Compare
    DMA  = 0xFF46, // FF46 - OAM DMA
    BGP  = 0xFF47, // FF47 - BG Palette
    OBP0 = 0xFF48, // FF48 - Object Palette 0
    OBP1 = 0xFF49, // FF49 - Object Palette 1
    WY   = 0xFF4A, // FF4A - Window Y position
    WX   = 0xFF4B  // FF4B - Window X position
}

class Sprite {
    public ushort address;
    
    public int X;
    public int Y;
        
    public byte tile;
    public byte attr;

    public bool BGPriority => (byte)(attr & (1 << 7)) != 0;
        
    public bool FlipY => (byte)(attr & (1 << 6)) != 0;
    public bool FlipX => (byte)(attr & (1 << 5)) != 0;
        
    public bool Palette1 => (byte)(attr & (1 << 4)) != 0;

    public Sprite(GameBoy gameboy, ushort address) {
        this.address = address;
        
        Y = gameboy.ReadOAM(address) - 16;
        X = gameboy.ReadOAM((ushort)(address + 1)) - 8;

        tile = gameboy.ReadOAM((ushort)(address + 2));
        attr = gameboy.ReadOAM((ushort)(address + 3));
    }
}

public class PPU {
    private int cycle_counter = 0;
    private int pixels_drawn = 0;
    
    public readonly byte[] frame_buffer = new byte[160 * 144];
    
    private GameBoy gameboy;

    private List<Sprite> visible_sprites = new();
    
    public PPU(GameBoy gameboy) {
        this.gameboy = gameboy;

        for (var index = 0; index < frame_buffer.Length; index++) {
            var b = frame_buffer[index];
            frame_buffer[index] = 1;
        }
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
    public bool BGEnabled => (LCDC & 0x01) != 0;
    
    public byte STAT {
        get => _STAT;
        set => _STAT = (byte)((value & 0xF8) | (_STAT & 0x07) | 0x80); //protect lower hardware-controlled bits
    }

    [Flags]
    public enum STATMode : byte {
        HBLANK = 0, 
        VBLANK = 1,
        OAM_SEARCH = 2,
        LCD_TRANSFER = 3
    }
    
    
    private void UpdateHardwareSTATBits(STATMode mode) {
        _STAT &= 0xF8;
        _STAT |= (byte)((byte)mode & 0x03);
        if (LY == LYC) _STAT |= 0x04;
    }

    public STATMode Mode => mode;
    private STATMode mode = STATMode.OAM_SEARCH;

    byte DrawBackgroundPixel() {
        int x = pixels_drawn;
        int y = LY;

        int bg_x = (x + SCX) & 0xFF;
        int bg_y = (y + SCY) & 0xFF;

        int tile_x = bg_x >> 3;
        int tile_y = bg_y >> 3;

        int pixel_x = bg_x & 7;
        int pixel_y = bg_y & 7;

        ushort tile_map = (ushort)((BGTileMap) ? 0x9C00 : 0x9800);
        ushort tile_address = (ushort)(tile_map + (tile_y * 32) + tile_x);

        byte tile_id = gameboy.ReadVRAM(tile_address);
        ushort tile_data = (ushort)(0x8000 + (tile_id * 16) + (pixel_y * 2));

        byte lo = gameboy.ReadVRAM(tile_data);
        byte hi = gameboy.ReadVRAM((ushort)(tile_data + 1));

        int bit = 7 - pixel_x;

        byte color = (byte)(((hi >> bit) & 1) << 1 | ((lo >> bit) & 1));
        byte shade = (byte)((BGP >> (color * 2)) & 0x03);
        
        frame_buffer[x + y * 160] = shade;
        return color;
    }

    void OAMLookup() {
        visible_sprites.Clear();
        
        for (int i = 0; i < 40; i++) {
            ushort address = (ushort)(0xFE00 + i * 4);

            int y = gameboy.ReadOAM(address) - 16;

            if (LY >= y && LY < y + SpriteHeight && visible_sprites.Count < 10) {
                visible_sprites.Add(new Sprite(gameboy, address));
            }
        }
    }

    void DrawSpritePixel(byte background_color) {
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
            if (sprite.BGPriority && background_color != 0) continue;

            byte palette = sprite.Palette1 ? OBP1 : OBP0;
            byte shade = (byte)((palette >> (color * 2)) & 0x03);

            frame_buffer[pixels_drawn + LY * 160] = shade;
            break;
        }
    }

    private bool old_stat_line = false;
    public void Execute() {
        if (!LCDEnabled) return;
        
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
                if (pixels_drawn < 160) {
                    byte background_color = DrawBackgroundPixel();
                    DrawSpritePixel(background_color);
                    pixels_drawn++;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        if (cycle_counter >= GameBoy.DOTS_PER_SCANLINE) {
            cycle_counter -= GameBoy.DOTS_PER_SCANLINE;

            LY++;
            LYC_interrupt_fired_this_line = false;
            pixels_drawn = 0;
            
            if (LY == 144) gameboy.RequestInterrupt(CPU.InterruptMask.VBlank); 
            if (LY >= GameBoy.SCANLINES_PER_FRAME) LY = 0;
        }
        
        if (LY >= 144) mode = STATMode.VBLANK;
        else if (cycle_counter < 80) mode = STATMode.OAM_SEARCH;
        else if (cycle_counter < 252) mode = STATMode.LCD_TRANSFER;
        else mode = STATMode.HBLANK;
        
        bool LYC_match = (LY == LYC);
        
        bool hblank_int_operand = (mode == STATMode.HBLANK) && ((_STAT & 0x08) != 0);
        bool vblank_int_operand = (mode == STATMode.VBLANK) && ((_STAT & 0x10) != 0);
        bool oam_int_operand    = (mode == STATMode.OAM_SEARCH) && ((_STAT & 0x20) != 0);
        bool lyc_int_operand    = (LY == LYC) && ((_STAT & 0x40) != 0);

        bool current_stat_line = hblank_int_operand || vblank_int_operand || oam_int_operand || lyc_int_operand;

        if (!old_stat_line && current_stat_line) {
            gameboy.RequestInterrupt(CPU.InterruptMask.LCD); 
        }

        old_stat_line = current_stat_line;

        UpdateHardwareSTATBits(mode);
    }
}




