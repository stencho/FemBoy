using System;

namespace FemBoy;

public class MicroOp {
    public byte wait_after;
    public Action operation;
    
    public MicroOp(byte wait_after, Action operation) {
        this.wait_after = wait_after;
        this.operation = operation;
    }
}

public class Operation {
    public byte initial_wait = 0;
    public MicroOp[] operations;
    
    public Operation(byte initial_wait, MicroOp[] operations) {
        this.initial_wait = initial_wait;
        this.operations = operations;
    }
} 

public class CPUOperations {
    private GameBoy gameboy;
    private CPU CPU => gameboy.CPU;
    private CPURegisters Registers => CPU.Registers;
    
    internal Operation? current_operation = null;
    
    internal int source_register;
    internal int target_register;

    internal ushort pointer;
    internal ushort return_address;
    internal ushort buffer;
    internal int alu_op;

    internal byte cb_sub_op;
    internal int cb_op;
    
    internal enum HLMutation {None, Inc, Dec}
    internal HLMutation hl_mutation = HLMutation.None;
    
    internal Operation AddU16RegReg;

    internal Operation LDU16SP;
    
    internal Operation JR;

    internal Operation RETTaken;
    internal Operation RETFailed;
    
    internal Operation JRFailed;

    internal Operation LDU16;
    internal Operation LDSPHL;
    
    internal Operation JPFailed;
    internal Operation JPTaken;

    internal Operation LDRegFromMem;
    internal Operation LDMemFromReg;
    
    internal Operation IncU16Reg;
    internal Operation DecU16Reg;

    internal Operation IncHLMem;
    internal Operation DecHLMem;
    
    internal Operation LDRegImmU8;
    internal Operation LDHLImmU8;
    
    internal Operation AddMem;
    internal Operation AdcMem;
    internal Operation SubMem;
    internal Operation SbcMem;
    internal Operation AndMem;
    internal Operation XorMem;
    internal Operation OrMem;
    internal Operation CpMem;

    internal Operation LDHImmToA;
    internal Operation LDHAToImm;

    internal Operation AddSPImm8;
    internal Operation LDHSPImm8;
    
    internal Operation PopReg16;
    internal Operation PushReg16;
    
    internal Operation RETUnconditional;
    internal Operation RETI;
    
    internal Operation LDAnn;
    internal Operation LDnnA;

    internal Operation CALLTaken;

    internal Operation ALUImm;
    
    internal Operation RSTPipeline;

    internal Operation CBDispatch;
    
    internal Operation InterruptServicePipeline;
    
    static readonly Action NoOp = () => {};

