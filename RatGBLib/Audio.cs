namespace RatGBLib;

public static class AudioRegisterAddresses {
    public const ushort NR10 = 0xFF10;
    public const ushort NR11 = 0xFF11;
    public const ushort NR12 = 0xFF12;
    public const ushort NR13 = 0xFF13;
    public const ushort NR14 = 0xFF14;
    
    public const ushort NR21 = 0xFF16;
    public const ushort NR22 = 0xFF17;
    public const ushort NR23 = 0xFF18;
    public const ushort NR24 = 0xFF19;
    
    public const ushort NR30 = 0xFF1A;
    public const ushort NR31 = 0xFF1B;
    public const ushort NR32 = 0xFF1C;
    public const ushort NR33 = 0xFF1D;
    public const ushort NR34 = 0xFF1E;
    
    public const ushort NR41 = 0xFF20;
    public const ushort NR42 = 0xFF21;
    public const ushort NR43 = 0xFF22;
    public const ushort NR44 = 0xFF23;
    
    public const ushort NR50 = 0xFF24;
    public const ushort NR51 = 0xFF25;
    public const ushort NR52 = 0xFF26;
}

public class Audio {
    private GameBoy gameboy;

    public Audio(GameBoy gameboy) => this.gameboy = gameboy;
    
    // Channel 1
    
    private byte _NR10;
    public byte NR10
    {
        get => (byte)(_NR10 | 0x80);
        set => _NR10 = (byte)(value & 0x7F);
    }
    
    private byte _NR11;
    public byte NR11 {
        get => (byte)(_NR11 | 0x3F);
        set => _NR11 = value;
    }

    private byte _NR12;
    public byte NR12 {
        get => _NR12;
        set => _NR12 = value;
    }

    private byte _NR13;
    public byte NR13 {
        get => 0xFF;
        set => _NR13 = value;
    }

    private byte _NR14;
    public byte NR14 {
        get => (byte)(_NR14 | 0xBF);
        set => _NR14 = (byte)(value & 0xC7);
    }


    // Channel 2

    private byte _NR21;
    public byte NR21 {
        get => (byte)(_NR21 | 0x3F);
        set => _NR21 = value;
    }

    private byte _NR22;
    public byte NR22 {
        get => _NR22;
        set => _NR22 = value;
    }

    private byte _NR23;
    public byte NR23 {
        get => 0xFF;
        set => _NR23 = value;
    }

    private byte _NR24;
    public byte NR24 {
        get => (byte)(_NR24 | 0xBF);
        set => _NR24 = (byte)(value & 0xC7);
    }


    // Channel 3

    private byte _NR30;
    public byte NR30 {
        get => (byte)(_NR30 | 0x7F);
        set => _NR30 = (byte)(value & 0x80);
    }

    private byte _NR31;
    public byte NR31 {
        get => 0xFF;
        set => _NR31 = value;
    }

    private byte _NR32;
    public byte NR32 {
        get => (byte)(_NR32 | 0x9F);
        set => _NR32 = (byte)(value & 0x60);
    }

    private byte _NR33;
    public byte NR33 {
        get => 0xFF;
        set => _NR33 = value;
    }

    private byte _NR34;
    public byte NR34 {
        get => (byte)(_NR34 | 0xBF);
        set => _NR34 = (byte)(value & 0xC7);
    }


    // Channel 4

    private byte _NR41;
    public byte NR41 {
        get => 0xFF;
        set => _NR41 = value;
    }

    private byte _NR42;
    public byte NR42 {
        get => _NR42;
        set => _NR42 = value;
    }

    private byte _NR43;
    public byte NR43 {
        get => _NR43;
        set => _NR43 = value;
    }

    private byte _NR44;
    public byte NR44 {
        get => (byte)(_NR44 | 0xBF);
        set => _NR44 = (byte)(value & 0xC0);
    }


    // Control

    private byte _NR50;
    public byte NR50 {
        get => _NR50;
        set => _NR50 = value;
    }

    private byte _NR51;
    public byte NR51 {
        get => _NR51;
        set => _NR51 = value;
    }

    private byte _NR52;
    public byte NR52 {
        get => (byte)(_NR52 | 0x70);
        set => _NR52 = (byte)(value & 0x80);
    }
    
}