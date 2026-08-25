using System.Net;
using RatGBLib;

namespace RatGBLib;

public static class SerialRegisterAddresses {
    public const ushort SB = 0xFF01; 
    public const ushort SC = 0xFF02;
}

interface ISerialDevice {
    bool ClockBit(bool incoming_bit);
}

class NullDevice : ISerialDevice {
    public bool ClockBit(bool incoming_bit) {
        return true;
    }
}

public class Serial {
    private GameBoy gameboy;
    
    ISerialDevice connected_device = new NullDevice();

    private bool transfer_active = false;
    private int cycle_accumulator = 0;
    private int transferred_bits = 0;
    
    public byte SB = 0x00;
    
    private byte _SC = 0x00;
    public byte SC {
        get => (byte)(_SC | 0x7E);
        set {
            _SC = (byte)(value & 0x81);
            if ((_SC & 0x80) != 0 && (_SC & 0x01) != 0) 
                StartTransfer();
            
        }
    }
    
    public Serial(GameBoy gameboy) {
        this.gameboy = gameboy;
    }

    public void Execute() {
        if (!transfer_active) return;

        cycle_accumulator++;

        while (cycle_accumulator >= 512) {
            cycle_accumulator -= 512;

             Transfer();
        }
    }
    
    private void StartTransfer() {
        transfer_active = true;
        transferred_bits = 0;
        cycle_accumulator = 0;
    }
    
    void Transfer() {
        bool outgoing_bit = (SB & 0x80) != 0;
        bool incoming_bit = connected_device.ClockBit(outgoing_bit);
        
        SB = (byte)((SB << 1) | (incoming_bit ? (byte)1 : (byte)0));

        transferred_bits++;
        if (transferred_bits == 8) {
            transfer_active = false;
            _SC &= 0x7F;
            
            gameboy.RequestInterrupt(CPU.InterruptMask.Serial);
        }
    }

}