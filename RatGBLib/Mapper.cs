namespace RatGBLib.MBC;

public interface IMapper {
    public byte Read(ushort address);
    public void Write(ushort  address, byte value);
}
