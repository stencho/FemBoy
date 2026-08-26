namespace RatGBLib.Mappers;

public interface IMapper {
    public string Name { get; }
    
    public byte Read(ushort address);
    public void Write(ushort  address, byte value);

    public void SaveRAM();
}
