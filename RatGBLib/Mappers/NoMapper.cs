using RatGBLib;

namespace RatGBLib.Mappers;

public class NoMapper : IMapper {
    Cartridge cartridge;
    
    public string Name => "None";
    
    public NoMapper(Cartridge cartridge) {
        this.cartridge = cartridge;
    }
    
    public byte Read(ushort address) {
        if (address < 0x8000) return cartridge.ROM[address];
        return 0xFF;
    }

    public void Write(ushort address, byte value) { }
    
    public void SaveRAM() { }
}