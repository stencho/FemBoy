namespace RatGBLib.Mappers;

public interface IMapper {
    public byte Read(ushort address);
    public void Write(ushort  address, byte value);

    public void SaveRAM();
}
