namespace RatGBLib;

public class Cartridge {
    public byte[] ROM;
    public byte[] RAM;
    
    public bool RAMEnabled;
    public int RAM_bank => banking_mode == 1 ? rom_bank_hi : 0;

    public byte cartridge_type => ROM[0x147];
    public byte ROM_size_code => ROM[0x148];
    public byte RAM_size_code => ROM[0x149];

    public int RAM_size;
    
    private byte rom_bank_lo = 1;
    private byte rom_bank_hi = 0;

    private int rom_bank_count;
    
    private byte banking_mode = 0;

    public Cartridge(string file_name) {
        using (FileStream file = new(file_name, FileMode.Open)) {
            ROM = new byte[file.Length];
            file.ReadExactly(ROM, 0, ROM.Length);
        }
        
        int ram_size = RAM_size_code switch
        {
            0x00 => 0,
            0x01 => 2 * 1024,
            0x02 => 8 * 1024,
            0x03 => 32 * 1024,
            0x04 => 128 * 1024,
            0x05 => 64 * 1024,
            _ => throw new InvalidDataException(
                $"Unknown RAM size code {RAM_size_code:X2}")
        };

        RAM_size = ram_size;
        
        if (ram_size > 0) {
            RAM = new byte[ram_size];
        }

        rom_bank_count = ROM.Length / 0x4000;
    }
    
    private int GetRAMOffset(ushort address) {
        if (banking_mode == 0) return address - 0xA000;

        return (RAM_bank * 0x2000) + (address - 0xA000);
    }
    
    private byte ReadROMBank(int bank, int offset) {
        int rom_offset = bank * 0x4000 + offset;

        if (rom_offset < 0 || rom_offset >= ROM.Length) {
            Console.WriteLine(
                $"BAD ROM ACCESS: bank={bank:X2} " +
                $"offset={offset:X4} " +
                $"ROM offset={rom_offset:X6} " +
                $"ROM size={ROM.Length:X6}");
            //throw new Exception();
        }

        return ROM[rom_offset];
    }
    public byte Read(ushort address) {
        if (address < 0x4000) {
            int current_bank = banking_mode == 0 
                ? 0 : rom_bank_hi << 5;
            
            //return ROM[current_bank * 0x4000 + address];
            return ReadROMBank(current_bank, address);
        }
        
        if (address < 0x8000) {
            int current_bank = rom_bank_lo | (rom_bank_hi << 5);
            if ((current_bank & 0x1F) == 0) current_bank++;
            
            //return ROM[current_bank * 0x4000 + (address - 0x4000)];
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
            //Console.WriteLine($"MBC ROM LO = {rom_bank_lo:X2}");
        } else if (address >= 0x4000 && address < 0x6000) {
            rom_bank_hi = (byte)(value & 0x03);
            //Console.WriteLine($"MBC ROM HI/RAM BANK = {rom_bank_hi:X2}");
        } else if (address >= 0x6000 && address < 0x8000) {
            banking_mode = (byte)(value & 0x01);
            //Console.WriteLine($"MBC MODE = {banking_mode}");
        } else if (address >= 0xA000 && address < 0xC000) {
            if (RAMEnabled && RAM_size != 0)
                RAM[GetRAMOffset(address)] = value;
        }
    }
}