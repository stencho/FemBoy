namespace RatGBLib;

public enum TimerRegisterAddresses : ushort {
    DIV = 0xFF04,
    TIMA = 0xFF05,
    TMA = 0xFF06,
    TAC = 0xFF07
}

public class Timer {
    private GameBoy gameboy;
    
    public Timer(GameBoy gameboy) => this.gameboy = gameboy;
    
    private ushort divider = 0xABCC;
    public byte DIV => (byte)(divider >> 8);

    public byte TIMA = 0x00;
    public byte TMA = 0x00;
    public byte TAC = 0x00;

    private bool TIMA_reload_pending = false;
    public bool ReloadPending => TIMA_reload_pending;
    private int  TIMA_reload_delay = 4;
    
    public int ReloadDelay => TIMA_reload_delay;
    
    public void Execute() {
        if (TIMA_reload_pending) {
            if (TIMA_reload_delay == 0) {
                TIMA_reload_pending = false;
            } else {
                TIMA_reload_delay--;
            
                if (TIMA_reload_delay == 0) {
                    TIMA = TMA;
                    gameboy.RequestInterrupt(CPU.InterruptMask.Timer);
                }
            }
        }
    
        bool old_timer_signal = GetTimerSignal();
        divider++;
        bool timer_signal = GetTimerSignal();

        if (old_timer_signal && !timer_signal) IncrementTIMA();
    }

    public void CancelPendingTIMAReload(byte value) {
        TIMA = value;
        
        TIMA_reload_pending = false;
        TIMA_reload_delay = 0;
    }
    
    public uint last_tima_increment = 0;
    
    void IncrementTIMA() {
        //Console.WriteLine($"TIMA PERIOD = {gameboy.TotalCycles - last_tima_increment}");
        
        if (TIMA_reload_pending) {
            if (TIMA_reload_delay > 0) {
                TIMA_reload_pending = false;
                TIMA_reload_delay = 0;
                TIMA = 0;
                return;
            } else {
                TIMA = TMA;
                return;
            }
        }

        last_tima_increment = gameboy.TotalCycles;
        
        if (TIMA == 0xFF) {
            TIMA = 0x00;
            TIMA_reload_pending = true;
            TIMA_reload_delay = 4;
        } else TIMA++;
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