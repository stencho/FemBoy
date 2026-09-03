using System;

namespace FemBoy;

public class DotMatrixLCD : ILCD {
    const int TOTAL_PIXELS = 160 * 144;
    
    private readonly byte[] frame_buffer = new byte[TOTAL_PIXELS];
    private readonly byte[] offscreen_buffer = new byte[TOTAL_PIXELS];
    
    public void PushPixel(int X, int Y, byte color, byte attributes = 0) {
        
    }

    public void PresentFrame() {
        Array.Copy(offscreen_buffer, 0, frame_buffer, 0, TOTAL_PIXELS);
    }
}