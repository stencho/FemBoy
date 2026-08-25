using RatGBLib.Mappers;

namespace RatGBLib.Mappers.MBC;

public class MBC3 : IMapper {
    Cartridge cartridge;
    
    public byte[] RAM;
    public int RAM_size;
    public bool RAMEnabled;

    private byte rom_bank;
    private byte rtc_select;
    private byte rtc_latch;
    
    private bool battery_saving = false;
    public bool BatterySaving => battery_saving;
    private bool RAM_dirty = false;
    public bool RAMDirty => RAM_dirty;
    
    public MBC3(Cartridge cartridge, bool battery_save) {
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