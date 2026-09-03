namespace FemBoy;

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
    
    public void Tick() {
        if (!Active) return;

        cycle_counter++;
        if (cycle_counter < 4) return;
        cycle_counter = 0;
        
        byte value = gameboy.RAM.Read((ushort)(source + index));
        gameboy.RAM.Write((ushort)(0xFE00 + index), value);
        
        index++;
        
        if (index == 160) {
            Active = false;
        }
    }
}