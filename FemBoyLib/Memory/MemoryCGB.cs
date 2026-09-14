using System;

namespace FemBoy.Memory;

public class MemoryCGB : IMemory  {
    public void Tick() { }

    public bool WithinVRAM(ushort address) => (address is >= 0x8000 and <= 0x9FFF);

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