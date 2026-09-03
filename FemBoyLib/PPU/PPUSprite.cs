namespace FemBoy;

public class Sprite {
    public ushort address;
    
    public int X;
    public int Y;
        
    public byte tile;
    public byte attr;

    public bool BGPriority => (byte)(attr & (1 << 7)) != 0;
        
    public bool FlipY => (byte)(attr & (1 << 6)) != 0;
    public bool FlipX => (byte)(attr & (1 << 5)) != 0;
        
    public bool Palette1 => (byte)(attr & (1 << 4)) != 0;

    public int index = 0;
    
    public Sprite(GameBoy gameboy, ushort address, int index) {
        this.address = address;
        this.index = index;
        
        Y = gameboy.ReadMemory(address) - 16;
        X = gameboy.ReadMemory((ushort)(address + 1)) - 8;

        tile = gameboy.ReadMemory((ushort)(address + 2));
        attr = gameboy.ReadMemory((ushort)(address + 3));
    }
}