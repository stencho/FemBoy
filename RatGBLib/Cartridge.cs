using System.Runtime.Intrinsics.Arm;
using RatGBLib.Mappers;
using RatGBLib.Mappers.MBC;
using Crc32 = System.IO.Hashing.Crc32;

namespace RatGBLib;

public class Cartridge {
    public byte[] ROM;
    public uint ROMCRC32;
    
    public byte cartridge_type => ROM[0x147];
    public byte ROM_size_code => ROM[0x148];
    public byte RAM_size_code => ROM[0x149];
    
    private IMapper mapper;
    public IMapper Mapper => mapper;

    private bool _has_battery = false;
    public bool HasBattery => _has_battery;

    private bool _has_RAM = false;
    public bool HasRAM => _has_RAM;
    
    public Cartridge(string file_name) {
        using (FileStream file = new(file_name, FileMode.Open)) {
            ROM = new byte[file.Length];
            file.ReadExactly(ROM, 0, ROM.Length);
        }

        ROMCRC32 = Crc32.HashToUInt32(ROM.AsSpan());
        
        switch (cartridge_type) {
            case 0x00: case 0x08: case 0x09:
                mapper = new NoMapper(this);
                break;

            case 0x01: case 0x02: case 0x03:
                _has_RAM = cartridge_type != 0x01;
                _has_battery = cartridge_type == 0x03;
                
                mapper = new MBC1(this, _has_battery);
                break;
            
            /*
            case 0x05: case 0x06:
                mapper = new MBC2(this);
                break;

            
            case 0x0F: case 0x10: case 0x11: case 0x12: case 0x13:
                mapper = new MBC3(this);
                break;
            */
            default:
                throw new NotImplementedException($"Cartridge type not implemented: {cartridge_type:X2}");

            /*
            case 0x19: case 0x1A: case 0x1B: case 0x1C: case 0x1D: case 0x1E:
                //mapper = new MBC5(this);
                break;

            case 0x20:
                //mapper = new MBC6(this);
                break;

            case 0x22:
                //mapper = new MBC7(this);
                break;

            case 0x0B: case 0x0C: case 0x0D:
                //mapper = new MMM01(this);
                break;

            case 0xFC:
                //mapper = new PocketCamera(this);
                break;
            
            case 0xFD:
                //mapper = new TAMA5(this);
                break;
            
            case 0xFE:
                //mapper = new HuC3(this);
                break;
            
            case 0xFF:
                //mapper = new HuC1(this);
                break;
            */
        }

    }
    
    public byte Read(ushort address) {
        return mapper.Read(address);
    }

    public void Write(ushort address, byte value) {
        mapper.Write(address, value);
    }
}