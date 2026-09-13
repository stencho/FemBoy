namespace FemBoy;

public enum RWState { Read, Write }

public enum MemoryBusDriver { CPU, DMA }
public enum VRAMBusDriver { CPU, PPU }

public enum SelectedBus { Memory, Video }

public interface IBus {
    public ushort Address { get; set; }
    public byte Data { get; set; }
    
    public RWState BusState { get; set; }
}

public class MemoryBus : IBus {
    public ushort Address { get; set; } = 0x0000;
    public byte Data { get; set; } = 0x00;

    public RWState BusState { get; set; } = RWState.Read;
    public MemoryBusDriver Driver { get; set; } = MemoryBusDriver.CPU;
}

public class VRAMBus : IBus {
    public ushort Address { get; set; } = 0x0000;
    public byte Data { get; set; } = 0x00;

    public RWState BusState { get; set; } = RWState.Read;
    public VRAMBusDriver Driver { get; set; } = VRAMBusDriver.CPU;
}