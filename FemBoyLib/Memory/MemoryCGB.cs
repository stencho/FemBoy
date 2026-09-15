using System;

namespace FemBoy.Memory;

public class MemoryCGB : IMemory  {
    public void HandleBusRW() { }
    public void HandleVideoBusRW() { }

    public bool WithinVRAM(ushort address) => (address is >= 0x8000 and <= 0x9FFF);
    public bool WithinOAM(ushort address) => (address is >= 0xFE00 and <= 0xFE9F);
    
    public bool WithinHRAM(ushort address) => (address is >=0xFF80 and <= 0xFFFE);

    public byte ReadVRAM(ushort address) {
        throw new NotImplementedException();
    }

    public void WriteVRAM(ushort address, byte value) {
        throw new NotImplementedException();
    }

    public byte Read(ushort address) {
        throw new NotImplementedException();
    }

    public void Write(ushort address, byte value) {
        throw new NotImplementedException();
    }
}