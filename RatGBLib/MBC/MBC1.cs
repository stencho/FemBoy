namespace RatGBLib.MBC;

public class MBC1 : IMapper {
    public byte[] RAM;
    public bool RAMEnabled;
    
    private byte rom_bank_lo = 1;
    private byte rom_bank_hi = 0;
    
    private byte banking_mode = 0;
    
    public int RAM_size;
    
    public int RAM_bank => banking_mode == 1 ? rom_bank_hi : 0;

    Cartridge cartridge;
    
    public MBC1(Cartridge cartridge) {
        this.cartridge = cartridge;
        
        int ram_size = cartridge.RAM_size_code switch
        {
            0x00 => 0,
            0x01 => 2 * 1024,
            0x02 => 8 * 1024,
            0x03 => 32 * 1024,
            0x04 => 128 * 1024,
            0x05 => 64 * 1024,
            _ => throw new InvalidDataException(
                $"Unknown RAM size code {cartridge.RAM_size_code:X2}")
        };

        RAM_size = ram_size;
        
        if (ram_size > 0) {
            RAM = new byte[ram_size];
        }

    }
    
    private int GetRAMOffset(ushort address) {
        if (banking_mode == 0) return address - 0xA000;

        return (RAM_bank * 0x2000) + (address - 0xA000);
    }
    
    private byte ReadROMBank(int bank, int offset) {
        int rom_offset = bank * 0x4000 + offset;

        return cartridge.ROM[rom_offset];
    }
    
    public byte Read(ushort address) {
        if (address < 0x4000) {
            int current_bank = banking_mode == 0 
                ? 0 : rom_bank_hi << 5;
            
            return ReadROMBank(current_bank, address);
        }
        
        if (address < 0x8000) {
            int current_bank = rom_bank_lo | (rom_bank_hi << 5);
            if ((current_bank & 0x1F) == 0) current_bank++;
            
            return ReadROMBank(current_bank, (address - 0x4000));
        }

        if (address >= 0xA000 && address < 0xC000) {
            if (!RAMEnabled || RAM_size == 0) return 0xFF;
            return RAM[GetRAMOffset(address)];
        }

        throw new ArgumentOutOfRangeException($"{address:X4}");
    }

    public void Write(ushort address, byte value) {
        if (address < 0x2000) {
            RAMEnabled = (value & 0x0F) == 0x0A;
        } else if (address >= 0x2000 && address < 0x4000) {
            rom_bank_lo = (byte)(value & 0x1F);
            if (rom_bank_lo == 0) rom_bank_lo = 1;
        } else if (address >= 0x4000 && address < 0x6000) {
            rom_bank_hi = (byte)(value & 0x03);
        } else if (address >= 0x6000 && address < 0x8000) {
            banking_mode = (byte)(value & 0x01);
        } else if (address >= 0xA000 && address < 0xC000) {
            if (RAMEnabled && RAM_size != 0)
                RAM[GetRAMOffset(address)] = value;
        }
    }
}