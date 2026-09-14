namespace FemBoy;

public enum RWState { Read, Write }

public enum MemoryBusDriver { CPU, DMA }
public enum VRAMBusDriver { CPU, PPU }

public enum SelectedBus { Memory, Video }

public class MemoryBus {
    public ushort Address { get; set; }
    public byte Data { get; set; }
    
    public RWState BusState { get; set; }
    public MemoryBusDriver RAMDriver { get; set; } = MemoryBusDriver.CPU;
    public VRAMBusDriver VRAMDriver { get; set; } = VRAMBusDriver.CPU;
}
