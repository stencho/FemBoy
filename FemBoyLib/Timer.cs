using System.Runtime.CompilerServices;

namespace FemBoy;

public static class TimerRegisterAddresses {
    public const ushort DIV = 0xFF04;
    public const ushort TIMA = 0xFF05;
    public const ushort TMA = 0xFF06;
    public const ushort TAC = 0xFF07;
}

public class Timer {
    private GameBoy gameboy;
    CPU CPU => gameboy.CPU;
    
    public Timer(GameBoy gameboy) => this.gameboy = gameboy;
    
    public bool ReloadPending => TIMA_reload_pending;
    private bool TIMA_reload_pending = false;
    private int TIMA_reload_delay = 0;

    private ushort divider = 0xABCC;
    public ushort Divider => divider;

    public byte DIV => (byte)(divider >> 8);
    
    private byte _TIMA = 0x00;

    private byte _TMA = 0x00;
    public byte TIMA {
        get {
            // Reads during Cycle 0, 1, and 2 return 0x00
            if (TIMA_reload_pending && TIMA_reload_delay < 3) {
                return 0x00;
            }
            return _TIMA;
        }
        set {
            if (TIMA_reload_pending) {
                // CPU can overwrite and cancel the reload up until the latch closes at index 3
                if (TIMA_reload_delay < 3) {
                    _TIMA = value;
                    TIMA_reload_pending = false;
                }
                return;
            }
            _TIMA = value;
        }
    }

    public byte TMA {
        get => _TMA;
        set {
            _TMA = value;
            // If TMA is written to on the exact cycle the value propagates to TIMA (Cycle 3)
            // the newly written byte must instantly become visible in TIMA
            if (TIMA_reload_pending && TIMA_reload_delay == 3) {
                _TIMA = _TMA;
            }
        }
    }

    public byte TAC = 0x00;

    public void DebugSetDivider(ushort value) {
        divider = value;
    }
    
    public void Tick() {
        bool old_timer_signal = GetTimerSignal();
        divider++;
        bool timer_signal = GetTimerSignal();

        
        if (old_timer_signal && !timer_signal) {
            IncrementTIMA();
        }
    
        if (TIMA_reload_pending) {
            TIMA_reload_delay++;
        
            // Phase 1: The Interrupt Flag triggers early!
            if (TIMA_reload_delay == 1) {
                CPU.RequestInterrupt(InterruptMask.Timer); // IF |= 0x04
            }
        
            // Phase 3: The Latch copies TMA over to TIMA and shuts down
            if (TIMA_reload_delay == 3) {
                _TIMA = _TMA;
            }
        
            // Phase 4: Reset the cycle tracking limits
            if (TIMA_reload_delay >= 4) {
                TIMA_reload_pending = false;
                TIMA_reload_delay = 0;
            }
        }
    }


    public void ResetDivider() {
        bool old = GetTimerSignal();
        divider = 0;
        if (old && !GetTimerSignal()) {
            IncrementTIMA();
        }
    }

    public void WriteTAC(byte value) {
        bool old = GetTimerSignal();
        TAC = (byte)(value & 0x07);
        if (old && !GetTimerSignal()) {
            IncrementTIMA();
        }
    }
    
    private void IncrementTIMA() {
        _TIMA++;
        if (_TIMA == 0x00) {
            TIMA_reload_pending = true;
            TIMA_reload_delay = 0;
        }
    }

    public bool GetTimerSignal() {
        if ((TAC & 0x04) == 0) return false;
        int bit = (TAC & 0x03) switch {
            0 => 9,  // Clock / 1024
            1 => 3,  // Clock / 16
            2 => 5,  // Clock / 64
            3 => 7,  // Clock / 256
            _ => 0
        };
        return (((divider+1) >> bit) & 1) == 1;
    }
}


