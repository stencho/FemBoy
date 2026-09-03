using System;
using FemBoy.Mappers;

namespace FemBoy.Mappers.MBC;

public class MBC2 : IMapper {
    Cartridge cartridge;
    
    public string Name => "MBC2";

    public MBC2(Cartridge cartridge) {
        this.cartridge = cartridge;
    }

    public byte Read(ushort address) {
        throw new NotImplementedException();
    }

    public void Write(ushort address, byte value) {
        throw new NotImplementedException();
    }

    public void SaveRAM() {
        throw new NotImplementedException();
    }
}