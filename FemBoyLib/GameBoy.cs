using System.Diagnostics;
using System.Text;
using FemBoy.Memory;

namespace FemBoy;

public enum GameBoyModel {
    Color, DotMatrix
}

public class GameBoy {
    public const int CYCLES_PER_FRAME = 70_224;
    public const uint CLOCK_SPEED_HZ = 4_194_304;
    public const int SCANLINES_PER_FRAME = 154;
    public const int VBLANK_SCANLINE = 144;
    public const int DOTS_PER_SCANLINE = 456;
    
    public GameBoyModel Model = GameBoyModel.DotMatrix;

    
    private bool double_speed_mode = false;
    
    public CPU CPU;
    public IMemory RAM;
    public PPU PPU;
    public DMA DMA;
    public Serial serial;
    public Timer Timer;
    public APU APU;
    
    public Joypad joypad;
    
    public Cartridge Cartridge;

    public Action<ushort, byte>? WriteMonitor;
    public Action<ushort>? ReadMonitor;
    
    public GameBoy(GameBoyModel model = GameBoyModel.DotMatrix) {
        Model = model;
        
        CPU = new CPU(this);
        PPU = new PPU(this);
        DMA = new DMA(this);
        RAM = new DotMatrixRAM(this);
        
        Timer = new Timer(this);
        serial = new Serial(this);
        joypad = new Joypad(this);
        APU = new APU(this);
        
        CPU.Registers.A = 0x01;
        CPU.Registers.F = 0xB0;
        CPU.Registers.B = 0x00;
        CPU.Registers.C = 0x13;
        CPU.Registers.D = 0x00;
        CPU.Registers.E = 0xD8;
        CPU.Registers.H = 0x01;
        CPU.Registers.L = 0x4D;
        
        Timer.TIMA = 0x00;
        Timer.TMA = 0x00;
        Timer.TAC = 0x00;
        
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
        
        if (Model == GameBoyModel.Color && CPU.Registers.DoubleSpeed) {
            CPU.Tick();
            Timer.Tick();
            serial.Tick();
            DMA.Tick();
            
            CPU.Tick();
            Timer.Tick();
            serial.Tick();
            DMA.Tick();
            
        } else {
            CPU.Tick();
            Timer.Tick();
            serial.Tick();
            DMA.Tick();
        }
        
        PPU.Tick();
        APU.Tick();
        
        total_cycle++;
        save_timer++;
        if (save_timer > CLOCK_SPEED_HZ) {
            save_timer = 0;
            if (Cartridge.HasBattery && !SaveGame.CurrentlySaving) 
                Cartridge.Mapper.SaveRAM();
        }
    }

    public byte ReadMemory(ushort address) {
        ReadMonitor?.Invoke(address);
        
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

            // KEY1
            case CPURegisterAddresses.KEY1: return CPU.Registers.KEY1;
            
            // AUDIO REGISTERS
            case >= 0xFF10 and <= 0xFF3F: return APU.Read(address);
        }
        
        return RAM.Read(address);
    }
    
    public void WriteMemory(ushort address, byte value) {
        WriteMonitor?.Invoke(address, value);
        
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
            
            case PPURegisterAddresses.DMA: DMA.Request(value); return;
            
            // TIMER REGISTERS
            case TimerRegisterAddresses.DIV: { Timer.ResetDivider(); return; }
            case TimerRegisterAddresses.TIMA: { Timer.TIMA = value; return; }
            case TimerRegisterAddresses.TMA: { Timer.TMA = value; return; }
            case TimerRegisterAddresses.TAC: { Timer.WriteTAC(value); return; }
            
            // KEY1
            case CPURegisterAddresses.KEY1: CPU.Registers.KEY1 = value; return;
            
            // AUDIO REGISTERS
            case >= 0xFF10 and <= 0xFF3F: { APU.Write(address, value); return; }

        }
        
        RAM.Write(address, value);
    }
}