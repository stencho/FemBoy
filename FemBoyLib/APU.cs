namespace FemBoy;

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

public static class LocalAudioRegisterAddresses {
    public const byte NR10 = 0x00;
    public const byte NR11 = 0x01;
    public const byte NR12 = 0x02;
    public const byte NR13 = 0x03;
    public const byte NR14 = 0x04;
    
    public const byte NR21 = 0x06;
    public const byte NR22 = 0x07;
    public const byte NR23 = 0x08;
    public const byte NR24 = 0x09;
    
    public const byte NR30 = 0x0A;
    public const byte NR31 = 0x0B;
    public const byte NR32 = 0x0C;
    public const byte NR33 = 0x0D;
    public const byte NR34 = 0x0E;
    
    public const byte NR41 = 0x10;
    public const byte NR42 = 0x11;
    public const byte NR43 = 0x12;
    public const byte NR44 = 0x13;
    
    public const byte NR50 = 0x14;
    public const byte NR51 = 0x15;
    public const byte NR52 = 0x16;
}

public class AudioChannel {
    public bool Active { get; set; } = false;
    public int length { get; set; }= 0;

    public virtual void Trigger(byte value) {
        Active = true;

        length = 64 - (value & 0x3F);
        if (length == 0) length = 64;
    }

    public void clock_length(bool enabled) {
        if (!enabled || length == 0) return;
        length--;
        if (length == 0) Active = false;
    }
}

public class SquareChannel : AudioChannel {
    
    
    public void Trigger(byte value) {
        base.Trigger(value);
    }
}

public class WaveChannel : AudioChannel {
    public void Trigger(byte value) {
        Active = true;
        length = 256 - value;
        if (length == 0) length = 256;
    }
}
public class NoiseChannel : AudioChannel {
    public void Trigger(byte value) {
        base.Trigger(value);
    }
}

public class APU {
    private GameBoy gameboy;

    public readonly byte[] registers = [
        0x80, 0xBF, 0xF3, 0xFF, 0xBF, // channel 1
        0xFF,                         // UNUSED
        0x3F, 0x00, 0xFF, 0xBF,       // channel 2
        0x7F, 0xFF, 0x9F, 0x00, 0xBF, // channel 3
        0xFF,                         // UNUSED
        0xFF, 0x00, 0x00, 0xBF,       // channel 4
        0x77, 0xF3, 0xF1
    ];
    
    public readonly byte[] wave_ram = new byte[0x10];
    
    public APU(GameBoy gameboy) {
        this.gameboy = gameboy;

        enabled = true;
        
        square1.Active = true;
        square2.Active = false;
        wave.Active = false;
        noise.Active = false;
    }


    private bool enabled = true;
    public float volume = 1.0f;

    public readonly SquareChannel square1 = new();
    public readonly SquareChannel square2 = new();
    public readonly WaveChannel wave = new();
    public readonly NoiseChannel noise = new();

    void APUOn() {
        enabled = true;
    }
    void APUOff() {
        enabled = false;
        
        square1.Active = false;
        square2.Active = false;
        wave.Active = false;
        noise.Active = false;
        
        Array.Clear(registers, 0x00, 0x17);
    }

    private int counter = 0;
    private int step = 0;
    
    public void Tick() {
        if (!enabled) return;

        counter++;

        if (counter < 8192) return;
        counter = 0;
        
        step = (step + 1) & 7;
        
        if ((step & 1) == 0) {
            square1.clock_length((registers[LocalAudioRegisterAddresses.NR14] & 0x40) != 0);
            square2.clock_length((registers[LocalAudioRegisterAddresses.NR24] & 0x40) != 0);
            wave.clock_length((registers[LocalAudioRegisterAddresses.NR34] & 0x40) != 0);
            noise.clock_length((registers[LocalAudioRegisterAddresses.NR44] & 0x40) != 0);
        }

        if (step == 2 || step == 6) {
            //square1.clock_sweep();
        }

        if (step == 7) {
            //square1.clock_envelope();
            //square2.clock_envelope();
            //noise.clock_envelope();
        }

    }

    public byte Read(ushort address) {
        if (address is >= 0xFF27 and <= 0xFF2F) return 0xFF; // UNUSED
        
        if (address >= 0xFF30) return wave_ram[(ushort)(address - 0xFF30)];
        
        if (address == AudioRegisterAddresses.NR52) { // special handling for NR52/AUDENA
            byte value = 0x70;

            if (enabled)        value |= 0x80;
            if (square1.Active) value |= 0x01;
            if (square2.Active) value |= 0x02;
            if (wave.Active)    value |= 0x04;
            if (noise.Active)   value |= 0x08;

            return value;
        }
        
        return (byte)(registers[address - 0xFF10] | ReadMask(address));
    }
    
