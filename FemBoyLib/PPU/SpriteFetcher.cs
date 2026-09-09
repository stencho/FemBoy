namespace FemBoy;

public class SpriteFetcher {
    private GameBoy gameboy;
    private PPU PPU => gameboy.PPU;
    private OAMSearch OAM => gameboy.PPU.oam_search;

    public SpritePixel?[] FIFO = new SpritePixel?[8];
    
    public FetchState current_fetch_state = FetchState.Tile;
    
    public SpriteFetcher(GameBoy gameboy) { this.gameboy = gameboy; }
    
    private Sprite? sprite;

    private ushort tile_address;
    private byte sprite_lo;
    private byte sprite_hi;

    private int sprite_start_x;
    private int sprite_pixel_x;
    private int sprite_pixel_y;

    public bool Active = false;
    
    public Sprite? GetSpriteAtX(int x) {
        foreach (Sprite sprite in OAM.visible_sprites) {
            if (x >= sprite.X && x < sprite.X + 8)
                return sprite;
        }
        return null;
    }
    
    public void Start(Sprite sprite) {
        this.sprite = sprite;
        sprite_start_x = PPU.pixels_drawn;
        OAM.visible_sprites.Remove(sprite);
        current_fetch_state = FetchState.Tile;
        Active = true;
    }

    public int CountFIFO() {
        int output = 0;
        for (int i = 0; i < 8; i++) {
            if (!FIFO[i].HasValue) output++;
        }
        return output;
    }
    public void ClearFIFO() {
        for (int i = 0; i < 8; i++) {
            FIFO[i] = null;
        }
    }
    
    public void Tick() {
        if (!Active) return;
        if (PPU.dot % 2 != 0) return;
        switch (current_fetch_state) {
            case FetchState.Tile: FetchTileID(); current_fetch_state = FetchState.Low; break;
            case FetchState.Low: FetchTileAddressLow(); current_fetch_state = FetchState.High; break;
            case FetchState.High: FetchTileAddressHigh(); current_fetch_state = FetchState.Push; break;
            case FetchState.Push: 
                PushTile(); 
                current_fetch_state = FetchState.Tile;
                break;
        }
    }

    void FetchTileID() {
        sprite_pixel_x = sprite_start_x - sprite.X;
        sprite_pixel_y = PPU.LY - sprite.Y;

        if (sprite.FlipX) sprite_pixel_x = 7 - sprite_pixel_x;
        if (sprite.FlipY) sprite_pixel_y = PPU.SpriteHeight - 1 - sprite_pixel_y;

        byte tile = sprite.tile;

        if (PPU.SpriteHeight == 16) {
            tile &= 0xFE;

            if (sprite_pixel_y >= 8) tile++;
        
            sprite_pixel_y &= 7;
        }

        tile_address = (ushort)(0x8000 + tile * 16 + sprite_pixel_y * 2);
    }

    void FetchTileAddressLow() {
        sprite_lo = gameboy.ReadMemory(tile_address);
    }

    void FetchTileAddressHigh() {
        sprite_hi = gameboy.ReadMemory((ushort)(tile_address + 1));
    }

    void MergeIntoFIFO(SpritePixel pixel, int offset) {
        if (offset < 0 || offset >= 8) return;
        
        if (FIFO[offset].HasValue && FIFO[offset].Value.OAMIndex >= pixel.OAMIndex) return;
        if (FIFO[offset].HasValue && FIFO[offset].Value.Color != 0) return;
        
        FIFO[offset] = pixel;
    }
    
    void PushTile() {
        for (int x = 7; x >= 0; x--) {
            int bit = sprite.FlipX ? x : 7 - x;

            byte color = (byte)((((sprite_hi >> bit) & 1) << 1) | ((sprite_lo >> bit) & 1));
            
            var sp = new SpritePixel() { Color = color, OAMIndex = sprite.index, Palette = sprite.Palette1, Priority = sprite.BGPriority };
            
            MergeIntoFIFO(sp, sprite.X - sprite_start_x + x);
        }

        sprite = null;
        Active = false;
    }

    public bool TryPopPixel(out SpritePixel? color) {
        color = FIFO[0];

        for (int i = 0; i < 7; i++)
            FIFO[i] = FIFO[i + 1];

        FIFO[7] = null;

        return color.HasValue;
    }
}