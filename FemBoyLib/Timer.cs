using System.Diagnostics;
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
    MemoryBus MemoryBus => gameboy.memory_bus;
    
    public Timer(GameBoy gameboy) => this.gameboy = gameboy;

    private bool TIMA_reload_pending_last_tick;
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
            if (TIMA_reload_pending)  return 0x00;
            return _TIMA;
        }
        set {
            if (!TIMA_reload_pending && !TIMA_reload_pending_last_tick) {
                _TIMA = value;
                return;
            }
            
            if (TIMA_reload_pending && TIMA_reload_pending_last_tick) {
                _TIMA = value;
                TIMA_reload_pending = false;
            }
        }
    }

    public byte TMA {
        get => _TMA;
        set {
            _TMA = value; 
            if (!TIMA_reload_pending && TIMA_reload_pending_last_tick) {
                _TIMA = _TMA;
            }
        }
    }

    public byte TAC = 0x00;

    public void DebugSetDivider(ushort value) {
        divider = value;
    }

    public void HandleBusR() {
        if (MemoryBus.BusState == RWState.Read) {
            switch (MemoryBus.Address) {
                case TimerRegisterAddresses.DIV:
                    MemoryBus.Data = DIV;
                    break;
                case TimerRegisterAddresses.TIMA:
                    MemoryBus.Data = TIMA;
                    break;
                case TimerRegisterAddresses.TMA:
                    MemoryBus.Data = TMA;
                    break;
                case TimerRegisterAddresses.TAC:
                    MemoryBus.Data = (byte)(TAC | 0xF8);
                    break;
            }
            MemoryBus.BusState = RWState.Idle;
        }
    }
    
    public void HandleBusW() {
        if (MemoryBus.BusState == RWState.Write) {
            switch (MemoryBus.Address) {
                case TimerRegisterAddresses.DIV:
                    ResetDivider();
                    break;
                case TimerRegisterAddresses.TIMA:
                    TIMA = MemoryBus.Data;
                    break;
                case TimerRegisterAddresses.TMA:
                    TMA = MemoryBus.Data;
                    break;
                case TimerRegisterAddresses.TAC:
                    WriteTAC(MemoryBus.Data);
                    break;
            }
            MemoryBus.BusState = RWState.Idle;
        }
    }
    
    public void Tick() {
        if (MemoryBus.Target == BusTarget.Timer) HandleBusW();
        
        bool old_timer_signal = GetTimerSignal();
        divider++;
        bool timer_signal = GetTimerSignal();
        if (old_timer_signal && !timer_signal) IncrementTIMA();

        TIMA_reload_pending_last_tick = TIMA_reload_pending;
        if (TIMA_reload_pending) {
            if (TIMA_reload_delay == 1) {
                CPU.RequestInterrupt(InterruptMask.Timer);
            }

            if (TIMA_reload_delay == 2) {
            }
            
            if (TIMA_reload_delay > 4) {
                _TIMA = _TMA; 
                TIMA_reload_pending = false;
                TIMA_reload_delay = 1;
            }
            
            TIMA_reload_delay++;
        }
        
        if (MemoryBus.Target == BusTarget.Timer) HandleBusR();
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
            TIMA_reload_delay = 1;
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
        return (((divider) >> bit) & 1) != 0;
    }
}


