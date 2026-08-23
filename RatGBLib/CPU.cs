using System.Diagnostics;

namespace RatGBLib;
using static GameBoy;

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
    
    public bool InterruptEnabled(InterruptMask interrupt) => (gameboy.ReadByte(0xFFFF) & (byte)interrupt) != 0;
    public bool InterruptRequested(InterruptMask interrupt) => (gameboy.ReadByte(0xFF0F) & (byte)interrupt) != 0; 
    
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
        byte pre_increment = gameboy.ReadByte(address);
        byte result = (byte)(pre_increment + 1);

        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, (pre_increment & 0x0F) == 0x0F);
        
        gameboy.WriteByte(address, result);
    }
    
    private void DecrementAtAddress(GameBoy gameboy, ushort address) {
        byte pre_decrement = gameboy.ReadByte(address);
        byte result = (byte)(pre_decrement - 1);

        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Zero, result == 0);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.Negative, true);
        gameboy.CPU.REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, (pre_decrement & 0x0F) == 0);
        
        gameboy.WriteByte(address, result);
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
    
    private byte FetchOpcode() {
        byte op = gameboy.ReadByte(REGISTERS.PC);

        if (HALT_BUG) HALT_BUG = false;
        else REGISTERS.PC++;
        
        return op;
    }

    private GameBoy gameboy;
    public CPU(GameBoy gameboy) => this.gameboy = gameboy;
    
    private int ServiceInterrupt() {
        InterruptMask interrupt;

        if (InterruptEnabled(InterruptMask.VBlank) &&
            InterruptRequested(InterruptMask.VBlank))
            interrupt = InterruptMask.VBlank;

        else if (InterruptEnabled(InterruptMask.LCD) &&
                 InterruptRequested(InterruptMask.LCD))
            interrupt = InterruptMask.LCD;

        else if (InterruptEnabled(InterruptMask.Timer) &&
                 InterruptRequested(InterruptMask.Timer))
            interrupt = InterruptMask.Timer;

        else if (InterruptEnabled(InterruptMask.Serial) &&
                 InterruptRequested(InterruptMask.Serial))
            interrupt = InterruptMask.Serial;

        else if (InterruptEnabled(InterruptMask.Joypad) &&
                 InterruptRequested(InterruptMask.Joypad))
            interrupt = InterruptMask.Joypad;
        else
            interrupt = 0;
        
        
        INTERRUPT_MASTER_ENABLE = false;
        REGISTERS.IF &= (byte)~interrupt;

        gameboy.PushU16(ref REGISTERS.SP, REGISTERS.PC);

        switch (interrupt) {
            case InterruptMask.VBlank:
                REGISTERS.PC = 0x0040;
                break;

            case InterruptMask.LCD:
                REGISTERS.PC = 0x0048;
                break;

            case InterruptMask.Timer:
                REGISTERS.PC = 0x0050;
                break;

            case InterruptMask.Serial:
                REGISTERS.PC = 0x0058;
                break;

            case InterruptMask.Joypad:
                REGISTERS.PC = 0x0060;
                break;
        }

        return 4;
    }
    
    public int Execute() {
        
        if (STOPPED)
            return 4;

        if (HALTED) {
            if (InterruptPending) HALTED = false;
            return 4;
        }
        
        if (ENABLE_INTERRUPT_DELAY > 0) {
            ENABLE_INTERRUPT_DELAY--;

            if (ENABLE_INTERRUPT_DELAY == 0) {
                INTERRUPT_MASTER_ENABLE = true;
                ENABLE_INTERRUPT_DELAY = -1;
            }
        }
        
        if (INTERRUPT_MASTER_ENABLE && InterruptPending) return ServiceInterrupt();
        
        byte opcode = FetchOpcode();
        
        //Console.WriteLine($"PC_AFTER_FETCH = {REGISTERS.PC:X4} NEXT_OP = {opcode:X2} :: A:{REGISTERS.A:X2} B:{REGISTERS.B:X2} C:{REGISTERS.C:X2} D:{REGISTERS.D:X2} E:{REGISTERS.E:X2} H:{REGISTERS.H:X2} L:{REGISTERS.L:X2} :: FLAGS Z:{REGISTERS.GetFlag(GBRegisters.Mask.Zero):X} N:{REGISTERS.GetFlag(GBRegisters.Mask.Negative):X} H:{REGISTERS.GetFlag(GBRegisters.Mask.HalfCarry):X} C:{REGISTERS.GetFlag(GBRegisters.Mask.Carry):X}");
        
        switch (opcode) {
            // -------- 0x0x --------
            
            case 0x00: return 4; // NOP
            
            case 0x01: // LD BC, ushort
                REGISTERS.BC = gameboy.ReadU16(ref REGISTERS.PC);
                return 12;
            
            case 0x02: // LD (BC), A
                gameboy.WriteByte(REGISTERS.BC, REGISTERS.A);
                return 8;
            
            case 0x03: // INC BC
                REGISTERS.BC++;    
                return 8;

            case 0x04: //INC B
                Increment(ref REGISTERS.B);
                return 4;
            
            case 0x05: // DEC B
                Decrement(ref REGISTERS.B);
                return 4;
            
            case 0x06: // LD B, (byte)
                REGISTERS.B = gameboy.ReadByte(REGISTERS.PC++);
                return 8;

            case 0x07: { // RLCA 
                bool carry = (REGISTERS.A & 0x80) != 0;
                REGISTERS.A = (byte)((REGISTERS.A << 1) | (carry ? 1 : 0));

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, carry);
                return 4;
            }

            case 0x08: { // LD ushort, SP
                ushort value = gameboy.ReadU16(ref REGISTERS.PC);
                gameboy.WriteByte(value, (byte)REGISTERS.SP);
                gameboy.WriteByte((ushort)(value + 1), (byte)(REGISTERS.SP >> 8));
                return 20;
            }

            case 0x09: // ADD HL, BC
                REGISTERS.HL = Add(REGISTERS.HL, REGISTERS.BC);
                return 8;
            
            case 0x0A: // LD A, (BC)
                REGISTERS.A = gameboy.ReadByte(REGISTERS.BC);
                return 8;
            
            case 0x0B: // DEC BC
                REGISTERS.BC--;
                return 8;
            
            case 0x0C: // INC C
                Increment(ref REGISTERS.C);
                return 4;
            
            case 0x0D: // DEC C
                Decrement(ref REGISTERS.C);
                return 4;
            
            case 0x0E: // LD C, byte
                REGISTERS.C = gameboy.ReadByte(REGISTERS.PC++);
                return 8;
            
            case 0x0F: { // RRCA 
                bool carry = (REGISTERS.A & 0x01) != 0;
                REGISTERS.A = (byte)((REGISTERS.A >> 1) | (carry ? 0x80 : 0));

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, carry);
                
                return 4;
            }
            
            // -------- 0x1x -------- 
            
            case 0x10: // STOP
                REGISTERS.PC++;
                STOPPED = true;
                return 4;
            
            case 0x11: // LD DE, ushort
                REGISTERS.DE = gameboy.ReadU16(ref REGISTERS.PC);
                return 12;
            
            case 0x12: // LD (DE), A
                gameboy.WriteByte(REGISTERS.DE, REGISTERS.A);
                return 8;
            
            case 0x13: // INC DE
                REGISTERS.DE++;
                return 8;
            
            case 0x14: // INC D
                Increment(ref REGISTERS.D);
                return 4;
            
            case 0x15: // DEC D
                Decrement(ref REGISTERS.D);
                return 4;
            
            case 0x16: // LD D, byte
                REGISTERS.D = gameboy.ReadByte(REGISTERS.PC++);
                return 8;
            
            case 0x17: { // RLA 
                bool old_carry = REGISTERS.GetFlag(GBRegisters.Mask.Carry);
                bool carry = (REGISTERS.A & 0x80) != 0;
                REGISTERS.A = (byte)((REGISTERS.A << 1) | (old_carry ? 1 : 0));

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, carry);
                return 4;
            }

            case 0x18: { // JR byte
                sbyte offset = unchecked((sbyte)gameboy.ReadByte(REGISTERS.PC++));
                REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                return 12;
            }
            
            case 0x19: // ADD HL, DE
                REGISTERS.HL = Add(REGISTERS.HL, REGISTERS.DE);
                return 8;
            
            case 0x1A: // LD A, (DE)
                REGISTERS.A = gameboy.ReadByte(REGISTERS.DE);
                return 8;
            
            case 0x1B: // DEC DE
                REGISTERS.DE--;
                return 8;
            
            case 0x1C: // INC E
                Increment(ref REGISTERS.E);
                return 4;
            
            case 0x1D: // DEC E
                Decrement(ref REGISTERS.E);
                return 4;
            
            case 0x1E: // LD E, byte
                REGISTERS.E = gameboy.ReadByte(REGISTERS.PC++);
                return 8;
            
            case 0x1F: { // RRA 
                bool old_carry = REGISTERS.GetFlag(GBRegisters.Mask.Carry);
                bool carry = (REGISTERS.A & 0x01) != 0;
                
                REGISTERS.A = (byte)((REGISTERS.A >> 1) | (old_carry ? 0x80 : 0));

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, carry);
                
                return 4;
            }
            
            // -------- 0x2x --------
            
            case 0x20: { // JR NZ, byte
                sbyte offset = unchecked((sbyte)gameboy.ReadByte(REGISTERS.PC++));

                if (!REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                    return 12;
                }
                return 8;
            }

            case 0x21: // LD HL, ushort
                REGISTERS.HL = gameboy.ReadU16(ref REGISTERS.PC);
                return 12;
            
            case 0x22: // LD (HL+), A
                gameboy.WriteByte(REGISTERS.HL++, REGISTERS.A);
                return 8;
            
            case 0x23: // INC HL
                REGISTERS.HL++;
                return 8;
            
            case 0x24: // INC H
                Increment(ref REGISTERS.H);
                return 4;
            
            case 0x25: // DEC H
                Decrement(ref REGISTERS.H);
                return 4;
            
            case 0x26: // LD H, byte
                REGISTERS.H = gameboy.ReadByte(REGISTERS.PC++);
                return 8;

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
                
                return 4;
            }
            
            case 0x28: { // JR Z, byte
                sbyte offset = unchecked((sbyte)gameboy.ReadByte(REGISTERS.PC++));

                if (REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                    return 12;
                }

                return 8;
            }

            case 0x29: // ADD HL, HL
                REGISTERS.HL = Add(REGISTERS.HL, REGISTERS.HL);
                return 8;
            
            case 0x2A: // LD A, (HL+)
                REGISTERS.A = gameboy.ReadByte(REGISTERS.HL++);
                return 8;
            
            case 0x2B: // DEC HL
                REGISTERS.HL--;
                return 8;
            
            case 0x2C: // INC L
                Increment(ref REGISTERS.L);
                return 4;
            
            case 0x2D: // DEC L
                Decrement(ref REGISTERS.L);
                return 4;
            
            case 0x2E: // LD L, byte
                REGISTERS.L = gameboy.ReadByte(REGISTERS.PC++);
                return 8;
            
            case 0x2F: // CPL
                REGISTERS.A = (byte)~REGISTERS.A;
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, true);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, true);
                
                return 4;
            
            // -------- 0x3x --------
            
            case 0x30: { // JR NC, byte
                sbyte offset = unchecked((sbyte)gameboy.ReadByte(REGISTERS.PC++));

                if (!REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                    return 12;
                }

                return 8;
            }
            
            case 0x31: // LD SP, ushort
                REGISTERS.SP = gameboy.ReadU16(ref REGISTERS.PC);
                return 12;
            
            case 0x32: // LD (HL-), A
                gameboy.WriteByte(REGISTERS.HL--, REGISTERS.A);
                return 8;
            
            case 0x33: // INC SP
                REGISTERS.SP++;
                return 8;
            
            case 0x34: // INC (HL)
                IncrementAtAddress(gameboy, REGISTERS.HL);
                return 12;
            
            case 0x35: // DEC (HL)
                DecrementAtAddress(gameboy, REGISTERS.HL);
                return 12;
            
            case 0x36: // LD (HL), byte
                gameboy.WriteByte(REGISTERS.HL, gameboy.ReadByte(REGISTERS.PC++));
                return 12;
            
            case 0x37: // SCF
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, true);
                return 4;
            
            case 0x38: { // JR C, byte
                sbyte offset = unchecked((sbyte)gameboy.ReadByte(REGISTERS.PC++));

                if (REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = (ushort)(REGISTERS.PC + offset);
                    return 12;
                }

                return 8;
            }
            
            case 0x39: // ADD HL, SP
                REGISTERS.HL = Add(REGISTERS.HL, REGISTERS.SP);
                return 8;
            
            case 0x3A: // LD A, (HL-)
                REGISTERS.A = gameboy.ReadByte(REGISTERS.HL--);
                return 8;
            
            case 0x3B: // DEC SP
                REGISTERS.SP--;
                return 8;
            
            case 0x3C: // INC A
                Increment(ref REGISTERS.A);
                return 4;
            
            case 0x3D: // DEC A
                Decrement(ref REGISTERS.A);
                return 4;
            
            case 0x3E: // LD A, byte
                REGISTERS.A = gameboy.ReadByte(REGISTERS.PC++);
                return 8;
            
            case 0x3F: // CCF
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, !REGISTERS.GetFlag(GBRegisters.Mask.Carry));
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, false);
                return 4;
            
            // -------- 0x4x --------
            
            case 0x40: REGISTERS.B = REGISTERS.B; return 4; // LD B, B
            case 0x41: REGISTERS.B = REGISTERS.C; return 4; // LD B, C
            case 0x42: REGISTERS.B = REGISTERS.D; return 4; // LD B, D
            case 0x43: REGISTERS.B = REGISTERS.E; return 4; // LD B, E
            case 0x44: REGISTERS.B = REGISTERS.H; return 4; // LD B, H
            case 0x45: REGISTERS.B = REGISTERS.L; return 4; // LD B, L
            case 0x46: REGISTERS.B = gameboy.ReadByte(REGISTERS.HL); return 8; // LD B, (HL)
            case 0x47: REGISTERS.B = REGISTERS.A; return 4; // LD B, A
            
            case 0x48: REGISTERS.C = REGISTERS.B; return 4; // LD C, B
            case 0x49: REGISTERS.C = REGISTERS.C; return 4; // LD C, C
            case 0x4A: REGISTERS.C = REGISTERS.D; return 4; // LD C, D
            case 0x4B: REGISTERS.C = REGISTERS.E; return 4; // LD C, E
            case 0x4C: REGISTERS.C = REGISTERS.H; return 4; // LD C, H
            case 0x4D: REGISTERS.C = REGISTERS.L; return 4; // LD C, L
            case 0x4E: REGISTERS.C = gameboy.ReadByte(REGISTERS.HL); return 8; // LD C, (HL)
            case 0x4F: REGISTERS.C = REGISTERS.A; return 4; // LD C, A
            
            // -------- 0x5x --------
            
            case 0x50: REGISTERS.D = REGISTERS.B; return 4; // LD D, B
            case 0x51: REGISTERS.D = REGISTERS.C; return 4; // LD D, C
            case 0x52: REGISTERS.D = REGISTERS.D; return 4; // LD D, D
            case 0x53: REGISTERS.D = REGISTERS.E; return 4; // LD D, E
            case 0x54: REGISTERS.D = REGISTERS.H; return 4; // LD D, H
            case 0x55: REGISTERS.D = REGISTERS.L; return 4; // LD D, L
            case 0x56: REGISTERS.D = gameboy.ReadByte(REGISTERS.HL); return 8; // LD D, (HL)
            case 0x57: REGISTERS.D = REGISTERS.A; return 4; // LD D, A
            
            case 0x58: REGISTERS.E = REGISTERS.B; return 4; // LD E, B
            case 0x59: REGISTERS.E = REGISTERS.C; return 4; // LD E, C
            case 0x5A: REGISTERS.E = REGISTERS.D; return 4; // LD E, D
            case 0x5B: REGISTERS.E = REGISTERS.E; return 4; // LD E, E
            case 0x5C: REGISTERS.E = REGISTERS.H; return 4; // LD E, H
            case 0x5D: REGISTERS.E = REGISTERS.L; return 4; // LD E, L
            case 0x5E: REGISTERS.E = gameboy.ReadByte(REGISTERS.HL); return 8; // LD E, (HL)
            case 0x5F: REGISTERS.E = REGISTERS.A; return 4; // LD E, A
            
            // -------- 0x6x --------
            
            case 0x60: REGISTERS.H = REGISTERS.B; return 4; // LD H, B
            case 0x61: REGISTERS.H = REGISTERS.C; return 4; // LD H, C
            case 0x62: REGISTERS.H = REGISTERS.D; return 4; // LD H, D
            case 0x63: REGISTERS.H = REGISTERS.E; return 4; // LD H, E
            case 0x64: REGISTERS.H = REGISTERS.H; return 4; // LD H, H
            case 0x65: REGISTERS.H = REGISTERS.L; return 4; // LD H, L
            case 0x66: REGISTERS.H = gameboy.ReadByte(REGISTERS.HL); return 8; // LD H, (HL)
            case 0x67: REGISTERS.H = REGISTERS.A; return 4; // LD H, A
            
            case 0x68: REGISTERS.L = REGISTERS.B; return 4; // LD L, B
            case 0x69: REGISTERS.L = REGISTERS.C; return 4; // LD L, C
            case 0x6A: REGISTERS.L = REGISTERS.D; return 4; // LD L, D
            case 0x6B: REGISTERS.L = REGISTERS.E; return 4; // LD L, E
            case 0x6C: REGISTERS.L = REGISTERS.H; return 4; // LD L, H
            case 0x6D: REGISTERS.L = REGISTERS.L; return 4; // LD L, L
            case 0x6E: REGISTERS.L = gameboy.ReadByte(REGISTERS.HL); return 8; // LD L, (HL)
            case 0x6F: REGISTERS.L = REGISTERS.A; return 4; // LD L, A
            
            // -------- 0x7x --------
            
            case 0x70: gameboy.WriteByte(REGISTERS.HL, REGISTERS.B); return 8; // LD (HL), B
            case 0x71: gameboy.WriteByte(REGISTERS.HL, REGISTERS.C); return 8; // LD (HL), C
            case 0x72: gameboy.WriteByte(REGISTERS.HL, REGISTERS.D); return 8; // LD (HL), D
            case 0x73: gameboy.WriteByte(REGISTERS.HL, REGISTERS.E); return 8; // LD (HL), E
            case 0x74: gameboy.WriteByte(REGISTERS.HL, REGISTERS.H); return 8; // LD (HL), H
            case 0x75: gameboy.WriteByte(REGISTERS.HL, REGISTERS.L); return 8; // LD (HL), L
            
            case 0x76: // HALT
                if (InterruptPending)
                    if (!INTERRUPT_MASTER_ENABLE) HALT_BUG = true;
                else HALTED = true; 
                return 4;
            
            case 0x77: gameboy.WriteByte(REGISTERS.HL, REGISTERS.A); return 8; // LD (HL), A
            
            case 0x78: REGISTERS.A = REGISTERS.B; return 4; // LD A, B
            case 0x79: REGISTERS.A = REGISTERS.C; return 4; // LD A, C
            case 0x7A: REGISTERS.A = REGISTERS.D; return 4; // LD A, D
            case 0x7B: REGISTERS.A = REGISTERS.E; return 4; // LD A, E
            case 0x7C: REGISTERS.A = REGISTERS.H; return 4; // LD A, H
            case 0x7D: REGISTERS.A = REGISTERS.L; return 4; // LD A, L
            case 0x7E: REGISTERS.A = gameboy.ReadByte(REGISTERS.HL); return 8; // LD A, (HL)
            case 0x7F: REGISTERS.A = REGISTERS.A; return 4; // LD A, A
            
            // -------- 0x8x --------
            
            case 0x80: Add(ref REGISTERS.A, REGISTERS.B); return 4; // ADD A, B
            case 0x81: Add(ref REGISTERS.A, REGISTERS.C); return 4; // ADD A, C
            case 0x82: Add(ref REGISTERS.A, REGISTERS.D); return 4; // ADD A, D
            case 0x83: Add(ref REGISTERS.A, REGISTERS.E); return 4; // ADD A, E
            case 0x84: Add(ref REGISTERS.A, REGISTERS.H); return 4; // ADD A, H
            case 0x85: Add(ref REGISTERS.A, REGISTERS.L); return 4; // ADD A, L
            case 0x86: Add(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.HL)); return 8; // ADD A, (HL)
            case 0x87: Add(ref REGISTERS.A, REGISTERS.A); return 4; // ADD A, A
            
            case 0x88: AddWithCarry(ref REGISTERS.A, REGISTERS.B); return 4; // ADC A, B
            case 0x89: AddWithCarry(ref REGISTERS.A, REGISTERS.C); return 4; // ADC A, C
            case 0x8A: AddWithCarry(ref REGISTERS.A, REGISTERS.D); return 4; // ADC A, D
            case 0x8B: AddWithCarry(ref REGISTERS.A, REGISTERS.E); return 4; // ADC A, E
            case 0x8C: AddWithCarry(ref REGISTERS.A, REGISTERS.H); return 4; // ADC A, H
            case 0x8D: AddWithCarry(ref REGISTERS.A, REGISTERS.L); return 4; // ADC A, L
            case 0x8E: AddWithCarry(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.HL)); return 8; // ADC A, (HL)
            case 0x8F: AddWithCarry(ref REGISTERS.A, REGISTERS.A); return 4; // ADC A, A
            
            // -------- 0x9x --------
            
            case 0x90: Subtract(ref REGISTERS.A, REGISTERS.B); return 4;                   // SUB A, B
            case 0x91: Subtract(ref REGISTERS.A, REGISTERS.C); return 4;          // SUB A, C
            case 0x92: Subtract(ref REGISTERS.A, REGISTERS.D); return 4;          // SUB A, D
            case 0x93: Subtract(ref REGISTERS.A, REGISTERS.E); return 4;          // SUB A, E
            case 0x94: Subtract(ref REGISTERS.A, REGISTERS.H); return 4;          // SUB A, H
            case 0x95: Subtract(ref REGISTERS.A, REGISTERS.L); return 4;          // SUB A, L
            case 0x96: Subtract(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.HL)); return 8; // SUB A, (HL)
            case 0x97: Subtract(ref REGISTERS.A, REGISTERS.A); return 4;          // SUB A, A
            
            case 0x98: SubtractWithCarry(ref REGISTERS.A, REGISTERS.B); return 4;          // SUB A, B
            case 0x99: SubtractWithCarry(ref REGISTERS.A, REGISTERS.C); return 4; // SBC A, C
            case 0x9A: SubtractWithCarry(ref REGISTERS.A, REGISTERS.D); return 4; // SBC A, D
            case 0x9B: SubtractWithCarry(ref REGISTERS.A, REGISTERS.E); return 4; // SBC A, E
            case 0x9C: SubtractWithCarry(ref REGISTERS.A, REGISTERS.H); return 4; // SBC A, H
            case 0x9D: SubtractWithCarry(ref REGISTERS.A, REGISTERS.L); return 4; // SBC A, L
            case 0x9E: SubtractWithCarry(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.HL)); return 8; // SBC A, (HL)
            case 0x9F: SubtractWithCarry(ref REGISTERS.A, REGISTERS.A); return 4; // SBC A, A
            
            // -------- 0xAx --------
            
            case 0xA0: And(ref REGISTERS.A, REGISTERS.B); return 4;                   // AND A, B
            case 0xA1: And(ref REGISTERS.A, REGISTERS.C); return 4;          // AND A, C
            case 0xA2: And(ref REGISTERS.A, REGISTERS.D); return 4;          // AND A, D
            case 0xA3: And(ref REGISTERS.A, REGISTERS.E); return 4;          // AND A, E
            case 0xA4: And(ref REGISTERS.A, REGISTERS.H); return 4;          // AND A, H
            case 0xA5: And(ref REGISTERS.A, REGISTERS.L); return 4;          // AND A, L
            case 0xA6: And(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.HL)); return 8; // AND A, (HL)
            case 0xA7: And(ref REGISTERS.A, REGISTERS.A); return 4;          // AND A, A
            
            case 0xA8: Xor(ref REGISTERS.A, REGISTERS.B); return 4;          // XOR A, B
            case 0xA9: Xor(ref REGISTERS.A, REGISTERS.C); return 4; // XOR A, C
            case 0xAA: Xor(ref REGISTERS.A, REGISTERS.D); return 4; // XOR A, D
            case 0xAB: Xor(ref REGISTERS.A, REGISTERS.E); return 4; // XOR A, E
            case 0xAC: Xor(ref REGISTERS.A, REGISTERS.H); return 4; // XOR A, H
            case 0xAD: Xor(ref REGISTERS.A, REGISTERS.L); return 4; // XOR A, L
            case 0xAE: Xor(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.HL)); return 8; // XOR A, (HL)
            case 0xAF: Xor(ref REGISTERS.A, REGISTERS.A); return 4; // XOR A, A
            
            // -------- 0xBx --------
            
            case 0xB0: Or(ref REGISTERS.A, REGISTERS.B); return 4;                   // OR A, B
            case 0xB1: Or(ref REGISTERS.A, REGISTERS.C); return 4;          // OR A, C
            case 0xB2: Or(ref REGISTERS.A, REGISTERS.D); return 4;          // OR A, D
            case 0xB3: Or(ref REGISTERS.A, REGISTERS.E); return 4;          // OR A, E
            case 0xB4: Or(ref REGISTERS.A, REGISTERS.H); return 4;          // OR A, H
            case 0xB5: Or(ref REGISTERS.A, REGISTERS.L); return 4;          // OR A, L
            case 0xB6: Or(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.HL)); return 8; // OR A, (HL)
            case 0xB7: Or(ref REGISTERS.A, REGISTERS.A); return 4;          // OR A, A
            
            case 0xB8: Compare(REGISTERS.A, REGISTERS.B); return 4; // CP B
            case 0xB9: Compare(REGISTERS.A, REGISTERS.C); return 4; // CP C
            case 0xBA: Compare(REGISTERS.A, REGISTERS.D); return 4; // CP D
            case 0xBB: Compare(REGISTERS.A, REGISTERS.E); return 4; // CP E
            case 0xBC: Compare(REGISTERS.A, REGISTERS.H); return 4; // CP H
            case 0xBD: Compare(REGISTERS.A, REGISTERS.L); return 4; // CP L
            case 0xBE: Compare(REGISTERS.A, gameboy.ReadByte(REGISTERS.HL)); return 8; // CP (HL)
            case 0xBF: Compare(REGISTERS.A, REGISTERS.A); return 4; // CP A
            
            // -------- 0xCx --------

            case 0xC0: // RET NZ
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = gameboy.PopU16(ref REGISTERS.SP);
                    //return 20;
                }
                return 8;
            
            case 0xC1: { // POP BC
                ushort value = gameboy.PopU16(ref REGISTERS.SP);
                REGISTERS.BC = value;
                return 0;
            }
            
            case 0xC2: { // JP NZ, ushort
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = address;
                    return 16;
                }
                return 12;
            }
            
            case 0xC3: // JP ushort
                REGISTERS.PC = gameboy.ReadU16(ref REGISTERS.PC);
                return 16;
            
            
            case 0xC4: { // CALL NZ, ushort
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    gameboy.PushU16(ref REGISTERS.SP, REGISTERS.PC);
                    REGISTERS.PC = address;
                    //return 24;
                }
                return 12;
            }
            
            case 0xC5: // PUSH BC
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.BC);
                return 0;
            
            case 0xC6: // ADD A, byte
                Add(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.PC++));
                return 8;
            
            case 0xC7: // RST 00h
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0000;
                return 0;
            
            case 0xC8: // RET Z
                if (REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = gameboy.PopU16(ref REGISTERS.SP);
                    //return 20;
                }
                return 8;
            
            case 0xC9: // RET
                REGISTERS.PC = gameboy.PopU16(ref REGISTERS.SP);
                return 0;
            
            case 0xCA: { // JP Z, ushort
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                if (REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    REGISTERS.PC = address;
                    return 16;
                }
                return 12;
            }

            case 0xCB: { // CB PREFIX
                return CBTable(gameboy.ReadByte(REGISTERS.PC++));
            }
                
            case 0xCC: { // CALL Z, ushort
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                if (REGISTERS.GetFlag(GBRegisters.Mask.Zero)) {
                    gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                    REGISTERS.PC = address;
                    //return 24;
                }
                return 12;
            }
            
            case 0xCD: { // CALL ushort
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = address;
                return 8;
            }
            
            case 0xCE: // ADC A, byte
                AddWithCarry(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.PC++));
                return 8;

            case 0xCF: // RST 08h
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0008;
                return 0;
            
            // -------- 0xDx --------
            
            case 0xD0: { //RET NC
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = gameboy.PopU16(ref REGISTERS.SP);
                    //return 20;
                }
                return 8;
            }
            
            case 0xD1: { // POP DE
                ushort value = gameboy.PopU16(ref REGISTERS.SP);
                REGISTERS.DE = value;
                return 0;
            }
            
            case 0xD2: { // JP NC, ushort
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = address;
                    return 16;
                }
                return 12;
            }
            
            // 0xD3 UNUSED
            
            case 0xD4: { // CALL NC, ushort
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                if (!REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                    REGISTERS.PC = address;
                    //return 24;
                }
                return 12;
            }
            
            case 0xD5: // PUSH DE
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.DE);
                return 0;
            
            case 0xD6: // SUB A, byte
                Subtract(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.PC++));
                return 8;
            
            case 0xD7: // RST 10h
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0010;
                return 0;
            
            case 0xD8: // RET C
                if (REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = gameboy.PopU16(ref REGISTERS.SP);
                    //return 20;
                }
                return 8;
            
            case 0xD9: // RETI
                REGISTERS.PC = gameboy.PopU16(ref REGISTERS.SP);
                INTERRUPT_MASTER_ENABLE = true;
                return 0;
            
            case 0xDA: { // JP C, ushort
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                if (REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    REGISTERS.PC = address;
                    return 16;
                }
                return 12;
            }
            
            // 0xDB UNUSED
            
            case 0xDC: { // CALL C, ushort
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                if (REGISTERS.GetFlag(GBRegisters.Mask.Carry)) {
                    gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                    REGISTERS.PC = address;
                    //return 24;
                }
                return 12;
            }
            
            // 0xDD UNUSED
            
            case 0xDE: // SBC A, byte
                SubtractWithCarry(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.PC++));
                return 8;
            
            case 0xDF: // RST 18h
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0018;
                return 0;
            
            // -------- 0xEx --------

            case 0xE0: { // LD (FF00 + byte), A
                byte offset = gameboy.ReadByte(REGISTERS.PC++);
                gameboy.WriteByte((ushort)(0xFF00 + offset), REGISTERS.A);
                return 12;
            }

            case 0xE1: { // POP HL
                ushort value = gameboy.PopU16(ref REGISTERS.SP);
                REGISTERS.HL = value;
                return 0;
            }
            
            case 0xE2: { // LD (FF00 + C), A
                gameboy.WriteByte((ushort)(0xFF00 + REGISTERS.C), REGISTERS.A);
                return 8;
            }
            
            // 0xE3 UNUSED
            
            // 0xE4 UNUSED
            
            case 0xE5: // PUSH HL
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.HL);
                return 0;
            
            case 0xE6: // AND A, byte
                And(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.PC++));
                return 8;
            
            case 0xE7: // RST 20h
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0020;
                return 0;

            case 0xE8: { // ADD SP, byte
                ushort a = REGISTERS.SP;
                sbyte b = unchecked((sbyte)gameboy.ReadByte(REGISTERS.PC++));

                int result = a + b;
                
                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry, ((a & 0x0F) + (b & 0x0F)) > 0x0F);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry, ((a & 0xFF) + (b & 0xFF)) > 0xFF);

                REGISTERS.SP = (ushort)result;
                return 16;
            }
            
            case 0xE9: // JP HL
                REGISTERS.PC = REGISTERS.HL;
                return 4;
            
            case 0xEA: { // LD (ushort), A
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                gameboy.WriteByte(address, REGISTERS.A);
                return 16;
            }
            
            // 0xEB UNUSED
            // 0xEC UNUSED
            // 0xED UNUSED
                
            case 0xEE: // XOR A, byte
                Xor(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.PC++));
                return 8;
            
            case 0xEF: // RST 28h
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0028;
                return 0;
            
            // -------- 0xFx --------
            
            case 0xF0: { // LD A, (FF00 + byte)
                REGISTERS.A = gameboy.ReadByte((ushort)(0xFF00 + gameboy.ReadByte(REGISTERS.PC++)));
                return 12;
            }
            
            case 0xF1: { // POP AF
                ushort value = gameboy.PopU16(ref REGISTERS.SP);
                REGISTERS.AF = value;
                return 0;
            }
            
            case 0xF2: { // LD A, (FF00 + C)
                REGISTERS.A = gameboy.ReadByte((ushort)(0xFF00 + REGISTERS.C));
                return 8;
            }
            
            case 0xF3: // DI
                INTERRUPT_MASTER_ENABLE = false;
                ENABLE_INTERRUPT_DELAY = -1;
                return 4;
            
            // 0xF4 UNUSED
            
            case 0xF5: // PUSH AF
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.AF);
                return 0;
            
            case 0xF6: // OR A, byte
                Or(ref REGISTERS.A, gameboy.ReadByte(REGISTERS.PC++));
                return 8;
            
            case 0xF7: // RST 30h
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0030;
                return 0;
            
            case 0xF8: { // LD HL, SP+byte
                ushort sp = REGISTERS.SP;
                sbyte offset = unchecked((sbyte)gameboy.ReadByte(REGISTERS.PC++));

                int result = sp + offset;

                REGISTERS.SetFlag(GBRegisters.Mask.Zero, false);
                REGISTERS.SetFlag(GBRegisters.Mask.Negative, false);
                REGISTERS.SetFlag(GBRegisters.Mask.HalfCarry,
                    ((sp & 0x0F) + (offset & 0x0F)) > 0x0F);
                REGISTERS.SetFlag(GBRegisters.Mask.Carry,
                    ((sp & 0xFF) + (offset & 0xFF)) > 0xFF);

                REGISTERS.HL = (ushort)result;
                return 12;
            }
            
            case 0xF9:  // LD SP, HL
                REGISTERS.SP = REGISTERS.HL;
                return 8;
            
            case 0xFA: { // LD A, (u16)
                ushort address = gameboy.ReadU16(ref REGISTERS.PC);
                REGISTERS.A = gameboy.ReadByte(address);
                return 16;
            }
            
            case 0xFB: // EI
                ENABLE_INTERRUPT_DELAY = 2;
                return 4;
            
            // 0xFC UNUSED
            // 0xFD UNUSED
            
            case 0xFE: { // CP A, u8
                byte value = gameboy.ReadByte(REGISTERS.PC++);
                Compare(REGISTERS.A, value);
                return 8;
            }
            
            case 0xFF: // RST 38h
                gameboy.PushU16( ref REGISTERS.SP, REGISTERS.PC);
                REGISTERS.PC = 0x0038;
                return 0;

            default: throw new Exception($"Unsupported OPCode: {opcode:X}");
        }
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
    
    private int CBTable(byte opcode) {
        int group = opcode >> 6;
        int op = (opcode >> 3) & 7;
        int target = opcode & 7;

        if (target == 6) {
            byte value = gameboy.ReadByte(REGISTERS.HL);
            byte result = ExecuteCBOperation(group, op, value);
            gameboy.WriteByte(REGISTERS.HL, result);
            return group == 1 ? 12 : 16;
        }

        byte register = ReadCBRegister(target);
        byte reg_result = ExecuteCBOperation(group, op, register);
        WriteCBRegister(target, reg_result);
        
        return 8;
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