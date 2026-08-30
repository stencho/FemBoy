using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;

namespace RatGBLib;
using static GameBoy;

public static class InterruptRegisterAddresses {
    public const ushort IF = 0xFF0F;
    public const ushort IE = 0xFFFF;
}

public class CPU {
    public GBRegisters REGISTERS = new GBRegisters();
    
    public bool HALTED = false;
    private bool HALT_BUG = false;
    public bool STOPPED = false;
    
    // INTERRUPTS
    public bool INTERRUPT_MASTER_ENABLE = false;
    public int ENABLE_INTERRUPT_DELAY = 0;

    [Flags]
    public enum InterruptMask : byte {
        VBlank = 0x01,
        LCD = 0x02,
        Timer = 0x04,
        Serial = 0x08,
        Joypad = 0x10
    }
    
    public bool InterruptRequested(InterruptMask interrupt) => (REGISTERS.IE & REGISTERS.IF & (byte)interrupt) != 0;
    
    public bool InterruptPending => (REGISTERS.IE & REGISTERS.IF & 0x1F) != 0;
    
    public byte InterruptFlags {
        get => (byte)(REGISTERS.IF | 0xE0);
        set => REGISTERS.IF = (byte)(value & 0x1F);
    }
    
    private void Increment(ref byte register) {
        byte pre_increment = register;
        register++;

        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, register == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, (pre_increment & 0x0F) == 0x0F);
    }
    
    private void Decrement(ref byte register) {
        byte pre_decrement = register;
        register--;

        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, register == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, true);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, (pre_decrement & 0x0F) == 0);
    }
    
    private void IncrementAtAddress(GameBoy gameboy, ushort address) {
        byte pre_increment = ReadByte(address);
        byte result = (byte)(pre_increment + 1);

        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, (pre_increment & 0x0F) == 0x0F);
        
        WriteByte(address, result);
    }
    
    private void DecrementAtAddress(GameBoy gameboy, ushort address) {
        byte pre_decrement = ReadByte(address);
        byte result = (byte)(pre_decrement - 1);

        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, true);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, (pre_decrement & 0x0F) == 0);
        
        WriteByte(address, result);
    }

    private ushort Add(ushort register_a, ushort register_b) {
        ushort a = register_a;
        ushort b = register_b;

        int result = a + b;
                
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, ((a & 0x0FFF) + (b & 0x0FFF)) > 0x0FFF);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Carry, result > 0xFFFF);

        return (ushort)result;
    }

    private void Add(ref byte register_a, byte register_b) {
        byte a = register_a;
        byte b = register_b;

        int result = a + b;
                
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, (byte)result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, ((a & 0x0F) + (b & 0x0F)) > 0x0F);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Carry, result > 0xFF);

        register_a = (byte)result;
    }
    
    private void AddWithCarry(ref byte register_a, byte register_b) {
        bool carry = gameboy.CPU.REGISTERS.GetFlag(GBRegisters.Mask.Carry);
        
        byte a = register_a;
        byte b = register_b;

        int result = a + b + (carry ? 1 : 0);
                
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, (byte)result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, ((a & 0x0F) + (b & 0x0F) + (carry ? 1 : 0)) > 0x0F);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Carry, result > 0xFF);

        register_a = (byte)result;
    }
    
    private void Subtract(ref byte register_a, byte register_b) {
        byte a = register_a;
        byte b = register_b;

        int result = a - b;
                
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, (byte)result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, true);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, (a & 0x0F) < (b & 0x0F));
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Carry, result < 0x00);

        register_a = (byte)result;
    }
    
    private void SubtractWithCarry(ref byte register_a, byte register_b) {
        bool carry = gameboy.CPU.REGISTERS.GetFlag(GBRegisters.Mask.Carry);
        
        byte a = register_a;
        byte b = register_b;

        int result = a - b - (carry ? 1 : 0);
                
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, (byte)result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, true);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, (a & 0x0F) < (b & 0x0F) + (carry ? 1 : 0));
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Carry, result < 0x00);

        register_a = (byte)result;
    }

    private void And(ref byte register_a, byte register_b) {
        byte a = register_a;
        byte b = register_b;

        byte result = (byte)(a & b);
        
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, true);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Carry, false);

        register_a = result;
    }
    
    private void Xor(ref byte register_a, byte register_b) {
        byte a = register_a;
        byte b = register_b;

        byte result = (byte)(a ^ b);
        
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Carry, false);

        register_a = result;
    }
    
    private void Or(ref byte register_a, byte register_b) {
        byte a = register_a;
        byte b = register_b;

        byte result = (byte)(a | b);
        
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Carry, false);

        register_a = result;
    }

    private void Compare(byte register_a, byte register_b) {
        int result = register_a - register_b;
                
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, (byte)result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, true);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, (register_a & 0x0F) < (register_b & 0x0F));
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Carry, result < 0x00);

        register_a = (byte)result;
    }
    
    public void PushU16(ref ushort SP, ushort value) {
        gameboy.Tick(4);
/*
        ushort hi_addr = (ushort)(SP - 1);
        WriteByte(hi_addr, (byte)(value >> 8));
        
        ushort lo_addr = (ushort)(SP - 2);
        WriteByte(lo_addr, (byte)value);

        SP -= 2;
*/
        SP--;
        WriteByte(SP, (byte)(value >> 8));

        SP--;
        WriteByte(SP, (byte)value);
        
    }
    
    public ushort PopU16(ref ushort SP) {
        byte lo = ReadByte(SP);
        byte hi = ReadByte((ushort)(SP + 1));
        ushort value = (ushort)(lo | (hi << 8));
        
        SP += 2;
        return value;
    }

    public byte ReadByte(ushort address) {
        byte value = 0;
        
        Tick(4);
        
        // CPU-ONLY ACCESS RESTRICTIONS
        if (gameboy.PPU.LCDEnabled &&
            address >= 0xFE00 &&
            address <= 0xFE9F &&
            (gameboy.PPU.mode == STATMode.OAM_SEARCH ||
             gameboy.PPU.mode == STATMode.LCD_TRANSFER)) {
            value = 0xFF;
        } 
        else if (address is >= 0x8000 and <= 0x9FFF && (gameboy.PPU.mode == STATMode.LCD_TRANSFER)) { return 0xFF; }
        else if (gameboy.DMA.Active && (address < 0xFF00 || address >= 0xFFFE)) value = 0xFF;
        else value = gameboy.ReadByte(address);
        
        //Tick(1);
        return value;
    } 

    public void WriteByte(ushort address, byte value) {
        Tick(2);
        gameboy.WriteByte(address, value);
        Tick(2);
    }
    
    public ushort ReadU16(ref ushort address) {
        byte lo = ReadByte(address++);
        byte hi = ReadByte(address++);
        return (ushort)((hi << 8) | lo);
    }
    
    public ushort ReadU16NoTick(ushort address) {
        byte lo = gameboy.ReadByte(address);
        byte hi = gameboy.ReadByte((ushort)(address+1));
        return (ushort)((hi << 8) | lo);
    }
    
    private byte FetchOpcode() {
        byte op = ReadByte(REGISTERS.PC);

        if (HALT_BUG) HALT_BUG = false;
        else REGISTERS.PC++;
        
        return op;
    }

    void Tick(int cycles) => gameboy.Tick(cycles);
    
    private GameBoy gameboy;
    public CPU(GameBoy gameboy) => this.gameboy = gameboy;
    
    private void ServiceInterrupt() {
        Tick(4);

        InterruptMask interrupt;
        if      (InterruptRequested(InterruptMask.VBlank)) interrupt = InterruptMask.VBlank;
        else if (InterruptRequested(InterruptMask.LCD)) interrupt = InterruptMask.LCD;
        else if (InterruptRequested(InterruptMask.Timer)) interrupt = InterruptMask.Timer;
        else if (InterruptRequested(InterruptMask.Serial)) interrupt = InterruptMask.Serial;
        else if (InterruptRequested(InterruptMask.Joypad)) interrupt = InterruptMask.Joypad;
        else {
            REGISTERS.PC = 0x0000;
            INTERRUPT_MASTER_ENABLE = false;
            Tick(4);
            return;
        }
        
        INTERRUPT_MASTER_ENABLE = false;
        REGISTERS.IF &= (byte)~(byte)interrupt;

        
        Tick(4);
        
        REGISTERS.SP--;
        WriteByte(REGISTERS.SP, (byte)(REGISTERS.PC >> 8));

        REGISTERS.SP--;
        WriteByte(REGISTERS.SP, (byte)REGISTERS.PC);
    
        REGISTERS.PC = interrupt switch {
            InterruptMask.VBlank => 0x0040,
            InterruptMask.LCD    => 0x0048,
            InterruptMask.Timer  => 0x0050,
            InterruptMask.Serial => 0x0058,
            InterruptMask.Joypad => 0x0060,
            _                    => 0x0000
        };
        
        Tick(4);
    }

    public ConcurrentQueue<OpcodeInfo> LastNOpcodes = new();
    private int track_n_opcodes = 80;
    public bool track_opcodes = false;
    private uint last_op_total_cycles = 0;
    private uint cycles_since_last_op = 0;
    
    public void Execute() {
        if (STOPPED) {
            Tick(4);
            return;
        }

        if (HALTED) {
            if (InterruptPending) {
                HALTED = false;
                if (INTERRUPT_MASTER_ENABLE) Tick(4);
            } else {
                Tick(4);
                return; 
            }
        }
        

        
        ushort current_PC = REGISTERS.PC;
        byte opcode = FetchOpcode();
        
        OpcodeInfo current_op = new OpcodeInfo(opcode);
        
        current_op.PC = current_PC;
        current_op.SP_before = REGISTERS.SP;

        if (track_opcodes) {
            LastNOpcodes.Enqueue(current_op);
            if (LastNOpcodes.Count > track_n_opcodes) LastNOpcodes.TryDequeue(out _);
        }

        switch (opcode) {
            // -------- 0x0x --------
            
            case 0x00: break; // NOP

            case 0x01: { // LD BC, ushort
                ushort value = ReadU16(ref REGISTERS.PC);
                REGISTERS.BC = value;
                current_op.operand_one = (byte)value;
                current_op.operand_two = (byte)(value >> 8);
                break;
            }
            case 0x02: // LD (BC), A
                WriteByte(REGISTERS.BC, REGISTERS.A);
                break;
            
            case 0x03: // INC BC
                REGISTERS.BC++;    
                Tick(4);
                break;

            case 0x04: //INC B
                Increment(ref REGISTERS.B);
                break;
            
            case 0x05: // DEC B
                Decrement(ref REGISTERS.B);
                break;
            
            case 0x06: // LD B, (byte)
                REGISTERS.B = ReadByte(REGISTERS.PC++);
                current_op.operand_one = REGISTERS.B;
                break;

            case 0x07: { // RLCA 
                bool carry = (REGISTERS.A & 0x80) != 0;
                REGISTERS.A = (byte)((REGISTERS.A << 1) | (carry ? 1 : 0));

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, carry);
                break;
            }

            case 0x08: { // LD ushort, SP
                ushort value = ReadU16(ref REGISTERS.PC);
                WriteByte(value, (byte)REGISTERS.SP);
                WriteByte((ushort)(value + 1), (byte)(REGISTERS.SP >> 8));
                
                current_op.operand_one = (byte)value;
                current_op.operand_two = (byte)(value >> 8);
                break;
            }

            case 0x09: // ADD HL, BC
                REGISTERS.HL = Add(REGISTERS.HL, REGISTERS.BC);
                Tick(4);
                break;
            
            case 0x0A: // LD A, (BC)
                REGISTERS.A = ReadByte(REGISTERS.BC);
                break;
            
            case 0x0B: // DEC BC
                REGISTERS.BC--;
                Tick(4);
                break;
            
            case 0x0C: // INC C
                Increment(ref REGISTERS.C);
                break;
            
            case 0x0D: // DEC C
                Decrement(ref REGISTERS.C);
                break;
            
            case 0x0E: // LD C, byte
                REGISTERS.C = ReadByte(REGISTERS.PC++);
                current_op.operand_one = REGISTERS.C;
                break;
            
            case 0x0F: { // RRCA 
                bool carry = (REGISTERS.A & 0x01) != 0;
                REGISTERS.A = (byte)((REGISTERS.A >> 1) | (carry ? 0x80 : 0));

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, carry);

                break;
            }
            
            // -------- 0x1x -------- 
            
            case 0x10: // STOP
                REGISTERS.PC++;
                STOPPED = true;
                break;

            case 0x11: { // LD DE, ushort
                ushort value = ReadU16(ref REGISTERS.PC);
                REGISTERS.DE = value;
                current_op.operand_one = (byte)value;
                current_op.operand_two = (byte)(value >> 8);
                break;
            }

            case 0x12: // LD (DE), A
                WriteByte(REGISTERS.DE, REGISTERS.A);
                break;
            
            case 0x13: // INC DE
                REGISTERS.DE++;
                Tick(4);
                break;
            
            case 0x14: // INC D
                Increment(ref REGISTERS.D);
                break;
            
            case 0x15: // DEC D
                Decrement(ref REGISTERS.D);
                break;
            
            case 0x16: // LD D, byte
                REGISTERS.D = ReadByte(REGISTERS.PC++);
                current_op.operand_one = REGISTERS.D;
                break;
            
            case 0x17: { // RLA 
                bool old_carry = REGISTERS.GetFlag(GBRegisters.Mask.Carry);
                bool carry = (REGISTERS.A & 0x80) != 0;
                REGISTERS.A = (byte)((REGISTERS.A << 1) | (old_carry ? 1 : 0));

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, carry);
                break;
            }

            case 0x18: { // JR byte
                sbyte offset = unchecked((sbyte)ReadByte(REGISTERS.PC++));
                current_op.operand_one = (byte)offset;
                REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                Tick(4);
                break;
            }
            
            case 0x19: // ADD HL, DE
                REGISTERS.HL = Add(REGISTERS.HL, REGISTERS.DE);
                Tick(4);
                break;
            
            case 0x1A: // LD A, (DE)
                REGISTERS.A = ReadByte(REGISTERS.DE);
                break;
            
            case 0x1B: // DEC DE
                REGISTERS.DE--;
                Tick(4);
                break;
            
            case 0x1C: // INC E
                Increment(ref REGISTERS.E);
                break;
            
            case 0x1D: // DEC E
                Decrement(ref REGISTERS.E);
                break;
            
            case 0x1E: // LD E, byte
                REGISTERS.E = ReadByte(REGISTERS.PC++);
                current_op.operand_one = REGISTERS.E;
                break;
            
            case 0x1F: { // RRA 
                bool old_carry = REGISTERS.GetFlag(GBRegisters.Mask.Carry);
                bool carry = (REGISTERS.A & 0x01) != 0;
                
                REGISTERS.A = (byte)((REGISTERS.A >> 1) | (old_carry ? 0x80 : 0));

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, carry);

                break;
            }
            
            // -------- 0x2x --------
            
            case 0x20: { // JR NZ, byte
                sbyte offset = unchecked((sbyte)ReadByte(REGISTERS.PC++));
                
                current_op.operand_one = (byte)offset;
                
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                    Tick(4);
                }

                break;
            }

            case 0x21: { // LD HL, ushort
                ushort value = ReadU16(ref REGISTERS.PC);
                REGISTERS.HL = value;
                
                current_op.operand_one = (byte)value;
                current_op.operand_two = (byte)(value >> 8);
                break;
                }
            
            case 0x22: // LD (HL+), A
                WriteByte(REGISTERS.HL++, REGISTERS.A);
                break;
            
            case 0x23: // INC HL
                REGISTERS.HL++;
                Tick(4);
                break;
            
            case 0x24: // INC H
                Increment(ref REGISTERS.H);
                break;
            
            case 0x25: // DEC H
                Decrement(ref REGISTERS.H);
                break;
            
            case 0x26: // LD H, byte
                REGISTERS.H = ReadByte(REGISTERS.PC++);
                current_op.operand_one = REGISTERS.H;
                break;

            case 0x27: { // DAA
                bool previous_op_subtract = REGISTERS.GetFlag(GBRegisters.Mask.Negative);
                bool half_carry = REGISTERS.GetFlag(GBRegisters.Mask.HalfCarry);
                bool carry = REGISTERS.GetFlag(GBRegisters.Mask.Carry);

                byte A = REGISTERS.A;
                
                if (!previous_op_subtract) {
                    if (carry || A > 0x99) {
                        A += 0x60;
                        REGISTERS.SetFlag(GBRegisters.Mask.Carry, true);
                    }
                    
                    if (half_carry || (A & 0x0F) > 9) A += 0x06;
                    
                } else {
                    if (half_carry) A -= 0x06;
                    if (carry) A -= 0x60;
                }
                
                REGISTERS.SetFlag(GBRegisters.Mask.Zero, A == 0);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                
                REGISTERS.A = A;

                break;
            }
            
            case 0x28: { // JR Z, byte
                sbyte offset = unchecked((sbyte)ReadByte(REGISTERS.PC++));
                current_op.operand_one = (byte)offset;
                
                if (REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                    Tick(4);
                }

                break;
            }

            case 0x29: // ADD HL, HL
                REGISTERS.HL = Add(REGISTERS.HL, REGISTERS.HL);
                Tick(4);
                break;
            
            case 0x2A: // LD A, (HL+)
                REGISTERS.A = ReadByte(REGISTERS.HL++);
                break;
            
            case 0x2B: // DEC HL
                REGISTERS.HL--;
                Tick(4);
                break;
            
            case 0x2C: // INC L
                Increment(ref REGISTERS.L);
                break;
            
            case 0x2D: // DEC L
                Decrement(ref REGISTERS.L);
                break;
            
            case 0x2E: // LD L, byte
                REGISTERS.L = ReadByte(REGISTERS.PC++);
                current_op.operand_one = REGISTERS.E;
                break;
            
            case 0x2F: // CPL
                REGISTERS.A = (byte)~REGISTERS.A;
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, true);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, true);
                break;
            
            // -------- 0x3x --------
            
            case 0x30: { // JR NC, byte
                sbyte offset = unchecked((sbyte)ReadByte(REGISTERS.PC++));
                current_op.operand_one = (byte)offset;
                
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                    Tick(4);
                }

                break;
            }
            
            case 0x31: { // LD SP, ushort
                ushort value = ReadU16(ref REGISTERS.PC);
                REGISTERS.SP = value;
                current_op.operand_one = (byte)value;
                current_op.operand_two = (byte)(value >> 8);
                break;
                }
            
            case 0x32: // LD (HL-), A
                WriteByte(REGISTERS.HL--, REGISTERS.A);
                break;
            
            case 0x33: // INC SP
                REGISTERS.SP++;
                Tick(4);
                break;
            
            case 0x34: // INC (HL)
                IncrementAtAddress(gameboy, REGISTERS.HL);
                break;
            
            case 0x35: // DEC (HL)
                DecrementAtAddress(gameboy, REGISTERS.HL);
                break;
            
            case 0x36: { // LD (HL), byte
                byte value = ReadByte(REGISTERS.PC++); 
                current_op.operand_one = value;
                WriteByte(REGISTERS.HL, value);
                break;
            }
                
            case 0x37: // SCF
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, true);
                break;
            
            case 0x38: { // JR C, byte
                sbyte offset = unchecked((sbyte)ReadByte(REGISTERS.PC++));
                current_op.operand_one = (byte)offset;
                if (REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                    Tick(4);
                }

                break;
            }
            
            case 0x39: // ADD HL, SP
                REGISTERS.HL = Add(REGISTERS.HL, REGISTERS.SP);
                Tick(4);
                break;
            
            case 0x3A: // LD A, (HL-)
                REGISTERS.A = ReadByte(REGISTERS.HL--);
                break;
            
            case 0x3B: // DEC SP
                REGISTERS.SP--;
                Tick(4);
                break;
            
            case 0x3C: // INC A
                Increment(ref REGISTERS.A);
                break;
            
            case 0x3D: // DEC A
                Decrement(ref REGISTERS.A);
                break;
            
            case 0x3E: // LD A, byte
                REGISTERS.A = ReadByte(REGISTERS.PC++);
                current_op.operand_one = REGISTERS.E;
                break;
            
            case 0x3F: // CCF
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, !REGISTERS.GetFlag(GBRegisters.Mask.Carry));
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                break;
            
            // -------- 0x4x --------
            
            case 0x40: REGISTERS.B = REGISTERS.B; break; // LD B, B
            case 0x41: REGISTERS.B = REGISTERS.C; break; // LD B, C
            case 0x42: REGISTERS.B = REGISTERS.D; break; // LD B, D
            case 0x43: REGISTERS.B = REGISTERS.E; break; // LD B, E
            case 0x44: REGISTERS.B = REGISTERS.H; break; // LD B, H
            case 0x45: REGISTERS.B = REGISTERS.L; break; // LD B, L
            case 0x47: REGISTERS.B = REGISTERS.A; break; // LD B, A
            case 0x46: REGISTERS.B = ReadByte(REGISTERS.HL); break; // LD B, (HL)
            
            case 0x48: REGISTERS.C = REGISTERS.B; break; // LD C, B
            case 0x49: REGISTERS.C = REGISTERS.C; break; // LD C, C
            case 0x4A: REGISTERS.C = REGISTERS.D; break; // LD C, D
            case 0x4B: REGISTERS.C = REGISTERS.E; break; // LD C, E
            case 0x4C: REGISTERS.C = REGISTERS.H; break; // LD C, H
            case 0x4D: REGISTERS.C = REGISTERS.L; break; // LD C, L
            case 0x4F: REGISTERS.C = REGISTERS.A; break; // LD C, A
            case 0x4E: REGISTERS.C = ReadByte(REGISTERS.HL); break; // LD C, (HL)
            
            // -------- 0x5x --------
            
            case 0x50: REGISTERS.D = REGISTERS.B; break; // LD D, B
            case 0x51: REGISTERS.D = REGISTERS.C; break; // LD D, C
            case 0x52: REGISTERS.D = REGISTERS.D; break; // LD D, D
            case 0x53: REGISTERS.D = REGISTERS.E; break; // LD D, E
            case 0x54: REGISTERS.D = REGISTERS.H; break; // LD D, H
            case 0x55: REGISTERS.D = REGISTERS.L; break; // LD D, L
            case 0x57: REGISTERS.D = REGISTERS.A; break; // LD D, A
            case 0x56: REGISTERS.D = ReadByte(REGISTERS.HL); break; // LD D, (HL)
            
            case 0x58: REGISTERS.E = REGISTERS.B; break; // LD E, B
            case 0x59: REGISTERS.E = REGISTERS.C; break; // LD E, C
            case 0x5A: REGISTERS.E = REGISTERS.D; break; // LD E, D
            case 0x5B: REGISTERS.E = REGISTERS.E; break; // LD E, E
            case 0x5C: REGISTERS.E = REGISTERS.H; break; // LD E, H
            case 0x5D: REGISTERS.E = REGISTERS.L; break; // LD E, L
            case 0x5F: REGISTERS.E = REGISTERS.A; break; // LD E, A
            case 0x5E: REGISTERS.E = ReadByte(REGISTERS.HL); break; // LD E, (HL)
            
            // -------- 0x6x --------
            
            case 0x60: REGISTERS.H = REGISTERS.B; break; // LD H, B
            case 0x61: REGISTERS.H = REGISTERS.C; break; // LD H, C
            case 0x62: REGISTERS.H = REGISTERS.D; break; // LD H, D
            case 0x63: REGISTERS.H = REGISTERS.E; break; // LD H, E
            case 0x64: REGISTERS.H = REGISTERS.H; break; // LD H, H
            case 0x65: REGISTERS.H = REGISTERS.L; break; // LD H, L
            case 0x67: REGISTERS.H = REGISTERS.A; break; // LD H, A
            case 0x66: REGISTERS.H = ReadByte(REGISTERS.HL); break; // LD H, (HL)
            
            case 0x68: REGISTERS.L = REGISTERS.B; break; // LD L, B
            case 0x69: REGISTERS.L = REGISTERS.C; break; // LD L, C
            case 0x6A: REGISTERS.L = REGISTERS.D; break; // LD L, D
            case 0x6B: REGISTERS.L = REGISTERS.E; break; // LD L, E
            case 0x6C: REGISTERS.L = REGISTERS.H; break; // LD L, H
            case 0x6D: REGISTERS.L = REGISTERS.L; break; // LD L, L
            case 0x6F: REGISTERS.L = REGISTERS.A; break; // LD L, A
            case 0x6E: REGISTERS.L = ReadByte(REGISTERS.HL); break; // LD L, (HL)
            
            // -------- 0x7x --------
            
            case 0x70: WriteByte(REGISTERS.HL, REGISTERS.B); break; // LD (HL), B
            case 0x71: WriteByte(REGISTERS.HL, REGISTERS.C); break; // LD (HL), C
            case 0x72: WriteByte(REGISTERS.HL, REGISTERS.D); break; // LD (HL), D
            case 0x73: WriteByte(REGISTERS.HL, REGISTERS.E); break; // LD (HL), E
            case 0x74: WriteByte(REGISTERS.HL, REGISTERS.H); break; // LD (HL), H
            case 0x75: WriteByte(REGISTERS.HL, REGISTERS.L); break; // LD (HL), L
            
            case 0x76: // HALT
                if (!INTERRUPT_MASTER_ENABLE) {
                    if (InterruptPending) {
                        HALTED = false; 
                        HALT_BUG = true; 
                    } else {
                        HALTED = true;
                    }
                } else {
                    HALTED = true;
                }
                break;
            
            case 0x77: WriteByte(REGISTERS.HL, REGISTERS.A); break; // LD (HL), A
            
            case 0x78: REGISTERS.A = REGISTERS.B; break; // LD A, B
            case 0x79: REGISTERS.A = REGISTERS.C; break; // LD A, C
            case 0x7A: REGISTERS.A = REGISTERS.D; break; // LD A, D
            case 0x7B: REGISTERS.A = REGISTERS.E; break; // LD A, E
            case 0x7C: REGISTERS.A = REGISTERS.H; break; // LD A, H
            case 0x7D: REGISTERS.A = REGISTERS.L; break; // LD A, L
            case 0x7F: REGISTERS.A = REGISTERS.A; break; // LD A, A
            case 0x7E: REGISTERS.A = ReadByte(REGISTERS.HL); break; // LD A, (HL)
            
            // -------- 0x8x --------
            
            case 0x80: Add(ref REGISTERS.A, REGISTERS.B); break; // ADD A, B
            case 0x81: Add(ref REGISTERS.A, REGISTERS.C); break; // ADD A, C
            case 0x82: Add(ref REGISTERS.A, REGISTERS.D); break; // ADD A, D
            case 0x83: Add(ref REGISTERS.A, REGISTERS.E); break; // ADD A, E
            case 0x84: Add(ref REGISTERS.A, REGISTERS.H); break; // ADD A, H
            case 0x85: Add(ref REGISTERS.A, REGISTERS.L); break; // ADD A, L
            case 0x87: Add(ref REGISTERS.A, REGISTERS.A); break; // ADD A, A
            case 0x86: Add(ref REGISTERS.A, ReadByte(REGISTERS.HL)); break; // ADD A, (HL)
            
            case 0x88: AddWithCarry(ref REGISTERS.A, REGISTERS.B); break; // ADC A, B
            case 0x89: AddWithCarry(ref REGISTERS.A, REGISTERS.C); break; // ADC A, C
            case 0x8A: AddWithCarry(ref REGISTERS.A, REGISTERS.D); break; // ADC A, D
            case 0x8B: AddWithCarry(ref REGISTERS.A, REGISTERS.E); break; // ADC A, E
            case 0x8C: AddWithCarry(ref REGISTERS.A, REGISTERS.H); break; // ADC A, H
            case 0x8D: AddWithCarry(ref REGISTERS.A, REGISTERS.L); break; // ADC A, L
            case 0x8F: AddWithCarry(ref REGISTERS.A, REGISTERS.A); break; // ADC A, A
            case 0x8E: AddWithCarry(ref REGISTERS.A, ReadByte(REGISTERS.HL)); break; // ADC A, (HL)
            
            // -------- 0x9x --------
            
            case 0x90: Subtract(ref REGISTERS.A, REGISTERS.B); break;                   // SUB A, B
            case 0x91: Subtract(ref REGISTERS.A, REGISTERS.C); break;          // SUB A, C
            case 0x92: Subtract(ref REGISTERS.A, REGISTERS.D); break;          // SUB A, D
            case 0x93: Subtract(ref REGISTERS.A, REGISTERS.E); break;          // SUB A, E
            case 0x94: Subtract(ref REGISTERS.A, REGISTERS.H); break;          // SUB A, H
            case 0x95: Subtract(ref REGISTERS.A, REGISTERS.L); break;          // SUB A, L
            case 0x97: Subtract(ref REGISTERS.A, REGISTERS.A); break;          // SUB A, A
            case 0x96: Subtract(ref REGISTERS.A, ReadByte(REGISTERS.HL)); break; // SUB A, (HL)
            
            case 0x98: SubtractWithCarry(ref REGISTERS.A, REGISTERS.B); break;          // SUB A, B
            case 0x99: SubtractWithCarry(ref REGISTERS.A, REGISTERS.C); break; // SBC A, C
            case 0x9A: SubtractWithCarry(ref REGISTERS.A, REGISTERS.D); break; // SBC A, D
            case 0x9B: SubtractWithCarry(ref REGISTERS.A, REGISTERS.E); break; // SBC A, E
            case 0x9C: SubtractWithCarry(ref REGISTERS.A, REGISTERS.H); break; // SBC A, H
            case 0x9D: SubtractWithCarry(ref REGISTERS.A, REGISTERS.L); break; // SBC A, L
            case 0x9F: SubtractWithCarry(ref REGISTERS.A, REGISTERS.A); break; // SBC A, A
            case 0x9E: SubtractWithCarry(ref REGISTERS.A, ReadByte(REGISTERS.HL)); break; // SBC A, (HL)
            
            // -------- 0xAx --------
            
            case 0xA0: And(ref REGISTERS.A, REGISTERS.B); break;                   // AND A, B
            case 0xA1: And(ref REGISTERS.A, REGISTERS.C); break;          // AND A, C
            case 0xA2: And(ref REGISTERS.A, REGISTERS.D); break;          // AND A, D
            case 0xA3: And(ref REGISTERS.A, REGISTERS.E); break;          // AND A, E
            case 0xA4: And(ref REGISTERS.A, REGISTERS.H); break;          // AND A, H
            case 0xA5: And(ref REGISTERS.A, REGISTERS.L); break;          // AND A, L
            case 0xA7: And(ref REGISTERS.A, REGISTERS.A); break;          // AND A, A
            case 0xA6: And(ref REGISTERS.A, ReadByte(REGISTERS.HL)); break; // AND A, (HL)
            
            case 0xA8: Xor(ref REGISTERS.A, REGISTERS.B); break;          // XOR A, B
            case 0xA9: Xor(ref REGISTERS.A, REGISTERS.C); break; // XOR A, C
            case 0xAA: Xor(ref REGISTERS.A, REGISTERS.D); break; // XOR A, D
            case 0xAB: Xor(ref REGISTERS.A, REGISTERS.E); break; // XOR A, E
            case 0xAC: Xor(ref REGISTERS.A, REGISTERS.H); break; // XOR A, H
            case 0xAD: Xor(ref REGISTERS.A, REGISTERS.L); break; // XOR A, L
            case 0xAF: Xor(ref REGISTERS.A, REGISTERS.A); break; // XOR A, A
            case 0xAE: Xor(ref REGISTERS.A, ReadByte(REGISTERS.HL)); break; // XOR A, (HL)
            
            // -------- 0xBx --------
            
            case 0xB0: Or(ref REGISTERS.A, REGISTERS.B); break;                   // OR A, B
            case 0xB1: Or(ref REGISTERS.A, REGISTERS.C); break;          // OR A, C
            case 0xB2: Or(ref REGISTERS.A, REGISTERS.D); break;          // OR A, D
            case 0xB3: Or(ref REGISTERS.A, REGISTERS.E); break;          // OR A, E
            case 0xB4: Or(ref REGISTERS.A, REGISTERS.H); break;          // OR A, H
            case 0xB5: Or(ref REGISTERS.A, REGISTERS.L); break;          // OR A, L
            case 0xB7: Or(ref REGISTERS.A, REGISTERS.A); break;          // OR A, A
            case 0xB6: Or(ref REGISTERS.A, ReadByte(REGISTERS.HL)); break; // OR A, (HL)
            
            case 0xB8: Compare(REGISTERS.A, REGISTERS.B); break; // CP B
            case 0xB9: Compare(REGISTERS.A, REGISTERS.C); break; // CP C
            case 0xBA: Compare(REGISTERS.A, REGISTERS.D); break; // CP D
            case 0xBB: Compare(REGISTERS.A, REGISTERS.E); break; // CP E
            case 0xBC: Compare(REGISTERS.A, REGISTERS.H); break; // CP H
            case 0xBD: Compare(REGISTERS.A, REGISTERS.L); break; // CP L
            case 0xBF: Compare(REGISTERS.A, REGISTERS.A); break; // CP A
            case 0xBE: Compare(REGISTERS.A, ReadByte(REGISTERS.HL)); break; // CP (HL)
            
            // -------- 0xCx --------

            case 0xC0: // RET NZ
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                Tick(4);
                
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    ushort value = PopU16(ref REGISTERS.SP);
                    REGISTERS.PC = value;
                    Tick(4);
                    current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                }
                current_op.SP_after = REGISTERS.SP;
                break;
            
            case 0xC1: { // POP BC
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                ushort value = PopU16(ref REGISTERS.SP);
                REGISTERS.BC = value;
                
                current_op.SP_after = REGISTERS.SP;
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                break;
            }
            
            case 0xC2: { // JP NZ, ushort
                ushort address = ReadU16(ref REGISTERS.PC);
                
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = address;
                    Tick(4);
                }

                break;
            }
            
            case 0xC3: { // JP ushort
                ushort value = ReadU16(ref REGISTERS.PC);
                REGISTERS.PC = value;
                current_op.operand_one = (byte)(value >> 8);
                current_op.operand_two = (byte)value;
                Tick(4);
                break;
            }
            
            case 0xC4: { // CALL NZ, ushort
                ushort address = ReadU16(ref REGISTERS.PC);
                
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    PushU16(ref REGISTERS.SP, REGISTERS.PC);
                    REGISTERS.PC = address;
                    current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                }
                
                current_op.SP_after = REGISTERS.SP;
                break;
            }
            
            case 0xC5: // PUSH BC
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                PushU16( ref REGISTERS.SP, REGISTERS.BC);
                //Tick(4);
                
                current_op.SP_after = REGISTERS.SP;
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                break;
            
            case 0xC6: { // ADD A, byte
                byte value = ReadByte(REGISTERS.PC++);
                current_op.operand_one = value;
                Add(ref REGISTERS.A, value);
                break;
                }
            
            case 0xC7: // RST 00h
                PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0000;
                break;
            
            case 0xC8: // RET Z
                Tick(4);
                
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                if (REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = PopU16(ref REGISTERS.SP);
                    Tick(4);
                    current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                }
                
                current_op.SP_after = REGISTERS.SP;
                break;
            
            case 0xC9: // RET
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                Tick(4);
                REGISTERS.PC = PopU16(ref REGISTERS.SP);
                current_op.SP_after = REGISTERS.SP;
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                break;
            
            case 0xCA: { // JP Z, ushort
                ushort address = ReadU16(ref REGISTERS.PC);
                
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                
                if (REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = address;
                    Tick(4);
                }

                break;
            }

            case 0xCB: { // CB PREFIX
                var operand = ReadByte(REGISTERS.PC++);
                current_op.operand_one = operand;
                CBTable(operand);
                break;
            }
                
            case 0xCC: { // CALL Z, ushort
                ushort address = ReadU16(ref REGISTERS.PC);
                
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                if (REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    PushU16( ref REGISTERS.SP, REGISTERS.PC);
                    REGISTERS.PC = address;
                    current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                }
                
                current_op.SP_after = REGISTERS.SP;
                break;
            }
            
            case 0xCD: { // CALL ushort
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                ushort address = ReadU16(ref REGISTERS.PC);
                
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                
                PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = address;
                
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                current_op.SP_after = REGISTERS.SP;
                break;
            }
            
            case 0xCE: { // ADC A, byte
                byte value = ReadByte(REGISTERS.PC++);
                current_op.operand_one = value;
                AddWithCarry(ref REGISTERS.A, value);
                break;
            }

            case 0xCF: // RST 08h
                PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0008;
                break;
            
            // -------- 0xDx --------
            
            case 0xD0: { // RET NC
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                Tick(4);
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = PopU16(ref REGISTERS.SP);
                    Tick(4);
                    current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                }
                
                current_op.SP_after = REGISTERS.SP;
                break;
            }
            
            case 0xD1: { // POP DE
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                ushort value = PopU16(ref REGISTERS.SP);
                REGISTERS.DE = value;
                
                current_op.SP_after = REGISTERS.SP;
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                break;
            }
            
            case 0xD2: { // JP NC, ushort
                ushort address = ReadU16(ref REGISTERS.PC);
                
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = address;
                    Tick(4);
                }

                break;
            }
            
            // 0xD3 UNUSED
            
            case 0xD4: { // CALL NC, ushort
                ushort address = ReadU16(ref REGISTERS.PC);
                
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    PushU16( ref REGISTERS.SP, REGISTERS.PC);
                    REGISTERS.PC = address;
                    current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                }

                current_op.SP_after = REGISTERS.SP;
                break;
            }
            
            case 0xD5: // PUSH DE
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                PushU16( ref REGISTERS.SP, REGISTERS.DE);
                //Tick(4);
                
                current_op.SP_after = REGISTERS.SP;
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                break;
            
            case 0xD6: { // SUB A, byte
                byte value = ReadByte(REGISTERS.PC++);
                current_op.operand_one = value;
                Subtract(ref REGISTERS.A, value);
                break;
                }
            
            case 0xD7: // RST 10h
                PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0010;
                break;
            
            case 0xD8: // RET C
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                Tick(4);
                
                if (REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = PopU16(ref REGISTERS.SP);
                    Tick(4);
                    current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                }
                
                current_op.SP_after = REGISTERS.SP;
                break;
            
            case 0xD9: // RETI
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                Tick(4);
                REGISTERS.PC = PopU16(ref REGISTERS.SP);
                INTERRUPT_MASTER_ENABLE = true;
                ENABLE_INTERRUPT_DELAY = 0;
                
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                current_op.SP_after = REGISTERS.SP;
                break;
            
            case 0xDA: { // JP C, ushort
                ushort address = ReadU16(ref REGISTERS.PC);
                
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                
                if (REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = address;
                    Tick(4);
                }

                break;
            }
            
            // 0xDB UNUSED
            
            case 0xDC: { // CALL C, ushort
                ushort address = ReadU16(ref REGISTERS.PC);
                
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                if (REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    PushU16( ref REGISTERS.SP, REGISTERS.PC);
                    REGISTERS.PC = address;
                    current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                }
                
                current_op.SP_after = REGISTERS.SP;
                break;
            }
            
            // 0xDD UNUSED
            
            case 0xDE: { // SBC A, byte
                byte value = ReadByte(REGISTERS.PC++);
                current_op.operand_one = value;
                SubtractWithCarry(ref REGISTERS.A, value);
                break;
            }
            
            case 0xDF: // RST 18h
                PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0018;
                break;
            
            // -------- 0xEx --------

            case 0xE0: { // LD (FF00 + byte), A
                byte offset = ReadByte(REGISTERS.PC++);
                current_op.operand_one = offset;
                WriteByte((ushort)(0xFF00 + offset), REGISTERS.A);
                break;
            }

            case 0xE1: { // POP HL
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                ushort value = PopU16(ref REGISTERS.SP);
                REGISTERS.HL = value;
                
                current_op.SP_after = REGISTERS.SP;
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                break;
            }
            
            case 0xE2: { // LD (FF00 + C), A
                WriteByte((ushort)(0xFF00 + REGISTERS.C), REGISTERS.A);
                break;
            }
            
            // 0xE3 UNUSED
            
            // 0xE4 UNUSED
            
            case 0xE5: // PUSH HL
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                PushU16( ref REGISTERS.SP, REGISTERS.HL);
                //Tick(4);
                
                current_op.SP_after = REGISTERS.SP;
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                break;
            
            case 0xE6: { // AND A, byte
                byte value = ReadByte(REGISTERS.PC++);
                current_op.operand_one = value;
                And(ref REGISTERS.A, value);
                break;
                }
            
            case 0xE7: // RST 20h
                PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0020;
                break;

            case 0xE8: { // ADD SP, byte
                ushort a = REGISTERS.SP;
                sbyte b = unchecked((sbyte)ReadByte(REGISTERS.PC++));
                
                current_op.operand_one = (byte)b;
                
                Tick(8);

                int result = a + b;
                
                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, ((a & 0x0F) + (b & 0x0F)) > 0x0F);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, ((a & 0xFF) + (b & 0xFF)) > 0xFF);

                REGISTERS.SP = (ushort)result;
                
                break;
            }
            
            case 0xE9: // JP HL
                REGISTERS.PC = REGISTERS.HL;
                break;
            
            case 0xEA: { // LD (ushort), A
                ushort address = ReadU16(ref REGISTERS.PC);
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                WriteByte(address, REGISTERS.A);
                break;
            }
            
            // 0xEB UNUSED
            // 0xEC UNUSED
            // 0xED UNUSED
                
            case 0xEE: { // XOR A, byte
                byte value = ReadByte(REGISTERS.PC++);
                current_op.operand_one = value;
                Xor(ref REGISTERS.A, value);
                break;
            }
            
            case 0xEF: // RST 28h
                PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0028;
                break;
            
            // -------- 0xFx --------
            
            case 0xF0: { // LD A, (FF00 + byte)
                byte value = ReadByte(REGISTERS.PC++);
                var addr = (ushort)(0xFF00 + value);
                current_op.operand_one = value;
                REGISTERS.A = ReadByte(addr);
                break;
            }
            
            case 0xF1: { // POP AF
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                ushort value = PopU16(ref REGISTERS.SP);
                REGISTERS.AF = value;
                
                current_op.SP_after = REGISTERS.SP;
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                break;
            }
            
            case 0xF2: { // LD A, (FF00 + C)
                REGISTERS.A = ReadByte((ushort)(0xFF00 + REGISTERS.C));
                break;
            }
            
            case 0xF3: // DI
                INTERRUPT_MASTER_ENABLE = false;
                ENABLE_INTERRUPT_DELAY = 0;
                break;
            
            // 0xF4 UNUSED
            
            case 0xF5: // PUSH AF
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_before);
                
                PushU16( ref REGISTERS.SP, REGISTERS.AF);
                //Tick(4);
                
                current_op.SP_after = REGISTERS.SP;
                current_op.store_stack(gameboy, REGISTERS.SP, ref current_op.stack_after);
                break;
            
            case 0xF6: { // OR A, byte
                byte value = ReadByte(REGISTERS.PC++);
                current_op.operand_one = value;
                Or(ref REGISTERS.A, value);
                break;
            }
            
            case 0xF7: // RST 30h
                PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0030;
                break;
            
            case 0xF8: { // LD HL, SP+byte
                ushort sp = REGISTERS.SP;
                sbyte offset = unchecked((sbyte)ReadByte(REGISTERS.PC++));
                current_op.operand_one = (byte)offset;
                Tick(4);

                int result = sp + offset;

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry,
                    ((sp & 0x0F) + (offset & 0x0F)) > 0x0F);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry,
                    ((sp & 0xFF) + (offset & 0xFF)) > 0xFF);

                REGISTERS.HL = (ushort)result;
                break;
            }
            
            case 0xF9:  // LD SP, HL
                REGISTERS.SP = REGISTERS.HL;
                Tick(4);
                break;
            
            case 0xFA: { // LD A, (u16)
                ushort address = ReadU16(ref REGISTERS.PC);
                current_op.operand_one = (byte)address;
                current_op.operand_two = (byte)(address >> 8);
                REGISTERS.A = ReadByte(address);
                break;
            }
            
            case 0xFB: // EI
                ENABLE_INTERRUPT_DELAY = 1;
                break;
            
            // 0xFC UNUSED
            // 0xFD UNUSED
            
            case 0xFE: { // CP A, u8
                byte value = ReadByte(REGISTERS.PC++);
                current_op.operand_one = value;
                Compare(REGISTERS.A, value);
                break;
            }
            
            case 0xFF: // RST 38h
                PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0038;
                break;

            default: throw new Exception($"Unsupported OPCode: {opcode:X} (PC {REGISTERS.PC})");
        }
        
        
        if (ENABLE_INTERRUPT_DELAY > 0) {
            ENABLE_INTERRUPT_DELAY--;
            if (ENABLE_INTERRUPT_DELAY == 0) INTERRUPT_MASTER_ENABLE = true;
        }
        
        if (INTERRUPT_MASTER_ENABLE && InterruptPending) {
            ServiceInterrupt();
            cycles_since_last_op -= 20;
            return;
        }
        
        cycles_since_last_op = gameboy.TotalCycles - last_op_total_cycles;
        last_op_total_cycles = gameboy.TotalCycles;

        current_op.cycles = cycles_since_last_op;
    }
    
    private byte ReadCBRegister(int target) {
        return target switch {
            0 => REGISTERS.B,
            1 => REGISTERS.C,
            2 => REGISTERS.D,
            3 => REGISTERS.E,
            4 => REGISTERS.H,
            5 => REGISTERS.L,
            7 => REGISTERS.A
        };
    }

    private void WriteCBRegister(int target, byte value) {
        switch (target) {
            case 0: REGISTERS.B = value; break;
            case 1: REGISTERS.C = value; break;
            case 2: REGISTERS.D = value; break;
            case 3: REGISTERS.E = value; break;
            case 4: REGISTERS.H = value; break;
            case 5: REGISTERS.L = value; break;
            case 7: REGISTERS.A = value; break;
        }
    }
    
    private void CBTable(byte opcode) {
        int group = opcode >> 6;
        int op = (opcode >> 3) & 7;
        int target = opcode & 7;

        if (target == 6) {
            byte value = ReadByte(REGISTERS.HL);
            
            if (group == 1) {
                ExecuteBit(op, value);
            } else {
                byte result = ExecuteCBOperation(group, op, value);
                WriteByte(REGISTERS.HL, result);
            }
            
            return;
        }

        byte register = ReadCBRegister(target);
        byte reg_result = ExecuteCBOperation(group, op, register);
        WriteCBRegister(target, reg_result);
    }
    
    private byte ExecuteCBOperation(int group, int operation, byte value) {
        switch (group) {
            case 0:
                return ExecuteRotateShift(operation, value);

            case 1:
                ExecuteBit(operation, value);
                return value;

            case 2:
                return ExecuteRes(operation, value);

            case 3:
                return ExecuteSet(operation, value);

            default:
                throw new InvalidOperationException();
        }
    }
    
    private void ExecuteBit(int bit, byte value) {
        REGISTERS.SetFlag(GBRegisters.Mask.Zero, (value & (1 << bit)) == 0);
        REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, true);
    }
    
    private static byte ExecuteRes(int bit, byte value) {
        return (byte)(value & ~(1 << bit));
    }
    
    private static byte ExecuteSet(int bit, byte value) {
        return (byte)(value | (1 << bit));
    }
    
    private byte ExecuteRotateShift(int operation, byte value)
    {
        bool carry;

        switch (operation) {
            case 0: // RLC
                carry = (value & 0x80) != 0;
                value = (byte)((value << 1) | (carry ? 1 : 0));
                break;

            case 1: // RRC
                carry = (value & 0x01) != 0;
                value = (byte)((value >> 1) | (carry ? 0x80 : 0));
                break;

            case 2: { // RL
                bool old_carry = REGISTERS.GetFlag(GBRegisters.Mask.Carry);
                carry = (value & 0x80) != 0;
                value = (byte)((value << 1) | (old_carry ? 1 : 0));
                break;
            }

            case 3: { // RR
                bool old_carry = REGISTERS.GetFlag(GBRegisters.Mask.Carry);
                carry = (value & 0x01) != 0;
                value = (byte)((value >> 1) | (old_carry ? 0x80 : 0));
                break;
            }

            case 4: // SLA
                carry = (value & 0x80) != 0;
                value <<= 1;
                break;

            case 5: // SRA
                carry = (value & 0x01) != 0;
                value = (byte)((value >> 1) | (value & 0x80));
                break;

            case 6: // SWAP
                carry = false;
                value = (byte)((value << 4) | (value >> 4));
                break;

            case 7: // SRL
                carry = (value & 0x01) != 0;
                value >>= 1;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        REGISTERS.SetFlag(GBRegisters.Mask.Zero, value == 0);
        REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
        REGISTERS.SetFlag(GBRegisters.Mask.Carry, carry);

        return value;
    }
}