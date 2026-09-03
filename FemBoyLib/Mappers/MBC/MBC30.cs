using System;

namespace FemBoy.Mappers.MBC;

public class MBC30 : IMapper {
    public string Name => "MBC30";
    
    Cartridge cartridge;
    public byte[] RAM;
    
    public MBC30(Cartridge cartridge, bool battery_save, bool real_time_clock) {
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