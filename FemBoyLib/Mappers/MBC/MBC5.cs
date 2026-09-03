using System;

namespace FemBoy.Mappers.MBC;

public class MBC5 : MBCMapper {
    public override string Name => "MBC5";
    
    protected Cartridge cartridge;

    public MBC5(Cartridge cartridge) {
        this.cartridge = cartridge;
    }
    
    public override byte Read(ushort address) {
        throw new NotImplementedException();
    }

    public override void Write(ushort address, byte value) {
        throw new NotImplementedException();
    }
}