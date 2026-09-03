namespace FemBoy;

public class RTC {
    public byte seconds;
    public byte minutes;
    public byte hours;
    public byte day;
    public byte control;
    
    public byte latch;
    
    public byte Read() {
        return 0xFF;
    }

    public void Write(byte value) {
        
    }
    
    public void Latch() {
        
    }
}