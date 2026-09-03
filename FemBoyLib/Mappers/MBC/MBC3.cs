using FemBoy.Mappers;

namespace FemBoy.Mappers.MBC;

public class MBC3 : MBCMapper {
    public override string Name => "MBC3";
    
    private RTC rtc;
    private byte rom_bank;
    private byte ram_rtc_select;
    
    private bool real_time_clock = false;
    
    public MBC3(Cartridge cartridge, bool battery_save, bool real_time_clock) {
        this.cartridge = cartridge;
        battery_saving = battery_save;
        this.real_time_clock = real_time_clock;

        RAM_size = cartridge.GetRAMSize();;

        if (battery_saving) {
            RAM = SaveGame.Load(cartridge.ROMCRC32, RAM_size);
        } else {
            if (RAM_size > 0) {
                RAM = new byte[RAM_size];
            }
        }
        
        if (real_time_clock) rtc = new RTC();
    }

    public override byte Read(ushort address) {
        switch (address) { 
            case <= 0x3FFF: 
                return ReadROMBank(0, address);
            
            case <= 0x7FFF: 
                return ReadROMBank(rom_bank, (address - 0x4000));
            
            case >= 0xA000 and <= 0xBFFF:
                if (!RAM_Enabled) return 0xFF;

                if (ram_rtc_select is >= 0x08 and <= 0x0C) {
                    if (real_time_clock)
                        return rtc.Read();
                    return 0xFF;
                }

                if (ram_rtc_select > 0x03 || RAM_size == 0) return 0xFF;
                
                return ReadRAMBank(ram_rtc_select, address);
                
            default: return 0xFF;
        }
    }

    public override void Write(ushort address, byte value) {
        switch (address) { 
            case <= 0x1FFF: 
                RAM_Enabled = (value & 0x0F) == 0x0A; 
                break;

            case <= 0x3FFF: 
                rom_bank = (byte)(value & 0x7F);
                if (rom_bank == 0) rom_bank = 1;
                break;

            case <= 0x5FFF: 
                if (value <= 0x03)
                    ram_rtc_select = (byte)(value & 0x03);
                else if (value >= 0x08 && value <= 0x0C)
                    ram_rtc_select = value;
                break;

            case <= 0x7FFF:
                if (real_time_clock) {
                    byte l = (byte)(value & 0x01);

                    if (rtc.latch == 0 && l == 1) {
                        rtc.Latch();
                    }

                    rtc.latch = l;
                }
                break;

            case >= 0xA000 and <= 0xBFFF:
                if (!RAM_Enabled || RAM_size == 0) return;
                
                if (ram_rtc_select is >= 0x08 and <= 0x0C) {
                    if (real_time_clock) rtc.Write(value);
                    return;
                }

                if (ram_rtc_select > 0x03 || RAM_size == 0) return;
                
                WriteRAMBank(ram_rtc_select, address, value);
                break;
        }
    }
}