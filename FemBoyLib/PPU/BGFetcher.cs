namespace FemBoy;

public enum FetchState { Tile, Low, High, Push }

public class BGFetcher {
    public FetchState current_fetch_state = FetchState.Tile;
    public Queue<byte> FIFO =  new Queue<byte>();
    
    private GameBoy gameboy;
    private PPU PPU => gameboy.PPU;
    
    private int dot_counter = 0;

    private byte tile_id;
    private byte tile_lo;
    private byte tile_hi;

    private int tile_x;
    private int tile_y;
    
    private int pixel_y;

    private int discard_pixels = 0;
    private int stall = 0;
    public bool window_active;
    
    public BGFetcher(GameBoy gameboy) {
        this.gameboy = gameboy;
    }

    
    private byte window_line_counter;
    public byte WindowLineCounter => window_line_counter;
    
    public void IncrementWindowLineCounter() => window_line_counter++;
    public void ResetWindowLineCounter() => window_line_counter = 0;

    public int pixels_popped = 0;
    public bool TransferComplete => pixels_popped >= 160;
    
    public void Start() {
        FIFO.Clear();
        dot_counter = 0;

        window_active = false;
        
        int bg_y = (PPU.SCY + PPU.LY) & 0xFF;

        tile_x = PPU.SCX >> 3;
        tile_y = bg_y >> 3;

        pixel_y = bg_y & 7;
        
        pixels_popped = 0;
        stall = 0;
        discard_pixels = PPU.SCX % 8;
        
        current_fetch_state = FetchState.Tile;
    }

    public void Reset() {
        ResetWindowLineCounter();
        window_active = false;
        dot_counter = 0;
        pixels_popped = 0;
        stall = 0;
        FIFO.Clear();
    }
    
    public void StartWindow() {
        FIFO.Clear();
        window_active = true;

        //int window_y = ppu.LY - ppu.WY;
        int window_y = window_line_counter;
        dot_counter = 0;
        
        tile_x = 0;
        tile_y = window_y >> 3;
        
        pixel_y = window_y & 7;
        
        discard_pixels = 0;
        stall = 6;
        
        current_fetch_state = FetchState.Tile;
    }

    public void Tick() {
        if (stall > 0) {
            stall--;
            return;
        }
        
        if (PPU.dot % 2 != 0) return;
        
        switch (current_fetch_state) {
            case FetchState.Tile: FetchTileID(); current_fetch_state = FetchState.Low; break;
            case FetchState.Low: FetchTileAddressLow(); current_fetch_state = FetchState.High; break;
            case FetchState.High: FetchTileAddressHigh(); current_fetch_state = FetchState.Push; break;
            case FetchState.Push: 
                tile_x = (tile_x + 1) & 31;
                PushTile(); 
                current_fetch_state = FetchState.Tile;
                break;
        }
    }

    void FetchTileID() {
        ushort tile_map;
        if (window_active) {
            tile_map = PPU.WindowTileMap ? (ushort)0x9C00 : (ushort)0x9800;
        } else {
            tile_map = PPU.BGTileMap ? (ushort)0x9C00 : (ushort)0x9800;
        }
        
        ushort addr = (ushort)(tile_map + (tile_y * 32) + tile_x);
        tile_id = gameboy.ReadMemory(addr);
    }
    
    private ushort GetTileDataAddress() {
        if (gameboy.PPU.TileDataSelect) return (ushort)(0x8000 + (byte)tile_id * 16 + pixel_y * 2);
        return (ushort)(0x9000 + (sbyte)tile_id * 16 + pixel_y * 2);
    }
    
    void FetchTileAddressLow() {
        ushort addr = GetTileDataAddress();
        tile_lo = gameboy.ReadMemory(addr);
    }

    void FetchTileAddressHigh() {
        ushort addr = (ushort)(GetTileDataAddress() + 1);
        tile_hi = gameboy.ReadMemory(addr);
    }

    void PushTile() {
        for (int i = 7; i >= 0; i--) {
            byte color = (byte)((((tile_hi >> i) & 1) << 1) | ((tile_lo >> i) & 1));
            FIFO.Enqueue(color);
        }
    }

    public bool TryPopPixel(out byte color) {
        color = 0xFF;
        
        if (FIFO.Count == 0) return false;

        color = FIFO.Dequeue();

        if (discard_pixels > 0) {
            discard_pixels--;
            return false;
        }

        //if (FIFO.Count > 0)
            pixels_popped++;
            
        return true;
    }

}
