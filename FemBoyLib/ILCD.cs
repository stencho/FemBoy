namespace FemBoy;

public interface ILCD {
    public void PushPixel(int X, int Y, byte color, byte attributes = 0);
    public void PresentFrame();
}