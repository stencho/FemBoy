namespace FemBoy.Mappers;

public abstract class MBCMapper : IMapper{
    public abstract string Name { get; }
    
    protected byte[] RAM;
    protected int RAM_size;
    protected bool RAM_Enabled;
    
    protected bool RAM_dirty = false;
    public bool RAMDirty => RAM_dirty;
    
    protected bool battery_saving = false;
    public bool BatterySaving => battery_saving;

    protected Cartridge cartridge;
    
    public abstract byte Read(ushort address);

    public abstract void Write(ushort address, byte value);
    
    public void SaveRAM() {
        if (RAM_dirty) {
            SaveGame.Save(cartridge.ROMCRC32, RAM);
            RAM_dirty = false;
        }
    }
    
    protected byte ReadROMBank(int bank, int offset) {
        int rom_offset = bank * 0x4000 + offset;

        if ((uint)rom_offset >= (uint)cartridge.ROM.Length)
            return 0xFF;

        return cartridge.ROM[rom_offset];
    }

    protected byte ReadRAMBank(int bank, ushort address) {
        int offset = bank * 0x2000 + (address - 0xA000);

        if ((uint)offset >= (uint)RAM.Length)
            return 0xFF;

        return RAM[offset];
    }

    protected void WriteRAMBank(int bank, ushort address, byte value) {
        int offset = bank * 0x2000 + (address - 0xA000);

        if ((uint)offset >= (uint)RAM.Length)
            return;

        RAM[offset] = value;
        RAM_dirty |= battery_saving;
    }
}