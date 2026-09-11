using System.Diagnostics;

namespace FemBoy;

public class DMA {
    GameBoy gameboy;
    public DMA(GameBoy gameboy) => this.gameboy = gameboy;
    
    public bool Active { get; set; } = false;
    public bool Requested { get; set; } = false;
    
    public bool BusBlocked { get; private set; } = false;
    
    
    private ushort source;
    private ushort requested_source;
    private int rw_index;
    private int cycle_counter = 0;
    public int Cycle => cycle_counter;
    
    private bool read_phase = true;
    private byte buffered_value = 0x00;

    public byte Register = 0x00;

    public void Request(byte value) {
        Register = value;
        requested_source = (ushort)(value << 8);
        Requested = true;
    }
    
    public void Start() {
        rw_index = 0;
        Active = true;
        Requested = false;
        cycle_counter = -4;
        read_phase = true;
        source = requested_source;
    }
    
    public void Tick() {
        if (!Active) return;
        
        cycle_counter++;
        if (cycle_counter < 2) return;
        cycle_counter = 0;

        if (read_phase) {
            BusBlocked = true;
            buffered_value = gameboy.RAM.Read((ushort)(source + rw_index));
        } else {
            gameboy.RAM.Write((ushort)(0xFE00 + rw_index), buffered_value);
            rw_index++;
        }

        read_phase = !read_phase;
        
        if (rw_index == 160) {
            Active = false;
            BusBlocked = false;
        }
    }
}