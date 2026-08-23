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
    
    private ushort divider = 0;
    public byte DIV => (byte)(divider >> 8);

    public byte TIMA = 0x00;
    public byte TMA = 0x00;
    public byte TAC = 0x00;

    private bool TIMA_reload_pending = false;
    private int  TIMA_reload_delay = 0;
    
    public void Execute(int cycles) {
        for (int i = 0; i < cycles; i++) {
            bool old_timer_signal = GetTimerSignal();
            divider++;
            bool timer_signal = GetTimerSignal();

            if (old_timer_signal && !timer_signal) IncrementTIMA();
            
            if (TIMA_reload_pending) {
                
                TIMA_reload_delay--;
                if (TIMA_reload_delay == 0) {
                    
                    TIMA = TMA;
                    gameboy.RequestInterrupt((int)CPU.InterruptMask.Timer);
                    TIMA_reload_pending = false;
                    
                }
            }
            
        }
    }

    public void CancelPendingTIMAReload(byte value) {
        TIMA = value;
        
        TIMA_reload_pending = false;
        TIMA_reload_delay = 0;
    }
    
    void IncrementTIMA() {
        if (TIMA_reload_pending) return;
        
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
            _ => throw new IndexOutOfRangeException()
        };

        return ((divider >> bit) & 1) != 0;
    }
}