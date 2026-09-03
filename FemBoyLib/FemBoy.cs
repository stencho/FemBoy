using System.Diagnostics;
using FemBoy.Memory;

namespace FemBoy;

enum GameBoyType {
    Color, DotMatrix
}

public class GameBoy {
    public const int CYCLES_PER_FRAME = 70_224;
    public const uint CLOCK_SPEED_HZ = 4_194_304;
    public const int SCANLINES_PER_FRAME = 154;
    public const int VBLANK_SCANLINE = 144;
    public const int DOTS_PER_SCANLINE = 456;
    
    private GameBoyType type = GameBoyType.DotMatrix;

    private const ushort SpeedSwitchMemoryAddress = 0xFF4D;
    
    private bool double_speed_mode = false;
    
    public CPU CPU;
    public IMemory RAM;
    public PPU PPU;
    public DMA DMA;
    public Serial serial;
    public Timer Timer;
    public Audio APU;
    
    public Joypad joypad;
    
    public Cartridge Cartridge;

    public GameBoy() {
        CPU = new CPU(this);
        PPU = new PPU(this);
        DMA = new DMA(this);
        RAM = new DotMatrixRAM(this);
        Timer = new Timer(this);
        serial = new Serial(this);
        joypad = new Joypad(this);
        APU = new Audio(this);
        
        CPU.Registers.A = 0x01;
        CPU.Registers.F = 0xB0;
        
        CPU.Registers.B = 0x00;
        CPU.Registers.C = 0x13;
        
        CPU.Registers.D = 0x00; // 0x13
        CPU.Registers.E = 0xD8;
        
        CPU.Registers.H = 0x01;
        CPU.Registers.L = 0x4D;
        
        WriteMemory(0xFF10, 0x80); // NR10
        WriteMemory(0xFF11, 0xBF); // NR11
        WriteMemory(0xFF12, 0xF3); // NR12
        WriteMemory(0xFF14, 0xBF); // NR14
        
        WriteMemory(0xFF16, 0x3F); // NR21
        WriteMemory(0xFF17, 0x00); // NR22
        WriteMemory(0xFF19, 0xBF); // NR24
        
        WriteMemory(0xFF1A, 0x7F); // NR30
        WriteMemory(0xFF1B, 0xFF); // NR31
        WriteMemory(0xFF1C, 0x9F); // NR32
        WriteMemory(0xFF1E, 0xBF); // NR34
        
        WriteMemory(0xFF20, 0xFF); // NR41
        WriteMemory(0xFF21, 0x00); // NR42
        WriteMemory(0xFF22, 0x00); // NR43
        WriteMemory(0xFF23, 0xBF); // NR44
        
        WriteMemory(0xFF24, 0x77); // NR50
        WriteMemory(0xFF25, 0xF3); // NR51
        WriteMemory(0xFF26, 0xF1); // NR52 (0x85 for Game Boy Color)

        PPU.LCDC = 0x91;
        PPU.STAT = 0x85;
        PPU.LY = 0x90;
        PPU.LYC = 0x00;

        Timer.TIMA = 0x00;
        Timer.TMA = 0x00;
        Timer.TAC = 0x00;
        
        WriteMemory(0xFF47, 0xFC); // BGP (Background Palette)
        WriteMemory(0xFF48, 0xFF); // OBP0
        WriteMemory(0xFF49, 0xFF); // OBP1
        WriteMemory(0xFF4A, 0x00); // WY
        WriteMemory(0xFF4B, 0x00); // WX
        
        WriteMemory(0xFFFF, 0x00); // IE (Interrupt Enable)
    }
    
    public void LoadROM(string filename) {
        Cartridge = new Cartridge(this, filename);
    }
    public void LoadROM(params byte[] bytes) {
        byte[] rom_array = new byte[0x4000];

        for (int i = 0; i < bytes.Length; i++) {
            rom_array[0x0100  + i] = bytes[i]; 
        }
        
        Cartridge = new Cartridge(this, rom_array);
    }

