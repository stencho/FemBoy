using System;

namespace FemBoy;

public static class TargetRegister {
    public const int A = 0; public const int B = 1; 
    public const int C = 2; public const int D = 3; 
    public const int E = 4; public const int H = 5; 
    public const int L = 6; public const int F = 7; 
    public const int SP = 8; public const int PC = 9; 
    public const int AF = 10; public const int BC = 11; 
    public const int DE = 12; public const int HL = 13;  
}

public enum CPUFlagMask : byte {
    Zero = 0x80,
    Negative = 0x40,
    HalfCarry = 0x20,
    Carry = 0x10
}

public class CPURegisters {
        
    public void SetFlag(CPUFlagMask mask, bool value) {
        if (value) _F |= (byte)mask;
        else _F &= (byte)~mask;
        _F &= 0xF0;
    }
        
    public bool GetFlag(CPUFlagMask mask) => (_F & (byte)mask) != 0;

    public Func<ushort>[]   Getters;
    public Action<ushort>[] Setters;

    private GameBoy gameboy;
    private CPU CPU => gameboy.CPU;
    
    public CPURegisters(GameBoy gameboy) {
        this.gameboy = gameboy;
        
        Getters = new Func<ushort>[14];
        Setters = new Action<ushort>[14];

        // 8bit getters
        Getters[TargetRegister.A] = () => A;
        Getters[TargetRegister.B] = () => B;
        Getters[TargetRegister.C] = () => C;
        Getters[TargetRegister.D] = () => D;
        Getters[TargetRegister.E] = () => E;
        Getters[TargetRegister.H] = () => H;
        Getters[TargetRegister.L] = () => L;
        Getters[TargetRegister.F] = () => F;
        
        // 16bit getters
        Getters[TargetRegister.BC] = () => BC;
        Getters[TargetRegister.DE] = () => DE;
        Getters[TargetRegister.HL] = () => HL;
        Getters[TargetRegister.AF] = () => AF;
        Getters[TargetRegister.SP] = () => SP;
        Getters[TargetRegister.PC] = () => PC;
        
        // 8bit setters
        Setters[TargetRegister.A] = (value) => A = (byte)value;
        Setters[TargetRegister.B] = (value) => B = (byte)value;
        Setters[TargetRegister.C] = (value) => C = (byte)value;
        Setters[TargetRegister.D] = (value) => D = (byte)value;
        Setters[TargetRegister.E] = (value) => E = (byte)value;
        Setters[TargetRegister.H] = (value) => H = (byte)value;
        Setters[TargetRegister.L] = (value) => L = (byte)value;
        Setters[TargetRegister.F] = (value) => F = (byte)value;
        
        // 16bit setters
        Setters[TargetRegister.BC] = (value) => BC = value;
        Setters[TargetRegister.DE] = (value) => DE = value;
        Setters[TargetRegister.HL] = (value) => HL = value;
        Setters[TargetRegister.AF] = (value) => AF = value;
        Setters[TargetRegister.SP] = (value) => SP = value;
        Setters[TargetRegister.PC] = (value) => PC = value;
    }
    
    public byte A, B, C, D, E, H, L;
        
    private byte _F;
    public byte F {
        get => _F;
        set => _F = (byte)(value & 0xF0);
    }
            
    public ushort AF {
        get => (ushort)((A << 8) | F);
        set {
            A = (byte)(value >> 8);
            F = (byte)value;
        }
    }
    
    public ushort BC {
        get => (ushort)((B << 8) | C);
        set {
            B = (byte)(value >> 8);
            C = (byte)value;
        }
    }
    public ushort DE {
        get => (ushort)((D << 8) | E);
        set {
            D = (byte)(value >> 8);
            E = (byte)value;
        }
    }
    public ushort HL {
        get => (ushort)((H << 8) | L);
        set {
            H = (byte)(value >> 8);
            L = (byte)value;
        }
    }
        
    public ushort SP = 0xFFFE;
    public ushort PC = 0x0100;

    public byte IE = 0x00;
    public byte IF = 0x00;
}
