namespace FemBoy;

public enum RWState { Read, Write, Idle }

public enum MemoryBusDriver { CPU, DMA }
public enum VideoBusDriver { CPU, PPU }

public enum BusTarget {
    PPU, Timer, Serial, DMA, Joypad, APU, Memory, CPURegister
}

public enum SelectedBus { Memory, Video, HRAMSideChannel, OpenBus }

public interface IBus {
    public ushort Address { get; set; }
    public byte Data { get; set; }
    
    public RWState BusState { get; set; }
    public BusTarget Target { get; set; }
    
    public BusTarget FindBusTarget();
    public void Tick();
}

public class HRAMSideChannel {
    public ushort Address { get; set; }
    public byte Data { get; set; }
    public RWState BusState { get; set; } = RWState.Idle;
}

public class MemoryBus : IBus {
    private GameBoy gameboy;
    
    public MemoryBus(GameBoy gameboy) => this.gameboy = gameboy;

    public HRAMSideChannel hram_side_channel = new HRAMSideChannel();
    
    public ushort Address { get; set; }
    public byte Data { get; set; }
    
    public BusTarget Target { get; set; }
    public RWState BusState { get; set; }
    
    public MemoryBusDriver Driver { get; set; } = MemoryBusDriver.CPU;
    
    public BusTarget FindBusTarget() {
        switch (Address) {
            case SerialRegisterAddresses.SB or SerialRegisterAddresses.SC: return BusTarget.Serial; 
            case >= TimerRegisterAddresses.DIV and <= TimerRegisterAddresses.TAC: return BusTarget.Timer; 
            case Joypad.RegisterAddress: return BusTarget.Joypad;
            case PPURegisterAddresses.DMA: return BusTarget.DMA;
            case >= PPURegisterAddresses.LCDC and <= PPURegisterAddresses.WX: return BusTarget.PPU;
            case InterruptRegisterAddresses.IE or InterruptRegisterAddresses.IF or CPURegisterAddresses.KEY1: 
                return BusTarget.CPURegister;
            case >= 0xFF10 and <= 0xFF3F: return BusTarget.APU;
            default: return BusTarget.Memory;
        }
    }
    
    public void Tick() {
        if (hram_side_channel.BusState != RWState.Idle) {
            if (hram_side_channel.BusState == RWState.Read) {
                hram_side_channel.Data = gameboy.ReadMemory(hram_side_channel.Address);
            } else {
                gameboy.WriteMemory(hram_side_channel.Address, hram_side_channel.Data);
            }
            
            hram_side_channel.BusState = RWState.Idle;
        }
        
        if (BusState == RWState.Idle || Target == BusTarget.Timer) return;
        
        switch (Target) {
            case BusTarget.PPU:
                gameboy.PPU.HandleBusRW();
                break;
            case BusTarget.Timer:
                //gameboy.Timer.HandleBusRW();
                break;
            case BusTarget.Serial:
                gameboy.serial.HandleBusRW();
                break;
            case BusTarget.DMA:
                gameboy.DMA.HandleBusRW();
                break;
            case BusTarget.Joypad:
                gameboy.joypad.HandleBusRW();
                break;
            case BusTarget.APU:
                gameboy.APU.HandleBusRW();
                break;
            case BusTarget.CPURegister:
                switch (Address) {
                    case InterruptRegisterAddresses.IE:
                        if (BusState == RWState.Read) Data = gameboy.CPU.Registers.IE;
                         else if (BusState == RWState.Write) gameboy.CPU.Registers.IE = Data;
                        break;
                    
                    case InterruptRegisterAddresses.IF:
                        if (BusState == RWState.Read) Data = (byte)(gameboy.CPU.Registers.IF | 0xE0);
                        else if (BusState == RWState.Write) gameboy.CPU.Registers.IF =  (byte)(Data & 0x1F);
                        break;
                    
                    case CPURegisterAddresses.KEY1:
                        if (BusState == RWState.Read) Data = gameboy.CPU.Registers.KEY1;
                        else if (BusState == RWState.Write) gameboy.CPU.Registers.KEY1 = Data;
                        break;
                }
                break;
            case BusTarget.Memory:
                gameboy.RAM.HandleBusRW();
                break;
        }

        BusState = RWState.Idle;
    }
}

public class VideoBus : IBus {
    private GameBoy gameboy;
    
    public VideoBus(GameBoy gameboy) => this.gameboy = gameboy;
    
    public ushort Address { get; set; }
    public byte Data { get; set; }
    
    public BusTarget Target { get; set; }
    public RWState BusState { get; set; }
    
    public VideoBusDriver Driver { get; set; } = VideoBusDriver.CPU;

    public BusTarget FindBusTarget() { return BusTarget.Memory; }
    
    public void Tick() {
        if (BusState == RWState.Idle) return;
        
        gameboy.RAM.HandleVideoBusRW();
        
        BusState = RWState.Idle;
    }
}