    internal uint total_cycle = 0;
    private uint save_timer = 0;
    
    public void Tick() {
        if (CPU.Stopped) return;
        total_cycle++;
        
        Timer.Tick();
        serial.Tick();
        DMA.Tick();
        PPU.Tick();
        //APU.Tick();
        
        if (type == GameBoyType.Color && double_speed_mode) {
            CPU.Tick();
            CPU.Tick();
        } else {
            CPU.Tick();
        }

        save_timer++;
        if (save_timer > CLOCK_SPEED_HZ) {
            save_timer = 0;
            if (Cartridge.HasBattery && !SaveGame.CurrentlySaving) 
                Cartridge.Mapper.SaveRAM();
        }
    }

    public byte ReadMemory(ushort address) {
        switch (address) {
            // INTERRUPT REGISTERS
            case InterruptRegisterAddresses.IF: return (byte)(CPU.Registers.IF | 0xE0);
            case InterruptRegisterAddresses.IE: return CPU.Registers.IE;
            
            // JOYPAD REGISTER
            case Joypad.RegisterAddress: return joypad.ReadState();
            
            // SERIAL REGISTERS
            case SerialRegisterAddresses.SB: return serial.SB;
            case SerialRegisterAddresses.SC: return serial.SC;
            
            // PPU REGISTERS
            case PPURegisterAddresses.LCDC: return PPU.LCDC;
            
            case PPURegisterAddresses.LY: return PPU.LY;
            case PPURegisterAddresses.LYC: return PPU.LYC;
            
            case PPURegisterAddresses.SCY: return PPU.SCY;
            case PPURegisterAddresses.SCX: return PPU.SCX;
            
            case PPURegisterAddresses.STAT: return PPU.STAT;
            
            case PPURegisterAddresses.BGP: return PPU.BGP;
            case PPURegisterAddresses.OBP0: return PPU.OBP0;
            case PPURegisterAddresses.OBP1: return PPU.OBP1;
            
            case PPURegisterAddresses.WY: return PPU.WY;
            case PPURegisterAddresses.WX: return PPU.WX;
            
            case PPURegisterAddresses.DMA: return DMA.Register;
            
            // TIMER REGISTERS
            case TimerRegisterAddresses.DIV: return Timer.DIV;
            case TimerRegisterAddresses.TIMA: return Timer.TIMA;
            case TimerRegisterAddresses.TMA: return Timer.TMA;
            case TimerRegisterAddresses.TAC: return (byte)(Timer.TAC | 0xF8);

            // AUDIO REGISTERS
            case AudioRegisterAddresses.NR10: return APU.NR10;
            case AudioRegisterAddresses.NR11: return APU.NR11;
            case AudioRegisterAddresses.NR12: return APU.NR12;
            case AudioRegisterAddresses.NR13: return APU.NR13;
            case AudioRegisterAddresses.NR14: return APU.NR14;
            case AudioRegisterAddresses.NR21: return APU.NR21;
            case AudioRegisterAddresses.NR22: return APU.NR22;
            case AudioRegisterAddresses.NR23: return APU.NR23;
            case AudioRegisterAddresses.NR24: return APU.NR24;
            case AudioRegisterAddresses.NR30: return APU.NR30;
            case AudioRegisterAddresses.NR31: return APU.NR31;
            case AudioRegisterAddresses.NR32: return APU.NR32;
            case AudioRegisterAddresses.NR33: return APU.NR33;
            case AudioRegisterAddresses.NR34: return APU.NR34;
            case AudioRegisterAddresses.NR41: return APU.NR41;
            case AudioRegisterAddresses.NR42: return APU.NR42;
            case AudioRegisterAddresses.NR43: return APU.NR43;
            case AudioRegisterAddresses.NR44: return APU.NR44;
            case AudioRegisterAddresses.NR50: return APU.NR50;
            case AudioRegisterAddresses.NR51: return APU.NR51;
            case AudioRegisterAddresses.NR52: return APU.NR52;

        }
        
        return RAM.Read(address);
    }
    
