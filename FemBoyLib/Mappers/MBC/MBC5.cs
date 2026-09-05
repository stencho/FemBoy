using System;

namespace FemBoy.Mappers.MBC;

public class MBC5 : MBCMapper {
    public override string Name => "MBC5";
    
    private ushort rom_bank = 1;
    private byte ram_bank = 0;
    
    public MBC5(Cartridge cartridge, bool battery_save) {
        this.cartridge = cartridge;
        battery_saving = battery_save;

        RAM_size = cartridge.GetRAMSize();

        if (battery_saving) {
            RAM = SaveGame.Load(cartridge.ROMCRC32, RAM_size);
        } else {
            if (RAM_size > 0) {
                RAM = new byte[RAM_size];
            }
        }
    }
    
    public override byte Read(ushort address) {
        switch (address) {
            case <= 0x3FFF: 
                return ReadROMBank(0, address);
            
            case <= 0x7FFF:
                return ReadROMBank(rom_bank, (address - 0x4000));
            
            case >= 0xA000 and <= 0xBFFF:
                if (!RAM_Enabled || cartridge.GetRAMSize() == 0) return 0xFF;
                return ReadRAMBank(ram_bank, address);
            
            default:
                return 0xFF;
        }
    }

    public override void Write(ushort address, byte value) {
        switch (address) {
            case <= 0x1FFF:
                RAM_Enabled = (value & 0x0F) == 0x0A;
                break;
            
            case <= 0x2FFF:
                rom_bank = (ushort)((rom_bank & 0x100) | value);
                break;
            
            case <= 0x3FFF:
                rom_bank = (ushort)((rom_bank & 0x0FF) | ((value & 0x01) << 8));
                break;
            
            case <= 0x5FFF:
                ram_bank = (byte)(value & 0x0F);
                break;
            
            case >= 0xA000 and <= 0xBFFF:
                if (!RAM_Enabled || cartridge.GetRAMSize() == 0) return;
                WriteRAMBank(ram_bank, address, value);
                break;
        }
    }
}