    public void Write(ushort address, byte value) {

        if (address is >= 0xFF27 and <= 0xFF2F) return; // UNUSED
        
        if (address == AudioRegisterAddresses.NR52) {
            bool turn_on = (value & 0x80) != 0;
            
            if (!enabled && turn_on) APUOn();
            if (enabled && !turn_on) APUOff();
            
            return;
        }

        if (address >= 0xFF30) {
            wave_ram[address - 0xFF30] = value;
            return;
        }

        if (!enabled) return;

        byte masked_value = (byte)(value & WriteMask(address));
        registers[address - 0xFF10] = masked_value;

        switch (address) {
            case AudioRegisterAddresses.NR14 when (value & 0x80) != 0: 
                square1.Trigger(registers[LocalAudioRegisterAddresses.NR11]); 
                break;
            
            case AudioRegisterAddresses.NR24 when (value & 0x80) != 0: 
                square2.Trigger(registers[LocalAudioRegisterAddresses.NR21]); 
                break;
            
            case AudioRegisterAddresses.NR34 when (value & 0x80) != 0: 
                wave.Trigger(registers[LocalAudioRegisterAddresses.NR31]); 
                break;
            
            case AudioRegisterAddresses.NR44 when (value & 0x80) != 0: 
                noise.Trigger(registers[LocalAudioRegisterAddresses.NR41]); 
                break;
        }

    }
    
    private static byte ReadMask(ushort address) {
        return address switch {
            AudioRegisterAddresses.NR10 => 0x80,
            
            AudioRegisterAddresses.NR11 => 0x3F, AudioRegisterAddresses.NR12 => 0x00, 
            AudioRegisterAddresses.NR13 => 0xFF, AudioRegisterAddresses.NR14 => 0xBF,
            
            AudioRegisterAddresses.NR21 => 0x3F, AudioRegisterAddresses.NR22 => 0x00,
            AudioRegisterAddresses.NR23 => 0xFF, AudioRegisterAddresses.NR24 => 0xBF,
            
            AudioRegisterAddresses.NR30 => 0x7F,
            
            AudioRegisterAddresses.NR31 => 0xFF, AudioRegisterAddresses.NR32 => 0x9F,
            AudioRegisterAddresses.NR33 => 0xFF, AudioRegisterAddresses.NR34 => 0xBF,
            
            AudioRegisterAddresses.NR41 => 0xC0, AudioRegisterAddresses.NR42 => 0x00,
            AudioRegisterAddresses.NR43 => 0x00, AudioRegisterAddresses.NR44 => 0xBF,
            
            AudioRegisterAddresses.NR50 => 0x00, AudioRegisterAddresses.NR51 => 0x00,

            _ => 0xFF
        };
    }
    
    private static byte WriteMask(ushort address) {
        return address switch {
            AudioRegisterAddresses.NR10 => 0x7F,
            
            AudioRegisterAddresses.NR11 => 0xFF, 
            AudioRegisterAddresses.NR12 => 0xFF,
            AudioRegisterAddresses.NR13 => 0xFF, 
            AudioRegisterAddresses.NR14 => 0xC7,
            
            AudioRegisterAddresses.NR21 => 0xFF, 
            AudioRegisterAddresses.NR22 => 0xFF,
            AudioRegisterAddresses.NR23 => 0xFF, 
            AudioRegisterAddresses.NR24 => 0xC7,
            
            AudioRegisterAddresses.NR30 => 0x80,
            
            AudioRegisterAddresses.NR31 => 0xFF, 
            AudioRegisterAddresses.NR32 => 0x60,
            AudioRegisterAddresses.NR33 => 0xFF, 
            AudioRegisterAddresses.NR34 => 0xC7,
            
            AudioRegisterAddresses.NR41 => 0x3F, 
            AudioRegisterAddresses.NR42 => 0xFF,
            AudioRegisterAddresses.NR43 => 0xFF, 
            AudioRegisterAddresses.NR44 => 0xC7,
            
            AudioRegisterAddresses.NR50 => 0xFF, 
            AudioRegisterAddresses.NR51 => 0xFF,

            _ => 0x00
        };
    }
}