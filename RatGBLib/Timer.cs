namespace RatGBLib;

public static class TimerRegisterAddresses {
    public const ushort DIV = 0xFF04;
    public const ushort TIMA = 0xFF05;
    public const ushort TMA = 0xFF06;
    public const ushort TAC = 0xFF07;
}

public class Timer {
    private GameBoy gameboy;
    
    public Timer(GameBoy gameboy) => this.gameboy = gameboy;
    
    public byte DIV => (byte)(divider >> 8);

    public byte TIMA = 0x00;
    public byte TMA = 0x00;
    public byte TAC = 0x00;

    private bool TIMA_reload_pending = false;
    public bool ReloadPending => TIMA_reload_pending;
    private int  TIMA_reload_delay = 4;
    
    public int ReloadDelay => TIMA_reload_delay;
    
    private ushort divider = 0xABCC;
    public ushort Divider => divider;
    
    public void Execute() {
        bool old_timer_signal = GetTimerSignal();
        divider++;
        bool timer_signal = GetTimerSignal();

        if (old_timer_signal && !timer_signal) IncrementTIMA();
        
        if (TIMA_reload_pending) {
            TIMA_reload_delay--;
            
            if (TIMA_reload_delay == 0) {
                TIMA = TMA;
                TIMA_reload_pending = false;
                gameboy.RequestInterrupt(CPU.InterruptMask.Timer);
            }
        }
    }

    public void CancelPendingTIMAReload(byte value) {
        TIMA = value;
        TIMA_reload_pending = false;
        TIMA_reload_delay = 0;
    }
    
    void IncrementTIMA() {
        if (TIMA == 0xFF) {
            TIMA = 0x00;
            TIMA_reload_pending = true;
            TIMA_reload_delay = 4;
            return;
        }
        
        TIMA++;
    }
    
    public void ResetDivider() {
        bool old = GetTimerSignal();
        divider = 0;
        bool signal = GetTimerSignal();
        if (old && !signal) IncrementTIMA();
    }
    
    public void WriteTAC(byte value) {
        bool old_signal = GetTimerSignal();
        TAC = (byte)(value & 0x07);
        bool new_signal = GetTimerSignal();
        if (old_signal && !new_signal) IncrementTIMA();
    }
    
    bool GetTimerSignal() {
        if ((TAC & 0x04) == 0) return false;
        
        int bit = (TAC & 0x03) switch {
            0 => 9,
            1 => 3,
            2 => 5,
            3 => 7,
            _ => 0
        };

        return ((divider >> bit) & 1) == 1;
    }
}


