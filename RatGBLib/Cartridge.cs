namespace RatGBLib;

public class Cartridge {
    public byte[] ROM;

    public byte cartridge_type => ROM[0x147];
    public byte ROM_size => ROM[0x148];
    public byte RAM_size => ROM[0x149];

    private byte rom_bank_lo = 1;
    private byte rom_bank_hi = 0;
    private byte banking_mode = 0;

    public Cartridge() { }

    public byte Read(ushort address) {
        if (address < 0x4000) {
            int current_bank = banking_mode == 0 
                ? 0
                : rom_bank_hi << 5;
            
            return ROM[current_bank * 0x4000 + address];
        }
        
        if (address < 0x8000) {
            int current_bank = rom_bank_lo | (rom_bank_hi << 5);

            if ((current_bank & 0x1F) == 0) current_bank++;
                    
            int offset = current_bank * 0x4000 + (address - 0x4000);
            return ROM[offset];
        }
        throw new ArgumentOutOfRangeException(nameof(address));
    }

    public void Write(ushort address, byte value) {
        int current_bank = rom_bank_lo | (rom_bank_hi << 5);
        if (address >= 0x2000 && address < 0x4000) {
            current_bank = (byte)(value & 0x1F);
            
            if (current_bank == 0) current_bank = 1;
        } else if (address >= 0x4000 && address < 0x6000) {
            rom_bank_hi = (byte)(value & 0x03);
        } else if (address >= 0x6000 && address < 0x8000) {
            banking_mode = (byte)(value & 0x01);
        }
    }
}