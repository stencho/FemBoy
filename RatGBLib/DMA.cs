namespace RatGBLib;

public class DMA {
    GameBoy gameboy;
    public DMA(GameBoy gameboy) => this.gameboy = gameboy;
    
    public bool Active { get; set; } = false;
    private ushort source;
    private int index;
    private int cycle_counter = 0;

    public byte Register = 0x00;

    public void Start(byte value) {
        Register = value;
        source = (ushort)(value << 8);
        index = 0;
        Active = true;
        cycle_counter = 0;
    }
    
    private byte ReadDMASource(ushort address) {
        if (address < 0x8000)
            return gameboy.cartridge.Read(address);

        if (address < 0xA000)
            return gameboy.ReadVRAM(address);

        if (address < 0xC000)
            return gameboy.cartridge.Read(address);

        if (address < 0xE000)
            return gameboy.ReadByte(address);

        if (address < 0xFE00)
            return gameboy.ReadByte(address);

        if (address < 0xFEA0)
            return gameboy.ReadByte((ushort)(address - 0x2000));
        
        if (address >= 0xFF00)
            return gameboy.ReadByte((ushort)(address - 0x2000));
        
        return gameboy.ReadByte(address);
    }
    
    
    public void Execute() {
        if (!Active) return;

        cycle_counter++;
        if (cycle_counter < 4) return;
        cycle_counter = 0;

        byte value = ReadDMASource((ushort)(source + index));
        
        gameboy.WriteOAM(index, value);
        
        index++;
        
        if (index == 160) Active = false;
    }
}