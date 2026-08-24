using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RatGBLib;


public class GameBoy {
    public const int CYCLES_PER_FRAME = 70_224;
    public const int SCANLINES_PER_FRAME = 154;
    public const int DOTS_PER_SCANLINE = 456;

    public readonly byte[] RAM =  new byte[0x10000];
    

    public Cartridge cartridge;
    
    /*
       0000–3FFF    16 KiB cartridge ROM bank 0
       4000–7FFF    16 KiB switchable cartridge ROM
       8000–9FFF    8 KiB VRAM
       A000–BFFF    8 KiB cartridge RAM
       C000–CFFF    4 KiB WRAM
       D000–DFFF    4 KiB WRAM
       E000–FDFF    Echo RAM
       FE00–FE9F    OAM
       FEA0–FEFF    unusable
       FF00–FF7F    I/O registers
       FF80–FFFE    HRAM
       FFFF         Interrupt Enable
     */
    
    public PPU PPU;
    public CPU CPU;
    public DMA DMA;

    public Timer timer;
    
    public Joypad joypad;
    
    public GameBoy() {
        PPU = new PPU(this);
        CPU = new CPU(this);
        DMA = new DMA(this);
        
        timer = new Timer(this);
        
        joypad = new Joypad(this);
        
        CPU.REGISTERS.A = 0x01;
        CPU.REGISTERS.F = 0xB0;
        
        CPU.REGISTERS.B = 0x00;
        CPU.REGISTERS.C = 0x13;
        
        CPU.REGISTERS.D = 0x13;
        CPU.REGISTERS.E = 0xD8;
        
        CPU.REGISTERS.H = 0x01;
        CPU.REGISTERS.L = 0x4D;
        
        WriteByte(0xFF10, 0x80); // NR10
        WriteByte(0xFF11, 0xBF); // NR11
        WriteByte(0xFF12, 0xF3); // NR12
        WriteByte(0xFF14, 0xBF); // NR14
        
        WriteByte(0xFF16, 0x3F); // NR21
        WriteByte(0xFF17, 0x00); // NR22
        WriteByte(0xFF19, 0xBF); // NR24
        
        WriteByte(0xFF1A, 0x7F); // NR30
        WriteByte(0xFF1B, 0xFF); // NR31
        WriteByte(0xFF1C, 0x9F); // NR32
        WriteByte(0xFF1E, 0xBF); // NR34
        
        WriteByte(0xFF20, 0xFF); // NR41
        WriteByte(0xFF21, 0x00); // NR42
        WriteByte(0xFF22, 0x00); // NR43
        WriteByte(0xFF23, 0xBF); // NR44
        
        WriteByte(0xFF24, 0x77); // NR50
        WriteByte(0xFF25, 0xF3); // NR51
        WriteByte(0xFF26, 0xF1); // NR52 (0x85 for Game Boy Color)

        PPU.LCDC = 0x91;
        PPU.STAT = 0x85;
        PPU.LY = 0x90;
        PPU.LYC = 0x00;

        timer.TIMA = 0x00;
        timer.TMA = 0x00;
        timer.TAC = 0x00;
        
        WriteByte(0xFF47, 0xFC); // BGP (Background Palette)
        WriteByte(0xFF48, 0xFF); // OBP0
        WriteByte(0xFF49, 0xFF); // OBP1
        WriteByte(0xFF4A, 0x00); // WY
        WriteByte(0xFF4B, 0x00); // WX
        
        WriteByte(0xFFFF, 0x00); // IE (Interrupt Enable)
    }
    
    public uint last_tima_read = 0;
    public uint last_tima_write = 0;
    public byte ReadByte(ushort address) {
        if (address == 0xFF0F) return (byte)(CPU.REGISTERS.IF | 0xE0);
        if (address == 0xFFFF) return (byte)(CPU.REGISTERS.IE | 0xE0);
        
        if (address == (ushort)PPURegisterAddresses.LCDC) return PPU.LCDC;
        
        if (address == (ushort)PPURegisterAddresses.LY) return PPU.LY;
        if (address == (ushort)PPURegisterAddresses.LYC) return PPU.LYC;
        
        if (address == (ushort)PPURegisterAddresses.SCY) return PPU.SCY;
        if (address == (ushort)PPURegisterAddresses.SCX) return PPU.SCX;
        
        if (address == (ushort)PPURegisterAddresses.STAT) return PPU.STAT;
        
        if (address == (ushort)PPURegisterAddresses.BGP) return PPU.BGP;
        if (address == (ushort)PPURegisterAddresses.OBP0) return PPU.OBP0;
        if (address == (ushort)PPURegisterAddresses.OBP1) return PPU.OBP1;
        if (address == (ushort)PPURegisterAddresses.WY) return PPU.WY;
        if (address == (ushort)PPURegisterAddresses.WX) return PPU.WX;

        if (address == (ushort)TimerRegisterAddresses.DIV) return timer.DIV;
        if (address == (ushort)TimerRegisterAddresses.TIMA) return timer.TIMA;
        if (address == (ushort)TimerRegisterAddresses.TMA) return timer.TMA;
        if (address == (ushort)TimerRegisterAddresses.TAC) return (byte)(timer.TAC | 0xF8);
        
        if (address == 0xFF00) return joypad.ReadState();
        
        if (address < 0x8000) {
            return cartridge.Read(address);
        }
        
        if (address >= 0x8000 && address <= 0x9FFF) 
            if (PPU.Mode == PPU.STATMode.LCD_TRANSFER) 
                return 0xFF;
        
        if (address >= 0xE000 && address <= 0xFDFF) return RAM[address - 0x2000];

        if (address == (ushort)PPURegisterAddresses.DMA) {
            return DMA.Register;
        }
        
        if (address == 0xFF85)
            Console.WriteLine(
                $"HRAM READ FF85={RAM[address]:X2} " +
                $"PC={CPU.REGISTERS.PC:X4} " +
                $"T={TotalCycles}");
        return RAM[address];
    }
    
