namespace FemBoy;

public interface IMemory {
    public void Tick();
    
    public bool WithinVRAM(ushort address);
    
    public byte ReadVRAM(ushort address);
    public void WriteVRAM(ushort address, byte value);
    
    public byte Read(ushort address);
    public void Write(ushort address, byte value);
}