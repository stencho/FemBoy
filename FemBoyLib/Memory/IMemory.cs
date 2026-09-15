namespace FemBoy;

public interface IMemory {
    public void HandleBusRW();
    public void HandleVideoBusRW();
    
    public bool WithinVRAM(ushort address);
    public bool WithinOAM(ushort address);
    public bool WithinHRAM(ushort address);
    
    public byte ReadVRAM(ushort address);
    public void WriteVRAM(ushort address, byte value);
    
    public byte Read(ushort address);
    public void Write(ushort address, byte value);
}