    public void WriteMemory(ushort address, byte value) {
        switch (address) {
            // INTERRUPT REGISTERS
            case InterruptRegisterAddresses.IF: CPU.Registers.IF = (byte)(value & 0x1F); return;
            case InterruptRegisterAddresses.IE: CPU.Registers.IE = value; return;
            
            // JOYPAD REGISTER
            case Joypad.RegisterAddress: {
                joypad.select_dpad = ((value & 0x10) == 0);
                joypad.select_buttons = ((value & 0x20) == 0);
                return;
            }
            
            // SERIAL REGISTERS
            case SerialRegisterAddresses.SB: serial.SB = value; return;
            case SerialRegisterAddresses.SC: serial.SC = value; return;
            
            // PPU REGISTERS
            case PPURegisterAddresses.LCDC: {
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
            case PPURegisterAddresses.STAT:  PPU.STAT = value; return;
            
            case PPURegisterAddresses.SCY:  PPU.SCY = value; return;
            case PPURegisterAddresses.SCX:  PPU.SCX = value; return;
            
            case PPURegisterAddresses.LY:  PPU.LY = 0x00; return;
            case PPURegisterAddresses.LYC:  PPU.LYC = value; return;
            
            case PPURegisterAddresses.BGP:  PPU.BGP = value; return;
            case PPURegisterAddresses.OBP0:  PPU.OBP0 = value; return;
            case PPURegisterAddresses.OBP1:  PPU.OBP1 = value; return;
            
            case PPURegisterAddresses.WY:  PPU.WY = value; return;
            case PPURegisterAddresses.WX:  PPU.WX = value; return;
            
            case PPURegisterAddresses.DMA: DMA.Register = value; DMA.Start(value); return;
            
            // TIMER REGISTERS
            case TimerRegisterAddresses.DIV: { Timer.ResetDivider(); return; }
            case TimerRegisterAddresses.TIMA: { Timer.TIMA = value; return; }
            case TimerRegisterAddresses.TMA: { Timer.TMA = value; return; }
            case TimerRegisterAddresses.TAC: { Timer.WriteTAC(value); return; }

        
            // AUDIO REGISTERS
            case AudioRegisterAddresses.NR10: { APU.NR10 = value; return; }
            case AudioRegisterAddresses.NR11: { APU.NR11 = value; return; }
            case AudioRegisterAddresses.NR12: { APU.NR12 = value; return; }
            case AudioRegisterAddresses.NR13: { APU.NR13 = value; return; }
            case AudioRegisterAddresses.NR14: { APU.NR14 = value; return; }
    
            case AudioRegisterAddresses.NR21: { APU.NR21 = value; return; }
            case AudioRegisterAddresses.NR22: { APU.NR22 = value; return; }
            case AudioRegisterAddresses.NR23: { APU.NR23 = value; return; }
            case AudioRegisterAddresses.NR24: { APU.NR24 = value; return; }
    
            case AudioRegisterAddresses.NR30: { APU.NR30 = value; return; }
            case AudioRegisterAddresses.NR31: { APU.NR31 = value; return; }
            case AudioRegisterAddresses.NR32: { APU.NR32 = value; return; }
            case AudioRegisterAddresses.NR33: { APU.NR33 = value; return; }
            case AudioRegisterAddresses.NR34: { APU.NR34 = value; return; }
    
            case AudioRegisterAddresses.NR41: { APU.NR41 = value; return; }
            case AudioRegisterAddresses.NR42: { APU.NR42 = value; return; }
            case AudioRegisterAddresses.NR43: { APU.NR43 = value; return; }
            case AudioRegisterAddresses.NR44: { APU.NR44 = value; return; }
    
            case AudioRegisterAddresses.NR50: { APU.NR50 = value; return; }
            case AudioRegisterAddresses.NR51: { APU.NR51 = value; return; }
            case AudioRegisterAddresses.NR52: { APU.NR52 = value; return; }

        }
        
        RAM.Write(address, value);
    }
}