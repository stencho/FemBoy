namespace RatGBLib.MBC;

public class MBC3 : IMapper {
    Cartridge cartridge;

    public MBC3(Cartridge cartridge) {
        this.cartridge = cartridge;
    }

    public byte Read(ushort address) {
        throw new NotImplementedException();
    }

    public void Write(ushort address, byte value) {
        throw new NotImplementedException();
    }
}