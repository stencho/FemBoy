namespace FemBoy;

public class DMA {
    GameBoy gameboy;
    public DMA(GameBoy gameboy) => this.gameboy = gameboy;
    
    public bool Active { get; set; } = false;
    
    private ushort source;
    private int rw_index;
    private int cycle_counter = 0;
    private bool read_phase = true;
    private byte buffered_value = 0x00;

    public byte Register = 0x00;
    
    public void Start(byte value) {
        Register = value;
        source = (ushort)(value << 8);
        rw_index = 0;
        Active = true;
        cycle_counter = -4;
        read_phase = true;
    }
    
    public void Tick() {
        if (!Active) return;
        
        cycle_counter++;
        if (cycle_counter < 2) return;
        cycle_counter = 0;

        if (read_phase) {
            buffered_value = gameboy.RAM.Read((ushort)(source + rw_index));
        } else {
            gameboy.RAM.Write((ushort)(0xFE00 + rw_index), buffered_value);
            rw_index++;
        }

        read_phase = !read_phase;
        
        if (rw_index == 160) {
            Active = false;
        }
    }
}