using System.Diagnostics;

namespace FemBoy;

public class DMA {
    GameBoy gameboy;
    MemoryBus MemoryBus => gameboy.memory_bus;
    
    public DMA(GameBoy gameboy) => this.gameboy = gameboy;
    
    public bool Active { get; set; } = false;
    public bool Requested { get; set; } = false;

    private byte buffered_value = 0;
    
    private ushort source;
    private ushort requested_source;
    private int rw_index;
    private int cycle_counter = 0;
    private int startup_delay = 0;
    public int Cycle => cycle_counter;
    
    public ushort Source => source;
    public int Index => rw_index;
    public byte Register = 0x00;

    public bool restarting = false;

    public void Request(byte value) {
        Register = value;
        requested_source = (ushort)(value << 8);
        Requested = true;
    }
    
    public void Start() {
        if (Active) {
            restarting = true;
            Requested = false;
            startup_delay = 0;
            
        } else {
            source = requested_source;
            rw_index = 0;
            Active = true;
            Requested = false;
            startup_delay = 0;
            cycle_counter = 0;
        }
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
        MemoryBus.Driver = MemoryBusDriver.DMA;
    }

    byte ReadBus() {
        return MemoryBus.Data;
    }

    void WriteMemory(ushort address, byte value) {
        MemoryBus.Address = address;
        MemoryBus.BusState = RWState.Write;
        MemoryBus.Data = value;
        MemoryBus.Target = BusTarget.Memory;
        MemoryBus.Driver = MemoryBusDriver.DMA;
    }
    
    private byte buffer = 0;

    public void Tick() {
        if (!Active) return;

        if (startup_delay < 4) {
            startup_delay++;
            
            if (!restarting)
                return;
            
            if (startup_delay == 4) {
                rw_index = 0;
                cycle_counter = 0;
                source = requested_source;
                rw_index = 0;
                restarting = false;
            }
        }
        
        switch (cycle_counter) {
            case 0:
                if ((source + rw_index) >= 0xFE00) ReadMemory((ushort)((source - 0x2000) + rw_index));
                else ReadMemory((ushort)(source + rw_index));
                break;
            
            case 1:
                buffered_value = ReadBus();
                break;
            
            case 2:
                WriteMemory((ushort)(0xFE00 + rw_index), buffered_value);
                break;
            
            case 3:
                rw_index++;
                
                if (rw_index == 160) {
                    rw_index = 0;
                    Active = false;
                    MemoryBus.Driver = MemoryBusDriver.CPU;
                }
                break;
        }
        
        cycle_counter++;
        if (cycle_counter == 4) cycle_counter = 0;
    }
}