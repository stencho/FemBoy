namespace RatGBLib.MBC;

public class NoMapper : IMapper {
    Cartridge cartridge;

    public NoMapper(Cartridge cartridge) {
        this.cartridge = cartridge;
    }
    
    public byte Read(ushort address) {
        if (address < 0x8000)
            return cartridge.ROM[address];
        else if (address is >= 0xA000 or <= 0xBFFF)
            return 0xFF;
        else
            throw new Exception();
    }

    public void Write(ushort address, byte value) { }
}