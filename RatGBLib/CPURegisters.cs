namespace RatGBLib;

public class GBRegisters {
    [Flags]
    public enum Mask : byte {
        Zero = 0x80,
        Negative = 0x40,
        HalfCarry = 0x20,
        Carry = 0x10
    }
        
    public void SetFlag(Mask mask, bool value) {
        if (value) _F |= (byte)mask;
        else _F &= (byte)~mask;
        _F &= 0xF0;
    }
        
    public bool GetFlag(Mask mask) => (_F & (byte)mask) != 0;
        
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