    public void WriteByte(ushort address, byte value) {
        if (address == 0xFF0F) {
            CPU.REGISTERS.IF = (byte)(value & 0x1F);
            return;
        }
        if (address == 0xFFFF) {
            CPU.REGISTERS.IE = (byte)(value & 0x1F);
            return;
        }

        if (address == (ushort)PPURegisterAddresses.LCDC) {
            PPU.LCDC = value;
            return;
        }
        
        if (address == (ushort)PPURegisterAddresses.LY) {
            PPU.LY = 0x00;
            return;
        }
        if (address == (ushort)PPURegisterAddresses.LYC) {
            PPU.LYC = value;
            return;
        }
        
        if (address == (ushort)PPURegisterAddresses.SCY) {
            PPU.SCY = value;
            return;
        }
        if (address == (ushort)PPURegisterAddresses.SCX) {
            PPU.SCX = value;
            return;
        }
        
        if (address == (ushort)PPURegisterAddresses.BGP) {
            PPU.BGP = value;
            return;
        }
        if (address == (ushort)PPURegisterAddresses.OBP0) {
            PPU.OBP0 = value;
            return;
        }
        if (address == (ushort)PPURegisterAddresses.OBP1) {
            PPU.OBP1 = value;
            return;
        }
        if (address == (ushort)PPURegisterAddresses.WY) {
            PPU.WY = value;
            return;
        }
        if (address == (ushort)PPURegisterAddresses.WX) {
            PPU.WX = value;
            return;
        }
        
        if (address == (ushort)TimerRegisterAddresses.DIV) {
            timer.ResetDivider();
            return;
        }

        if (address == (ushort)TimerRegisterAddresses.TIMA) { 
            Console.WriteLine(
                $"TIMA WRITE: " +
                $"T={TotalCycles} DELTA={TotalCycles - last_tima_write} " +
                $"value={value:X2}");
            last_tima_write = TotalCycles;
            if (timer.ReloadPending) {
                if (timer.ReloadDelay > 0) {
                    timer.CancelPendingTIMAReload(value);
                } else {
                    timer.TIMA = timer.TMA;
                }
            } else {
                timer.TIMA = value;
            }
            
            return;
        }
        
        if (address == (ushort)TimerRegisterAddresses.TMA) {
            timer.TMA = value;
            if (timer.ReloadPending && (timer.ReloadDelay == 0 || timer.ReloadDelay == 1)) timer.TIMA = value;
            return;
        }
        
        if (address == (ushort)TimerRegisterAddresses.TAC) {
            timer.WriteTAC(value);
            return;
        }
        
        if (address == (ushort)PPURegisterAddresses.DMA) {
            DMA.Register = value;
            Console.WriteLine(
                $"DMA START {address:X4} = {value:X2} " +
                $"T={TotalCycles} active={DMA.Active}"
            );
            DMA.Start(value);
            return;
        }
        
        if (address == (ushort)PPURegisterAddresses.STAT) {
            PPU.STAT = value;
            return;
        }
        
        // INPUT HANDLING
        if (address == 0xFF00) {
            joypad.select_dpad = ((value & 0x10) == 0);
            joypad.select_buttons = ((value & 0x20) == 0);
            return;
        }
        
        if (address < 0x8000 || (address >= 0xA000 && address < 0xC000)) {
            cartridge.Write(address, value);
            return;
        }
        
        RAM[address] = value;
    }
    
    public byte ReadByte(PPURegisterAddresses address) => ReadByte((ushort)address);
    public void WriteByte(PPURegisterAddresses address, byte value) => WriteByte((ushort)address, value);
    
    public byte ReadVRAM(ushort address) {
        if (address < 0x8000 || address > 0x9FFF) 
            throw new ArgumentOutOfRangeException(nameof(address));
        return RAM[address];
    }
    public byte ReadOAM(ushort address) {
        if (address < 0xFE00 || address > 0xFE9F) 
            throw new ArgumentOutOfRangeException(nameof(address));
        return RAM[address];
    }
    public void WriteOAM(int index, byte value) {
        RAM[0xFE00 + index] = value;
    }
    
    public void RequestInterrupt(CPU.InterruptMask interrupt) {
        Console.WriteLine(
            $"IRQ REQUEST {interrupt} " +
            $"PC={CPU.REGISTERS.PC:X4} " +
            $"T={TotalCycles}");
        CPU.REGISTERS.IF |= (byte)interrupt;
    }
    
    public void LoadROM(string file_name) {
        cartridge = new Cartridge(file_name);
    }

    public int CyclesThisFrame = 0;
    public uint TotalCycles = 0;
    
    public void Tick(int cycles) {
        for (int i = 0; i < cycles; i++) {
            TotalCycles++;
            timer.Execute();
            PPU.Execute();   
            DMA.Execute();
        }
        CyclesThisFrame += cycles;
    }
    
    public int Execute() {
        CPU.Execute();
        
        int cycles = CyclesThisFrame;
        CyclesThisFrame = 0;
        
        return cycles;
    }
}