namespace FemBoy.Memory;

public class DotMatrixRAM : IMemory {
    private GameBoy gameboy;
    private CPU CPU => gameboy.CPU;

    public DotMatrixRAM(GameBoy gameboy) {
        this.gameboy = gameboy;
    }
    
    public readonly byte[] VRAM = new byte[0x2000]; // 8k VRAM
    public readonly byte[] WRAM = new byte[0x2000]; // 8k WRAM
    public readonly byte[] OAM =  new byte[0x00A0]; // 160 bytes object attribute memory
    public readonly byte[] HRAM = new byte[0x007F]; // 127 byte high RAM/zero page

    
    public byte Read(ushort address) {
        switch (address) {
            // Cart ROM
            case (>=0x0000 and <= 0x7FFF): 
                return gameboy.Cartridge.Read(address);
            
            // VRAM
            case (>=0x8000 and <= 0x9FFF):
                return VRAM[address - 0x8000];
            
            // Cart RAM
            case (>=0xA000 and <= 0xBFFF):
                return gameboy.Cartridge.Read((ushort)(address));
            
            // WRAM
            case (>=0xC000 and <= 0xDFFF):
                return WRAM[address - 0xC000];
            
            // Echo RAM
            case (>=0xE000 and <= 0xFDFF):
                return WRAM[address - 0xE000];
            
            // OAM
            case (>=0xFE00 and <= 0xFE9F):
                return OAM[address - 0xFE00];
            
            // UNUSED
            case (>=0xFEA0 and <= 0xFEFF):
                return 0x00;
            
            // I/O registers
            case (>=0xFF00 and <= 0xFF7F): 
                return 0xFF;
            
            case (>=0xFF80 and <= 0xFFFE):
                return HRAM[address - 0xFF80];
        }
        
        return 0xFF;
    }
    
    public void Write(ushort address, byte value) {
        switch (address) {
            // Cart ROM (for MBC bank commands)
            case (>= 0x0000 and <= 0x7FFF): 
                gameboy.Cartridge.Write(address, value);
                break;
        
            // VRAM
            case (>= 0x8000 and <= 0x9FFF):
                VRAM[address - 0x8000] = value;
                break;
        
            // Cart RAM
            case (>= 0xA000 and <= 0xBFFF):
                gameboy.Cartridge.Write((ushort)(address), value); 
                break;
        
            // WRAM
            case (>= 0xC000 and <= 0xDFFF):
                WRAM[address - 0xC000] = value;
                break;
        
            // Echo RAM
            case (>= 0xE000 and <= 0xFDFF):
                WRAM[address - 0xE000] = value;
                break;
        
            // OAM
            case (>= 0xFE00 and <= 0xFE9F):
                OAM[address - 0xFE00] = value;
                break;
        
            // UNUSED
            case (>= 0xFEA0 and <= 0xFEFF):
                break;
        
            // I/O registers
            case (>= 0xFF00 and <= 0xFF7F): 
                break;
        
            // High RAM
            case (>= 0xFF80 and <= 0xFFFE):
                HRAM[address - 0xFF80] = value;
                break;
        }
    }

}