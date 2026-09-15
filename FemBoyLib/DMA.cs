using System.Diagnostics;

namespace FemBoy;

public class DMA {
    GameBoy gameboy;
    MemoryBus MemoryBus => gameboy.memory_bus;
    
    public DMA(GameBoy gameboy) => this.gameboy = gameboy;
    
    public bool Active { get; set; } = false;
    public bool Requested { get; set; } = false;
    
    private ushort source;
    private ushort requested_source;
    private int rw_index;
    private int cycle_counter = 0;
    public int Cycle => cycle_counter;
    
    private bool read_phase = true;
    public bool ReadPhase => read_phase;

    public ushort Source => source;
    
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
        cycle_counter = 5;
        read_phase = true;
        source = requested_source;
        MemoryBus.Driver = MemoryBusDriver.DMA;
    }

    public void HandleBusRW() {
        if (MemoryBus.BusState == RWState.Write) {
            Request(MemoryBus.Data);    
        }
        
        if (MemoryBus.BusState == RWState.Read) {
            MemoryBus.Data = Register;
        }
    }
    
    void ReadMemory(ushort address) {
        MemoryBus.Address = address;
        MemoryBus.BusState = RWState.Read;
        MemoryBus.Target = BusTarget.Memory;
    }

    byte ReadBus() {
        if (MemoryBus.Driver == MemoryBusDriver.CPU) {Debugger.Break();}
        return MemoryBus.Data;
    }

    private byte buffer = 0;
    
    public void Tick() {
        if (!Active) return;

        if (cycle_counter > 0) {
            cycle_counter--;
            return;
        }
        
        cycle_counter = 1;

        if (read_phase) {
            
            //MemoryBus.Target = BusTarget.Memory;
            
            if ((source + rw_index) >= 0xFE00) ReadMemory((ushort)((source - 0x2000) + rw_index));
            else ReadMemory((ushort)(source + rw_index));

            //gameboy.CPU.wants_pause = true;
        } else {
            gameboy.RAM.Write((ushort)(0xFE00 + rw_index), ReadBus());
            rw_index++;
        }

        read_phase = !read_phase;
        
        if (rw_index == 160) {
            Active = false;
            MemoryBus.Driver = MemoryBusDriver.CPU;
        }
    }
}