using System;
using FemBoy.Mappers;

namespace FemBoy.Mappers.MBC;

public class MBC1 : MBCMapper {
    public override string Name => "MBC1";
    
    private byte banking_mode = 0;
    private byte rom_bank_lo = 1;
    private byte rom_bank_hi = 0;
    
    int RAM_bank => banking_mode == 1 ? rom_bank_hi : 0;
    
    
    public MBC1(Cartridge cartridge, bool battery_save) {
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
            case < 0x4000: {
                int current_bank = banking_mode == 0 ? 0 : rom_bank_hi << 5;
                return ReadROMBank(current_bank, address);
            }
            
            case < 0x8000: {
                int current_bank = rom_bank_lo;
                if (banking_mode == 0)
                    current_bank |= rom_bank_hi << 5;
                if ((current_bank & 0x1F) == 0)
                    current_bank++;
                
                if ((current_bank & 0x1F) == 0) current_bank++;
                return ReadROMBank(current_bank, (address - 0x4000));
            }

            case >= 0xA000 and <0xC000: {
                if (!RAM_Enabled || RAM_size == 0) return 0xFF;
                return ReadRAMBank(RAM_bank, address);
            }
            
            default: throw new ArgumentOutOfRangeException($"{address:X4}");
        }
    }

    public override void Write(ushort address, byte value) {
        switch (address) {
            case < 0x2000: 
                RAM_Enabled = (value & 0x0F) == 0x0A;
                break;
            
            case < 0x4000: 
                rom_bank_lo = (byte)(value & 0x1F);
                if (rom_bank_lo == 0) rom_bank_lo = 1;
                break;
            
            case < 0x6000:
                rom_bank_hi = (byte)(value & 0x03);
                break;
            
            case < 0x8000:
                banking_mode = (byte)(value & 0x01);
                break;
            
            case >= 0xA000 and < 0xC000: 
                if (RAM_Enabled && RAM_size != 0) 
                    WriteRAMBank(RAM_bank, address, value);
                break;
        }
    }
}