    public CPUOperations(GameBoy gameboy) {
        this.gameboy = gameboy;

        AddU16RegReg = new Operation(3, [
            new MicroOp(0, () => {
                ushort t_val = Registers.Getters[target_register]();
                ushort s_val = Registers.Getters[source_register]();
        
                int result = t_val + s_val;

                bool halfCarry = ((t_val & 0x0FFF) + (s_val & 0x0FFF)) > 0x0FFF;
                bool carry = result > 0xFFFF;

                Registers.SetFlag(CPUFlagMask.Negative, false);
                Registers.SetFlag(CPUFlagMask.HalfCarry, halfCarry);
                Registers.SetFlag(CPUFlagMask.Carry, carry);

                // Save the final 16-bit value via your property delegate setter
                Registers.Setters[target_register]((ushort)result);
                CPU.FinishOperation();
            })
        ]);

        LDU16SP = new Operation(2, [
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2, 
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.PC++;
                }),
            
            
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(3, 
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.PC++;
                    pointer = buffer;
                }),
            
            
            new MicroOp(3, 
                () => {
                    CPU.WriteMemory(pointer, (byte)(Registers.SP & 0xFF));
                }),
            
            
            new MicroOp(0, 
                () => {
                    CPU.WriteMemory((ushort)(pointer + 1), (byte)(Registers.SP >> 8));
                    CPU.FinishOperation();
                }),
        ]);
        
        LDU16 = new Operation(2, [
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2, 
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.PC++;
                }),
            
            
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(0, 
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.PC++;
                    Registers.Setters[target_register](buffer);
                    CPU.FinishOperation(); 
                }),
            
        ]);
        
        JR = new Operation(2, [
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(3, 
                () => {
                    buffer = CPU.ReadBus();
                    Registers.PC++;
                }),
            
            
            new MicroOp(0, 
                () => {
                    Registers.PC = (ushort)(Registers.PC + (sbyte)buffer);
                    CPU.FinishOperation();
                }),
        ]);
        
        
        JRFailed = new Operation(2, [
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(0,
                () => {
                    buffer = CPU.ReadBus();
                    Registers.PC++;
                    CPU.FinishOperation();
                })
        ]);
                    
        JPFailed = new Operation(2, [
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2, 
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.PC++;
                }),
            
            
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(0, 
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.PC++;
                    CPU.FinishOperation(); 
                }),
        ]);
        
        JPTaken = new Operation(2, [
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2, 
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.PC++;
                }),
            
            
            new MicroOp(0, 
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(3, 
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.PC++;
                    pointer = buffer; 
                }),
            
            
            new MicroOp(0, 
                () => {
                    Registers.PC = pointer; 
                    CPU.FinishOperation(); 
                }),
        ]);

        LDRegFromMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer); 
                }),
            new MicroOp(0,
                () => {
                    Registers.Setters[target_register](CPU.ReadBus());
                    if (hl_mutation == HLMutation.Inc) Registers.HL++;
                    if (hl_mutation == HLMutation.Dec) Registers.HL--;
                    CPU.FinishOperation();
                }),
        ]);
        
        LDMemFromReg = new Operation(3, [
            new MicroOp(0,
                () => {
                    CPU.WriteMemory(pointer, (byte)Registers.Getters[source_register]());
                    if (hl_mutation == HLMutation.Inc) Registers.HL++;
                    if (hl_mutation == HLMutation.Dec) Registers.HL--;
                    CPU.FinishOperation();
                }),
        ]);

        
        LDSPHL = new Operation(3, [
            new MicroOp(0,
                () => {
                    Registers.SP = Registers.HL;
                    CPU.FinishOperation();
                }),
        ]);
        
        IncU16Reg = new Operation(3, [
            new MicroOp(0,
                () => {
                    ushort val = Registers.Getters[target_register]();
                    Registers.Setters[target_register]((ushort)(val + 1));
                    CPU.FinishOperation();
                }),
        ]);
        
        DecU16Reg = new Operation(3, [
            new MicroOp(0,
                () => {
                    ushort val = Registers.Getters[target_register]();
                    Registers.Setters[target_register]((ushort)(val - 1));
                    CPU.FinishOperation();
                }),
        ]);
        
        IncHLMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer);
                }),
            new MicroOp(3,
                () => {
                    buffer = CPU.ReadBus();
                }),
            
            new MicroOp(0,
                () => {
                    IncrementAtAddress();
                    CPU.FinishOperation();
                }),
        ]);
        
        DecHLMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer);
                }),
            new MicroOp(3,
                () => {
                    buffer = CPU.ReadBus();
                }),
            
            new MicroOp(0,
                () => {
                    DecrementAtAddress();
                    CPU.FinishOperation();
                }),
        ]);
        
        LDRegImmU8 = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC); 
                }),
            new MicroOp(0,
                () => {
                    Registers.PC++;
                    Registers.Setters[target_register](CPU.ReadBus());
                    CPU.FinishOperation();
                }),
        ]);
        
        LDHLImmU8 = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC); 
                }),
            new MicroOp(3,
                () => {
                    buffer = CPU.ReadBus();
                    Registers.PC++;
                }),
            new MicroOp(0,
                () => {
                    CPU.WriteMemory(pointer, (byte)buffer);
                    CPU.FinishOperation();
                }),
        ]);
        
        AddMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer); 
                }),
            new MicroOp(0,
                () => {
                    byte a = Registers.A;
                    byte b = CPU.ReadBus();
                    byte result = Add(a, b);
                    Registers.A = result;
                    CPU.FinishOperation();
                }),
        ]);
        
        AdcMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer); 
                }),
            new MicroOp(0,
                () => {
                    byte a = Registers.A;
                    byte b = CPU.ReadBus();
                    byte result = AddWithCarry(a, b);
                    Registers.A = result;
                    CPU.FinishOperation();
                }),
        ]);
        
        SubMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer); 
                }),
            new MicroOp(0,
                () => {
                    byte a = Registers.A;
                    byte b = CPU.ReadBus();
                    byte result = Subtract(a, b);
                    Registers.A = result;
                    CPU.FinishOperation();
                }),
        ]);
        
        SbcMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer); 
                }),
            new MicroOp(0,
                () => {
                    byte a = Registers.A;
                    byte b = CPU.ReadBus();
                    byte result = SubtractWithCarry(a, b);
                    Registers.A = result;
                    CPU.FinishOperation();
                }),
        ]);
        
        AndMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer); 
                }),
            new MicroOp(0,
                () => {
                    byte a = Registers.A;
                    byte b = CPU.ReadBus();
                    byte result = And(a, b);
                    Registers.A = result;
                    CPU.FinishOperation();
                }),
        ]);
        
        XorMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer); 
                }),
            new MicroOp(0,
                () => {
                    byte a = Registers.A;
                    byte b = CPU.ReadBus();
                    byte result = Xor(a, b);
                    Registers.A = result;
                    CPU.FinishOperation();
                }),
        ]);
        
        OrMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer); 
                }),
            new MicroOp(0,
                () => {
                    byte a = Registers.A;
                    byte b = CPU.ReadBus();
                    byte result = Or(a, b);
                    Registers.A = result;
                    CPU.FinishOperation();
                }),
        ]);
        
        CpMem = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer); 
                }),
            new MicroOp(0,
                () => {
                    byte a = Registers.A;
                    byte b = CPU.ReadBus();
                    Compare(a, b);
                    CPU.FinishOperation();
                }),
        ]);
        
        RETTaken = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.SP); 
                }),
            new MicroOp(2,
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.SP++;
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.SP); 
                }),
            new MicroOp(7,
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.SP++;
                }),
            
            new MicroOp(0,
                () => {
                    Registers.PC = buffer; 
                    CPU.FinishOperation();
                }),
        ]);

        RETFailed = new Operation(3, [
            new MicroOp(0,
                () => {
                    CPU.FinishOperation();
                }),
        ]);
        
        LDHImmToA = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2,
                () => {
                    buffer = CPU.ReadBus();
                    Registers.PC++;
                    pointer = (ushort)(0xFF00 + (byte)buffer); 
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer);
                }),
            new MicroOp(0,
                () => {
                    buffer = CPU.ReadBus();
                    Registers.Setters[(int)TargetRegister.A](buffer);
                    CPU.FinishOperation();
                }),
        ]);
        
        LDHAToImm = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(3,
                () => {
                    Registers.PC++;
                    pointer = (ushort)(0xFF00 + CPU.ReadBus()); 
                }),
            
            new MicroOp(0,
                () => {
                    byte a = (byte)Registers.Getters[(int)TargetRegister.A]();
                    CPU.WriteMemory(pointer, a);
                    CPU.FinishOperation();
                }),
        ]);
        
        AddSPImm8 = new Operation(6, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(3,
                () => {
                    buffer = CPU.ReadBus();
                    Registers.PC++;
                }),
            
            new MicroOp(0,
                () => {
                    ushort oldSp = Registers.SP;
                    sbyte offset = (sbyte)buffer;
                
                    int result = oldSp + offset;

                    bool halfCarry = ((oldSp & 0x0F) + (buffer & 0x0F)) > 0x0F;
                    bool carry = ((oldSp & 0xFF) + (buffer & 0xFF)) > 0xFF;

                    Registers.SetFlag(CPUFlagMask.Zero, false);
                    Registers.SetFlag(CPUFlagMask.Negative, false);
                    Registers.SetFlag(CPUFlagMask.HalfCarry, halfCarry);
                    Registers.SetFlag(CPUFlagMask.Carry, carry);

                    Registers.SP = (ushort)result;
                    CPU.FinishOperation();
                }),
        ]);
        
        LDHSPImm8 = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(3,
                () => {
                    buffer = CPU.ReadBus();
                    Registers.PC++;
                }),
            
            new MicroOp(0,
                () => {
                    ushort oldSp = Registers.SP;
                    sbyte offset = (sbyte)buffer; 
                    int result = oldSp + offset;
                
                    bool halfCarry = ((oldSp & 0x0F) + (buffer & 0x0F)) > 0x0F;
                    bool carry = ((oldSp & 0xFF) + (buffer & 0xFF)) > 0xFF;

                    Registers.SetFlag(CPUFlagMask.Zero, false);
                    Registers.SetFlag(CPUFlagMask.Negative, false);
                    Registers.SetFlag(CPUFlagMask.HalfCarry, halfCarry);
                    Registers.SetFlag(CPUFlagMask.Carry, carry);

                    Registers.HL = (ushort)result;
                    CPU.FinishOperation();
                }),
        ]);
        
        PopReg16 = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.SP); 
                }),
            new MicroOp(2,
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.SP++;
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.SP); 
                }),
            new MicroOp(0,
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.SP++;
                
                    CPU.Registers.Setters[target_register](buffer);
                    CPU.FinishOperation();
                }),
        ]);
        
        PushReg16 = new Operation(3, [
            new MicroOp(3,
                () => {
                    buffer = CPU.Registers.Getters[source_register](); 
                }),
            
            new MicroOp(3,
                () => {
                    CPU.WriteMemory(Registers.SP, (byte)(buffer >> 8)); 
                    Registers.SP--;
                }),
            
            new MicroOp(0,
                () => {
                    CPU.WriteMemory(Registers.SP, (byte)(buffer & 0xFF)); 
                    CPU.FinishOperation();
                }),
        ]);
        
        RETUnconditional = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.SP);
                }),
            new MicroOp(2,
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.SP++;
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.SP);
                }),
            new MicroOp(3,
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.SP++;
                }),
            
            new MicroOp(0,
                () => {
                    Registers.PC = buffer; 
                    CPU.FinishOperation();
                }),
        ]);
        
        RETI = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.SP);
                }),
            new MicroOp(2,
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.SP++;
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.SP);
                }),
            new MicroOp(3,
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.SP++;
                }),
            
            new MicroOp(0,
                () => {
                    Registers.PC = buffer; 
                    CPU.interrupt_master_enable = true;
                    CPU._ime_enable_delay = 0;
                    CPU.FinishOperation();
                }),
        ]);
        
        LDAnn = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2,
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.PC++;
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2,
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.PC++;
                    pointer = buffer; 
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(pointer);
                }),
            new MicroOp(0,
                () => {
                    Registers.A = CPU.ReadBus();
                    CPU.FinishOperation();
                }),
        ]);
        
        LDnnA = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2,
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.PC++;
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(3,
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.PC++;
                    pointer = buffer; 
                }),
            
            new MicroOp(0,
                () => {
                    CPU.WriteMemory(pointer, Registers.A);
                    CPU.FinishOperation();
                }),
        ]);
        
        CALLTaken = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2,
                () => {
                    BufferByteLow(CPU.ReadBus());
                    Registers.PC++;
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(4,
                () => {
                    BufferByteHigh(CPU.ReadBus());
                    Registers.PC++;
                    pointer = buffer;
                    return_address = Registers.PC;
                }),
            
            new MicroOp(2,
                () => {
                    Registers.SP--;
                }),
            new MicroOp(0,
                () => {
                    CPU.WriteMemory(Registers.SP, (byte)(return_address>> 8)); 
                }),
            
            new MicroOp(2,
                () => {
                    Registers.SP--;
                }),
            new MicroOp(0,
                () => {
                    CPU.WriteMemory(Registers.SP, (byte)(return_address & 0xFF));
                
                    Registers.PC = pointer;
                    CPU.FinishOperation();
                }),
        ]);


        ALUImm = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC); 
                }),
            new MicroOp(0,
                () => {
                    Registers.PC++;
                
                    byte a = Registers.A;
                    byte immValue = CPU.ReadBus();

                    switch (alu_op) {
                        case 0: Registers.A = Add(a, immValue); break;
                        case 1: Registers.A = AddWithCarry(a, immValue); break;
                        case 2: Registers.A = Subtract(a, immValue); break;
                        case 3: Registers.A = SubtractWithCarry(a, immValue); break;
                        case 4: Registers.A = And(a, immValue); break;
                        case 5: Registers.A = Xor(a, immValue); break;
                        case 6: Registers.A = Or(a, immValue);  break;
                        case 7: Compare(a, immValue); break; 
                    }
        
                    CPU.FinishOperation(); 
                }),
        ]);
        
        RSTPipeline = new Operation(7, [
            new MicroOp(3,
                () => {
                    Registers.SP--;
                    CPU.WriteMemory(Registers.SP, (byte)(Registers.PC >> 8)); 
                }),
            new MicroOp(0,
                () => {
                    Registers.SP--;
                    CPU.WriteMemory(Registers.SP, (byte)(Registers.PC & 0xFF)); 
                
                    Registers.PC = pointer;
                    CPU.FinishOperation();
                }),
        ]);
        
        
        CBDispatch = new Operation(2, [
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.PC);
                }),
            new MicroOp(2,
                () => {
                    Registers.PC++;

                    cb_sub_op = CPU.ReadBus();

                    if ((cb_sub_op & 0x07) != 6){
                        DecodeCbOpcode(cb_sub_op); 
                        CPU.FinishOperation(); 
                    }
                }),
            
            new MicroOp(0,
                () => {
                    CPU.ReadMemory(Registers.HL); 
                }),
            new MicroOp(3,
                () => {
                    int x = (cb_sub_op >> 6) & 0x03;
                    if (x == 1) { // BIT, early exit
                        ExecuteCbMemoryOperation(cb_sub_op, CPU.ReadBus());
                        CPU.FinishOperation(); 
                    }
                }),
            new MicroOp(0,
                () => {
                    byte result = ExecuteCbMemoryOperation(cb_sub_op, CPU.ReadBus());
        
                    if (IsCbBitInstruction(cb_sub_op)) {
                        CPU.FinishOperation();
                        return;
                    }

                    CPU.WriteMemory(Registers.HL, result);
                    CPU.FinishOperation();
                }),
        ]);
    }

    void BufferByteLow(byte value) {
        buffer = value;
    }

    void BufferByteHigh(byte value) {
        buffer |= (ushort)(value << 8);
    }


    private static bool IsCbBitInstruction(byte subOpcode) {
        return (subOpcode >> 6) == 1;
    }
    
    private void DecodeCbOpcode(byte subOpcode) {
        int x = (subOpcode >> 6) & 0x03;
        int y = (subOpcode >> 3) & 0x07;
        int z = subOpcode & 0x07;

        int regId = CPU.R_table(z);
        byte val = (byte)Registers.Getters[regId]();

        switch (x) {
            case 0: // Bit Shifts and Rotations (RLC, RRC, SLA, SRL, SWAP, etc.)
                byte shiftedResult = Shift(y, val);
                Registers.Setters[regId](shiftedResult);
                break;

            case 1: // BIT b, r
                bool isBitZero = (val & (1 << y)) == 0;
                Registers.SetFlag(CPUFlagMask.Zero, isBitZero);
                Registers.SetFlag(CPUFlagMask.Negative, false);
                Registers.SetFlag(CPUFlagMask.HalfCarry, true);
                break;

            case 2: // RES b, r (Force bit 'y' to 0)
                Registers.Setters[regId]((byte)(val & ~(1 << y)));
                break;

            case 3: // SET b, r (Force bit 'y' to 1)
                Registers.Setters[regId]((byte)(val | (1 << y)));
                break;
        }
    }
    
    private byte ExecuteCbMemoryOperation(byte subOpcode, byte memValue) {
        int x = (subOpcode >> 6) & 0x03;
        int y = (subOpcode >> 3) & 0x07;
        int z = subOpcode & 0x07;

        switch (x) {
            case 0: // Shifts / Rotations on RAM byte
                return Shift(y, memValue);

            case 1: // BIT b, (HL)
                bool isBitZero = (memValue & (1 << y)) == 0;
                Registers.SetFlag(CPUFlagMask.Zero, isBitZero);
                Registers.SetFlag(CPUFlagMask.Negative, false);
                Registers.SetFlag(CPUFlagMask.HalfCarry, true);
                return memValue; // Value isn't modified, but pipeline handles the exit flag

            case 2: // RES b, (HL)
                return (byte)(memValue & ~(1 << y));

            case 3: // SET b, (HL)
                return (byte)(memValue | (1 << y));
        }

        return memValue;
    }
    
    private byte Shift(int operation_id, byte val) {
        int result = 0;
        bool current_carry = Registers.GetFlag(CPUFlagMask.Carry);
        bool new_carry = false;

        switch (operation_id) {
            case 0: // RLC
                new_carry = ((val >> 7) & 1) == 1;
                result = (val << 1) | (new_carry ? 1 : 0);
                break;

            case 1: // RRC
                new_carry = (val & 1) == 1;
                result = (val >> 1) | (new_carry ? 0x80 : 0);
                break;

            case 2: // RL
                new_carry = ((val >> 7) & 1) == 1;
                result = (val << 1) | (current_carry ? 1 : 0);
                break;

            case 3: // RR
                new_carry = (val & 1) == 1;
                result = (val >> 1) | (current_carry ? 0x80 : 0);
                break;

            case 4: // SLA
                new_carry = ((val >> 7) & 1) == 1;
                result = val << 1;
                break;

            case 5: // SRA
                new_carry = (val & 1) == 1;
                int sign_bit = val & 0x80;
                result = (val >> 1) | sign_bit;
                break;

            case 6: // SWAP
                result = ((val & 0x0F) << 4) | ((val & 0xF0) >> 4);
                
                Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
                Registers.SetFlag(CPUFlagMask.Negative, false);
                Registers.SetFlag(CPUFlagMask.HalfCarry, false);
                Registers.SetFlag(CPUFlagMask.Carry, false);
                return (byte)result;

            case 7: //
                new_carry = (val & 1) == 1;
                result = val >> 1;
                break;
        }
        
        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, false);
        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
        Registers.SetFlag(CPUFlagMask.Carry, new_carry);

        return (byte)result;
    }
    
    internal byte Add(byte a, byte b) {
        int result = a + b;
                
        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, false);
        Registers.SetFlag(CPUFlagMask.HalfCarry, ((a & 0x0F) + (b & 0x0F)) > 0x0F);
        Registers.SetFlag(CPUFlagMask.Carry, result > 0xFF);

        return (byte)result;
    }
    
    internal byte AddWithCarry(byte register_a, byte register_b) {
        bool carry = Registers.GetFlag(CPUFlagMask.Carry);
        
        byte a = register_a;
        byte b = register_b;

        int result = a + b + (carry ? 1 : 0);
                
        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, false);
        Registers.SetFlag(CPUFlagMask.HalfCarry, ((a & 0x0F) + (b & 0x0F) + (carry ? 1 : 0)) > 0x0F);
        Registers.SetFlag(CPUFlagMask.Carry, result > 0xFF);

        return (byte)result;
    }
    
    internal byte Subtract(byte a, byte b) {
        int result = a - b;
                
        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, true);
        Registers.SetFlag(CPUFlagMask.HalfCarry, (a & 0x0F) < (b & 0x0F));
        Registers.SetFlag(CPUFlagMask.Carry, result < 0x00);

        return (byte)result;
    }
    
    internal byte SubtractWithCarry(byte register_a, byte register_b) {
        bool carry = Registers.GetFlag(CPUFlagMask.Carry);
        
        byte a = register_a;
        byte b = register_b;

        int result = a - b - (carry ? 1 : 0);
                
        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, true);
        Registers.SetFlag(CPUFlagMask.HalfCarry, (a & 0x0F) < (b & 0x0F) + (carry ? 1 : 0));
        Registers.SetFlag(CPUFlagMask.Carry, result < 0x00);

        return (byte)result;
    }
    
    internal void IncrementU8(int register) {
        byte pre_increment = (byte)Registers.Getters[register]();
        int result = pre_increment + 1;

        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, false);
        Registers.SetFlag(CPUFlagMask.HalfCarry, (pre_increment & 0x0F) == 0x0F);
        
        Registers.Setters[register]((byte)result);
    }
    
    internal void DecrementU8(int register) {
        byte pre_decrement =(byte)Registers.Getters[register]();
        int result = pre_decrement - 1;

        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, true);
        Registers.SetFlag(CPUFlagMask.HalfCarry, (pre_decrement & 0x0F) == 0);
        
        Registers.Setters[register]((byte)result);
    }
    
    private void IncrementAtAddress() {
        byte pre_increment = (byte)buffer;
        int result = (pre_increment + 1);

        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, false);
        Registers.SetFlag(CPUFlagMask.HalfCarry, (pre_increment & 0x0F) == 0x0F);
        
        CPU.WriteMemory(pointer, (byte)result);
    }
    
    private void DecrementAtAddress() {
        byte pre_decrement = (byte)buffer;
        int result = (pre_decrement - 1);

        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, true);
        Registers.SetFlag(CPUFlagMask.HalfCarry, (pre_decrement & 0x0F) == 0);
        
        CPU.WriteMemory(pointer, (byte)result);
    }
    
    internal byte And(byte register_a, byte register_b) {
        byte a = register_a;
        byte b = register_b;

        byte result = (byte)(a & b);
        
        Registers.SetFlag(CPUFlagMask.Zero, result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, false);
        Registers.SetFlag(CPUFlagMask.HalfCarry, true);
        Registers.SetFlag(CPUFlagMask.Carry, false);

        return result;
    }
    
    internal byte Xor(byte register_a, byte register_b) {
        byte a = register_a;
        byte b = register_b;

        byte result = (byte)(a ^ b);
        
        Registers.SetFlag(CPUFlagMask.Zero, result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, false);
        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
        Registers.SetFlag(CPUFlagMask.Carry, false);

        return result;
    }
    
    internal byte Or(byte register_a, byte register_b) {
        byte a = register_a;
        byte b = register_b;

        byte result = (byte)(a | b);
        
        Registers.SetFlag(CPUFlagMask.Zero, result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, false);
        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
        Registers.SetFlag(CPUFlagMask.Carry, false);

        return result;
    }

    internal void Compare(byte register_a, byte register_b) {
        int result = register_a - register_b;
                
        Registers.SetFlag(CPUFlagMask.Zero, (byte)result == 0);
        Registers.SetFlag(CPUFlagMask.Negative, true);
        Registers.SetFlag(CPUFlagMask.HalfCarry, (register_a & 0x0F) < (register_b & 0x0F));
        Registers.SetFlag(CPUFlagMask.Carry, result < 0x00);

    }
    
}