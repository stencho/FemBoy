using System;

namespace FemBoy.Memory;

public class MemoryCGB : IMemory  {
    public byte Read(ushort address) {
        throw new NotImplementedException();
    }

    public void Write(ushort address, byte value) {
        throw new NotImplementedException();
    }
}