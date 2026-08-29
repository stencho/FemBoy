using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RatGBLib;


public class GameBoy {
    public const int CYCLES_PER_FRAME = 70_224;
    public const uint CLOCK_SPEED_HZ = 4_194_304;
    public const int SCANLINES_PER_FRAME = 154;
    public const int VBLANK_SCANLINE = 144;
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
    
    public Audio Audio;
    
    public Timer timer;
    public Serial serial;
    public Joypad joypad;
    
    public GameBoy() {
        PPU = new PPU(this);
        CPU = new CPU(this);
        DMA = new DMA(this);
        Audio = new Audio(this);
        timer = new Timer(this);
        serial = new Serial(this);
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
    
    public byte ReadByte(ushort address) {
        // INTERRUPT REGISTERS
        if (address == InterruptRegisterAddresses.IF) return (byte)(CPU.REGISTERS.IF | 0xE0);
        if (address == InterruptRegisterAddresses.IE) return CPU.REGISTERS.IE;
        
        // PPU REGISTERS
        if (address == PPURegisterAddresses.LCDC) return PPU.LCDC;
        
        if (address == PPURegisterAddresses.LY) return PPU.LY;
        if (address == PPURegisterAddresses.LYC) return PPU.LYC;
        
        if (address == PPURegisterAddresses.SCY) return PPU.SCY;
        if (address == PPURegisterAddresses.SCX) return PPU.SCX;
        
        if (address == PPURegisterAddresses.STAT) return PPU.STAT;
        
        if (address == PPURegisterAddresses.BGP) return PPU.BGP;
        if (address == PPURegisterAddresses.OBP0) return PPU.OBP0;
        if (address == PPURegisterAddresses.OBP1) return PPU.OBP1;
        if (address == PPURegisterAddresses.WY) return PPU.WY;
        if (address == PPURegisterAddresses.WX) return PPU.WX;
        if (address == PPURegisterAddresses.DMA) return DMA.Register;
        
        // TIMER REGISTERS
        if (address == TimerRegisterAddresses.DIV) return timer.DIV;
        if (address == TimerRegisterAddresses.TIMA) return timer.TIMA;
        if (address == TimerRegisterAddresses.TMA) return timer.TMA;
        if (address == TimerRegisterAddresses.TAC) return (byte)(timer.TAC | 0xF8);
        
        // JOYPAD REGISTER
        if (address == Joypad.RegisterAddress) return joypad.ReadState();
        
        // SERIAL REGISTERS
        if (address == SerialRegisterAddresses.SB) return serial.SB;
        if (address == SerialRegisterAddresses.SC) return serial.SC;
        
        // AUDIO REGISTERS 
        if (address == AudioRegisterAddresses.NR10) return Audio.NR10;
        if (address == AudioRegisterAddresses.NR11) return Audio.NR11;
        if (address == AudioRegisterAddresses.NR12) return Audio.NR12;
        if (address == AudioRegisterAddresses.NR13) return Audio.NR13;
        if (address == AudioRegisterAddresses.NR14) return Audio.NR14;
        
        if (address == AudioRegisterAddresses.NR21) return Audio.NR21;
        if (address == AudioRegisterAddresses.NR22) return Audio.NR22;
        if (address == AudioRegisterAddresses.NR23) return Audio.NR23;
        if (address == AudioRegisterAddresses.NR24) return Audio.NR24;
        
        if (address == AudioRegisterAddresses.NR30) return Audio.NR30;
        if (address == AudioRegisterAddresses.NR31) return Audio.NR31;
        if (address == AudioRegisterAddresses.NR32) return Audio.NR32;
        if (address == AudioRegisterAddresses.NR33) return Audio.NR33;
        if (address == AudioRegisterAddresses.NR34) return Audio.NR34;
        
        if (address == AudioRegisterAddresses.NR41) return Audio.NR41;
        if (address == AudioRegisterAddresses.NR42) return Audio.NR42;
        if (address == AudioRegisterAddresses.NR43) return Audio.NR43;
        if (address == AudioRegisterAddresses.NR44) return Audio.NR44;
        
        if (address == AudioRegisterAddresses.NR50) return Audio.NR50;
        if (address == AudioRegisterAddresses.NR51) return Audio.NR51;
        if (address == AudioRegisterAddresses.NR52) return Audio.NR52;
        
        // Unmapped bits
        if (address == 0xFF03 || address is >= 0xFF08 and <= 0xFF0E || address == 0xFF15 || address == 0xFF1F || address is >= 0xFF27 and <= 0xFF2F || address is >= 0xFF4C and <= 0xFF7F) { return 0xFF; }
        
        // CARTRIDGE
        if (address < 0x8000 || (address >= 0xA000 && address < 0xC000)) { return cartridge.Read(address); }
        
        // ECHO RAM
        if (address >= 0xE000 && address <= 0xFDFF) return RAM[address - 0x2000];
        
        return RAM[address];
    }
    
    public void WriteByte(ushort address, byte value) {
        // INTERRUPT REGISTER
        if (address == InterruptRegisterAddresses.IF) { CPU.REGISTERS.IF = (byte)(value & 0x1F); return; }
        if (address == InterruptRegisterAddresses.IE) { CPU.REGISTERS.IE = value; return; }

        // PPU REGISTERS
        if (address == PPURegisterAddresses.LCDC) {
            bool lcd_old = PPU.LCDEnabled;
            PPU.LCDC = value; 
            
            // TURN ON LCD
            if (PPU.LCDEnabled && !lcd_old) {
                PPU.LCDOn();
            }
            
            // TURN OFF LCD
            if (!PPU.LCDEnabled && lcd_old) {
                PPU.LCDOff();
            }
            
            return; 
        }
        
        if (address == PPURegisterAddresses.STAT) { PPU.STAT = value; return; }
        
        if (address == PPURegisterAddresses.SCY) { PPU.SCY = value; return; }
        if (address == PPURegisterAddresses.SCX) {
        {
            PPU.SCX = value;
            return;
        } }

        if (address == PPURegisterAddresses.LY) { PPU.LY = 0x00; return; }
        if (address == PPURegisterAddresses.LYC) { PPU.LYC = value; return; }
        
        if (address == PPURegisterAddresses.BGP) { PPU.BGP = value; return; }
        if (address == PPURegisterAddresses.OBP0) { PPU.OBP0 = value; return; }
        if (address == PPURegisterAddresses.OBP1) { PPU.OBP1 = value; return; }
        if (address == PPURegisterAddresses.WY) { PPU.WY = value; return; }
        if (address == PPURegisterAddresses.WX) { PPU.WX = value; return; }
        
        if (address == PPURegisterAddresses.DMA) { DMA.Register = value; DMA.Start(value); return; }
        
        // TIMER REGISTERS
        if (address == TimerRegisterAddresses.DIV) { timer.ResetDivider(); return; }

        if (address == TimerRegisterAddresses.TIMA) { 
            if (timer.ReloadPending) {
                if (timer.ReloadDelay <= 1) {
                    timer.CancelPendingTIMAReload(value);
                } else {
                    timer.TIMA = timer.TMA;
                }
            } else {
                timer.TIMA = value;
            }
            
            return;
        }
        
        if (address == TimerRegisterAddresses.TMA) {
            timer.TMA = value;
            if (timer.ReloadPending && timer.ReloadDelay <= 1) timer.TIMA = value;
            return;
        }
        
        if (address == TimerRegisterAddresses.TAC) { timer.WriteTAC(value); return; }
        
        // JOYPAD REGISTER
        if (address == Joypad.RegisterAddress) {
            joypad.select_dpad = ((value & 0x10) == 0);
            joypad.select_buttons = ((value & 0x20) == 0);
            return;
        }
        
        // SERIAL REGISTERS 
        if (address == SerialRegisterAddresses.SB) { serial.SB = value; return; }

        if (address == SerialRegisterAddresses.SC) {
            Console.Write((char)RAM[SerialRegisterAddresses.SB]);
            serial.SC = value; return;
        }
        
        // AUDIO REGISTERS
        if (address == AudioRegisterAddresses.NR10) { Audio.NR10 = value; return; }
        if (address == AudioRegisterAddresses.NR11) { Audio.NR11 = value; return; }
        if (address == AudioRegisterAddresses.NR12) { Audio.NR12 = value; return; }
        if (address == AudioRegisterAddresses.NR13) { Audio.NR13 = value; return; }
        if (address == AudioRegisterAddresses.NR14) { Audio.NR14 = value; return; }
        
        if (address == AudioRegisterAddresses.NR21) { Audio.NR21 = value; return; }
        if (address == AudioRegisterAddresses.NR22) { Audio.NR22 = value; return; }
        if (address == AudioRegisterAddresses.NR23) { Audio.NR23 = value; return; }
        if (address == AudioRegisterAddresses.NR24) { Audio.NR24 = value; return; }
        
        if (address == AudioRegisterAddresses.NR30) { Audio.NR30 = value; return; }
        if (address == AudioRegisterAddresses.NR31) { Audio.NR31 = value; return; }
        if (address == AudioRegisterAddresses.NR32) { Audio.NR32 = value; return; }
        if (address == AudioRegisterAddresses.NR33) { Audio.NR33 = value; return; }
        if (address == AudioRegisterAddresses.NR34) { Audio.NR34 = value; return; }
        
        if (address == AudioRegisterAddresses.NR41) { Audio.NR41 = value; return; }
        if (address == AudioRegisterAddresses.NR42) { Audio.NR42 = value; return; }
        if (address == AudioRegisterAddresses.NR43) { Audio.NR43 = value; return; }
        if (address == AudioRegisterAddresses.NR44) { Audio.NR44 = value; return; }
        
        if (address == AudioRegisterAddresses.NR50) { Audio.NR50 = value; return; }
        if (address == AudioRegisterAddresses.NR51) { Audio.NR51 = value; return; }
        if (address == AudioRegisterAddresses.NR52) { Audio.NR52 = value; return; }

        // Unmapped bits
        if (address == 0xFF03 || address is >= 0xFF08 and <= 0xFF0E || address == 0xFF15 || address == 0xFF1F || address is >= 0xFF27 and <= 0xFF2F || address is >= 0xFF4C and <= 0xFF7F) { return; }
        
        // CARTRIDGE
        if (address < 0x8000 || (address >= 0xA000 && address < 0xC000)) { cartridge.Write(address, value); return; }
        
        // ECHO RAM
        if (address >= 0xE000 && address <= 0xFDFF) { RAM[address - 0x2000] = value; return; }
        
        RAM[address] = value;
    }
    
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
        CPU.REGISTERS.IF |= (byte)interrupt;
    }
    
    public void LoadROM(string file_name) {
        cartridge = new Cartridge(this, file_name);
    }

    public int CyclesThisFrame = 0;
    public uint TotalCycles = 0;
    private uint save_timer = 0;
    
    public void Tick(int cycles) {
        for (int i = 0; i < cycles; i++) {
            TotalCycles++;
            
            timer.Execute();
            PPU.Execute();   
            DMA.Execute();
            serial.Execute();
        }
        
        CyclesThisFrame += cycles;
    }

    
    
    public int Execute() {
        CPU.Execute();
        
        int cycles = CyclesThisFrame;
        CyclesThisFrame = 0;

        save_timer++;
        if (save_timer > CLOCK_SPEED_HZ) {
            save_timer = 0;
            if (cartridge.HasBattery && !SaveGame.CurrentlySaving) 
                cartridge.Mapper.SaveRAM();
        }
        
        return cycles;
    }
}