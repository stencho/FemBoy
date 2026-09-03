using System;

namespace FemBoy;

public class CPUOperations {
    private GameBoy gameboy;
    private CPU CPU => gameboy.CPU;
    private CPURegisters Registers => CPU.Registers;
    
    internal Action[]? current_operation;
    
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
    
    internal Action[] AddU16RegReg;

    internal Action[] LDU16SP;
    
    internal Action[] JR;

    internal Action[] RETTaken;
    internal Action[] RETFailed;
    
    internal Action[] JRFailed;
    internal Action[] JRTaken;

    internal Action[] LDU16;
    internal Action[] LDSPHL;
    
    internal Action[] JPFailed;
    internal Action[] JPTaken;

    internal Action[] LDRegFromMem;
    internal Action[] LDMemFromReg;
    
    internal Action[] IncU16Reg;
    internal Action[] DecU16Reg;

    internal Action[] IncHLMem;
    internal Action[] DecHLMem;
    
    internal Action[] LDRegImmU8;
    internal Action[] LDHLImmU8;
    
    internal Action[] AddMem;
    internal Action[] AdcMem;
    internal Action[] SubMem;
    internal Action[] SbcMem;
    internal Action[] AndMem;
    internal Action[] XorMem;
    internal Action[] OrMem;
    internal Action[] CpMem;

    internal Action[] LDHImmToA;
    internal Action[] LDHAToImm;

    internal Action[] AddSPImm8;
    internal Action[] LDHSPImm8;
    
    internal Action[] PopReg16;
    internal Action[] PushReg16;
    
    internal Action[] RetUnconditional;
    internal Action[] RetI;
    
    internal Action[] LDnnA;
    internal Action[] LDAnn;

    internal Action[] CALLTaken;

    internal Action[] ALUImm;
    
    internal Action[] RSTPipeline;

    internal Action[] CBMemory;
    internal Action[] CBRegister;
    
    internal Action[] InterruptServicePipeline;
    

    public CPUOperations(GameBoy gameboy) {
        this.gameboy = gameboy;
        
        AddU16RegReg = [
            //M2
            () => { }, () => { }, () => { },
            () => {
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
            }
        ];
        
        LDU16SP = [
            //M2
            () => { }, () => { },
            () => {
                BufferByteLow(CPU.ReadMemory(Registers.PC++));
            },
            () => { },
            //M3
            () => { }, () => { }, 
            () => {
                BufferByteHigh(CPU.ReadMemory(Registers.PC++));
            },
            () => {
                pointer = buffer;
            },
            //M4
            () => { }, () => { }, () => { },
            () => {
                CPU.WriteMemory(pointer, (byte)(Registers.SP & 0xFF));
            },
            //M5
            () => { }, () => { }, () => { },
            () => {
                CPU.WriteMemory((ushort)(pointer + 1), (byte)(Registers.SP >> 8));
                
                CPU.FinishOperation();
            },
        ];

        LDU16 = [
            //M2
            () => { }, () => { } , 
            () => {
                BufferByteLow(CPU.ReadMemory(Registers.PC++));
            }, 
            () => { },
            //M3
            () => { }, () => { } , 
            () => {
                BufferByteHigh(CPU.ReadMemory(Registers.PC++));
            }, 
            () => {
                Registers.Setters[target_register](buffer);
                CPU.FinishOperation(); 
            }
        ];
        
        JR = [
            //M2
            () => { }, () => { },
            () => {
                buffer = CPU.ReadMemory(Registers.PC++);
            },
            () => { },
            //M3
            () => { }, () => { }, () => { },
            () => {
                Registers.PC = (ushort)(Registers.PC + (sbyte)buffer);
                
                CPU.FinishOperation();
            },
        ];

        JRFailed = [
            () => { }, () => { }, () => {
                buffer = CPU.ReadMemory(Registers.PC++);
            },
            () => {
                CPU.FinishOperation();
            }
        ];
        
        JRTaken = [
            //M2
            () => { }, () => { }, 
            () => { buffer = CPU.ReadMemory(Registers.PC++); }, 
            () => {Registers.PC = (ushort)(Registers.PC + (sbyte)buffer); },
            //M3
            () => { }, () => { }, () => { },
            () => {
                CPU.FinishOperation();
            }, 
        ];
        
        JPFailed = [
            //M2
            () => { }, () => { },
            () => { BufferByteLow(CPU.ReadMemory(Registers.PC++)); }, 
            () => { },
            //M3
            () => { }, () => { },
            () => {
                BufferByteHigh(CPU.ReadMemory(Registers.PC++));
            },
            () => {
                CPU.FinishOperation(); 
            }
        ];
        
        
        JPTaken = [
            //M2
            () => { }, () => { },
            () => {
                BufferByteLow(CPU.ReadMemory(Registers.PC++));
            },
            () => { },
            //M3
            () => { }, () => { },
            () => {
                BufferByteHigh(CPU.ReadMemory(Registers.PC++));
            }, 
            () => { 
                pointer = buffer; 
            },
            //M4
            () => { }, () => { }, () => { }, 
            () => { 
                Registers.PC = pointer; 
                CPU.FinishOperation(); 
            }
        ];
        
        LDRegFromMem = [
            () => { }, () => { }, 
            () => { 
                buffer = CPU.ReadMemory(pointer); 
            },
            () => { 
                Registers.Setters[target_register](buffer);
                if (hl_mutation == HLMutation.Inc) Registers.HL++;
                if (hl_mutation == HLMutation.Dec) Registers.HL--;
                CPU.FinishOperation();
            }
        ];

        LDMemFromReg = [
            () => { }, () => { }, () => { }, 
            () => { 
                CPU.WriteMemory(pointer, (byte)Registers.Getters[source_register]());
                if (hl_mutation == HLMutation.Inc) Registers.HL++;
                if (hl_mutation == HLMutation.Dec) Registers.HL--;
                CPU.FinishOperation();
            }
        ];
        
        LDSPHL = [
            () => { }, () => { }, () => { }, 
            () => {    
                Registers.SP = Registers.HL;
                CPU.FinishOperation();
            }
        ];
        
        IncU16Reg = [
            () => { }, () => { }, () => { }, 
            () => {   
                ushort val = Registers.Getters[target_register]();
                Registers.Setters[target_register]((ushort)(val + 1));
                CPU.FinishOperation();
            }
        ];

        DecU16Reg = [
            () => { }, () => { }, () => { }, 
            () => {    
                ushort val = Registers.Getters[target_register]();
                Registers.Setters[target_register]((ushort)(val - 1));
                CPU.FinishOperation();
            }
        ];
        
        IncHLMem = [
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => { },

            () => { }, () => { }, () => { },
            () => {   
                IncrementAtAddress();
                CPU.FinishOperation();
            }
        ];

        DecHLMem = [
            () => { }, () => { }, 
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => { },
            
            () => { }, () => { }, () => { },
            () => {   
                DecrementAtAddress();
                CPU.FinishOperation();
            }
        ];
        
        LDRegImmU8 = [
            () => { }, () => { },
            () => { 
                buffer = CPU.ReadMemory(Registers.PC++); 
            },
            () => { 
                Registers.Setters[target_register](buffer);
                CPU.FinishOperation();
            }
        ];

        LDHLImmU8 = [
            () => { }, () => { },
            () => {
                buffer = CPU.ReadMemory(Registers.PC++);
            }, 
            () => { },
    
            () => { }, () => { }, () => { },
            () => {   
                CPU.WriteMemory(pointer, (byte)buffer);
                CPU.FinishOperation();
            }
        ];
        
        AddMem = [
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => {
                byte a = Registers.A;
                byte b = (byte)buffer;
                byte result = Add(a, b);
                Registers.A = result;
                CPU.FinishOperation();
            }
        ];
        
        AdcMem = [
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => {
                byte a = Registers.A;
                byte b = (byte)buffer;
                byte result = AddWithCarry(a, b);
                Registers.A = result;
                CPU.FinishOperation();
            }
        ];
        
        SubMem = [
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => {
                byte a = Registers.A;
                byte b = (byte)buffer;
                byte result = Subtract(a, b);
                Registers.A = result;
                CPU.FinishOperation();
            }
        ];
        
        SbcMem = [
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => {
                byte a = Registers.A;
                byte b = (byte)buffer;
                byte result = SubtractWithCarry(a, b);
                Registers.A = result;
                CPU.FinishOperation();
            }
        ];
        
        AndMem = [
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => {
                byte a = Registers.A;
                byte b = (byte)buffer;
                byte result = And(a, b);
                Registers.A = result;
                CPU.FinishOperation();
            }
        ];
        
        XorMem = [
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => {
                byte a = Registers.A;
                byte b = (byte)buffer;
                byte result = Xor(a, b);
                Registers.A = result;
                CPU.FinishOperation();
            }
        ];
        
        OrMem = [
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => {
                byte a = Registers.A;
                byte b = (byte)buffer;
                byte result = Or(a, b);
                Registers.A = result;
                CPU.FinishOperation();
            }
        ];
        
        CpMem = [
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(pointer); }, 
            () => {
                byte a = Registers.A;
                byte b = (byte)buffer;
                Compare(a, b);
                CPU.FinishOperation();
            }
        ];
        
        RETTaken = [
            //M2
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(Registers.SP++); }, 
            () => { },
    
            //M3
            () => { }, () => { },
            () => { 
                buffer |= (ushort)(CPU.ReadMemory(Registers.SP++) << 8); 
            }, 
            () => { },

            //M4
            () => { }, () => { }, () => { }, () => { },
            
            //M5
            () => { }, () => { }, () => { }, 
            () => {
                Registers.PC = buffer; 
                CPU.FinishOperation();
            }
        ];
        
        RETFailed = [
            () => { }, () => { }, () => { },
            () => { CPU.FinishOperation(); }
        ];
        
        LDHImmToA = [
            //M2
            () => { }, () => { },
            () => {
                buffer = CPU.ReadMemory(Registers.PC++);
            },
            () => { 
                pointer = (ushort)(0xFF00 + (byte)buffer); 
            },

            //M3
            () => { }, () => { },
            () => {
                buffer = CPU.ReadMemory(pointer);
            }, 
            () => { 
                Registers.Setters[(int)TargetRegister.A](buffer);
                CPU.FinishOperation();
            }
        ];

        LDHAToImm = [
            //M2
            () => { }, () => { },
            () => {
                buffer = CPU.ReadMemory(Registers.PC++);
            }, 
            () => { 
                pointer = (ushort)(0xFF00 + (byte)buffer); 
            },

            //M3
            () => { }, () => { }, () => { },
            () => { 
                
                byte a = (byte)Registers.Getters[(int)TargetRegister.A]();
                CPU.WriteMemory(pointer, a);
                CPU.FinishOperation();
            }
        ];
        
        AddSPImm8 = [
            //M2
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(Registers.PC++); }, 
            () => { },

            //M3
            () => { }, () => { }, () => { },
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
            }
        ];
        
        LDHSPImm8 = [
            //M2
            () => { }, () => { },
            () => { buffer = CPU.ReadMemory(Registers.PC++); },
            () => { },

            //M3
            () => { }, () => { }, () => { },
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
            }
        ];
        
        PopReg16 = [
            //M2
            () => { }, () => { },
            () => { BufferByteLow(CPU.ReadMemory(Registers.SP++)); },
            () => { },
    
            //M3
            () => { }, () => { },
            () => { BufferByteHigh(CPU.ReadMemory(Registers.SP++)); },
            () => {
                CPU.Registers.Setters[target_register](buffer);
                //if (target_register == TargetRegister.AF) CPU.wants_pause = true; 
                CPU.FinishOperation();
            }
        ];
        
        PushReg16 = [
            //M2
            () => { }, () => { }, () => { }, 
            () => {
                buffer = CPU.Registers.Getters[source_register](); 
            },
    
            //M3
            () => { }, () => { }, () => { },
            () => { 
                Registers.SP--;
                CPU.WriteMemory(Registers.SP, (byte)(buffer >> 8)); 
            },
    
            //M4
            () => { }, () => { }, () => { },
            () => { 
                Registers.SP--;
                CPU.WriteMemory(Registers.SP, (byte)(buffer & 0xFF)); 
                CPU.FinishOperation();
            }
        ];
        
        RetUnconditional = [
            //M2
            () => { }, 
            () => { },
            () => {
                BufferByteLow(CPU.ReadMemory(Registers.SP++)); 
            }, 
            () => { },
    
            //M3
            () => { }, 
            () => { },
            () => {
                BufferByteHigh(CPU.ReadMemory(Registers.SP++)); 
            }, 
            () => { },

            //M4
            () => { },
            () => { },
            () => { },
            () => {   
                Registers.PC = buffer; 
                CPU.FinishOperation();
            }
        ];
        
        RetI = [
            //M2
            () => { }, 
            () => { },
            () => {
                BufferByteLow(CPU.ReadMemory(Registers.SP++)); 
            }, 
            () => { },
    
            //M3
            () => { }, 
            () => { },
            () => {
                BufferByteHigh(CPU.ReadMemory(Registers.SP++)); 
            }, 
            () => { },

            //M4
            () => { },
            () => { },
            () => { },
            () => {   
                Registers.PC = buffer; 
                CPU.interrupt_master_enable = true;
                CPU._ime_enable_requested = false;
                CPU.FinishOperation();
            }
        ];
        
        LDnnA = [
            //M2
            () => { }, () => { },
            () => {
                BufferByteLow(CPU.ReadMemory(Registers.PC++));
            },
            () => { },

            //M3
            () => { }, () => { },
            () => {
                BufferByteHigh(CPU.ReadMemory(Registers.PC++));
            },
            () => {
                pointer = buffer; 
            },

            //M4
            () => { }, () => { },
            () => {
                buffer = CPU.ReadMemory(pointer);
            }, 
            () => {
                Registers.A = (byte)buffer;
                CPU.FinishOperation();
            }
        ];

        LDAnn = [
            //M2
            () => { }, () => { },
            () => {
                BufferByteLow(CPU.ReadMemory(Registers.PC++));
            },
            () => { },

            //M3
            () => { }, () => { },
            () => {
                BufferByteHigh(CPU.ReadMemory(Registers.PC++));
            },
            () => { 
                pointer = buffer; 
            },

            //M4
            () => { }, () => { }, () => { },
            () => { 
                CPU.WriteMemory(pointer, Registers.A);
                CPU.FinishOperation();
            }
        ];

        CALLTaken = [
            //M2
            () => { }, () => { },
            () => { BufferByteLow(CPU.ReadMemory(Registers.PC++)); }, 
            () => { },

            //M3
            () => { }, () => { },
            () => { BufferByteHigh(CPU.ReadMemory(Registers.PC++)); }, 
            () => {
                pointer = buffer;
                return_address = Registers.PC;
            },

            //M4
            () => { }, () => { }, () => { }, () => { },

            //M5
            () => {Registers.SP--; }, () => { }, () => { },
            () => { 
                
                CPU.WriteMemory(Registers.SP, (byte)(return_address>> 8)); 
            },

            //M6
            () => { Registers.SP--;}, () => { }, () => { },
            () => { 
                CPU.WriteMemory(Registers.SP, (byte)(return_address & 0xFF));
                
                Registers.PC = pointer;
                CPU.FinishOperation();
            }
        ];
        
        ALUImm = [
            //M2
            () => { }, () => { }, 
            () => { 
                buffer = CPU.ReadMemory(Registers.PC++); 
            },
            () => {  
                byte a = Registers.A;
                byte immValue = (byte)buffer;

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
            }
        ];
        
        RSTPipeline = [
            //M2
            () => { }, () => { }, () => { }, () => { },

            //M3
            () => { }, () => { }, () => { },
            () => { 
                Registers.SP--;
                CPU.WriteMemory(Registers.SP, (byte)(Registers.PC >> 8)); 
            },

            //M4
            () => { }, () => { }, () => { },
            () => { 
                Registers.SP--;
                CPU.WriteMemory(Registers.SP, (byte)(Registers.PC & 0xFF)); 
                
                Registers.PC = pointer;
                CPU.FinishOperation();
            }
        ];
        
        CBRegister = [
            //M2
            () => { }, () => { }, 
            () => { 
                buffer = CPU.ReadMemory(Registers.PC++); 
            },
            () => { 
                DecodeCbOpcode((byte)buffer); 
                CPU.FinishOperation(); 
            }
        ];
        
        CBMemory = [
            //M2
            () => { }, () => { },
            () => {
                buffer = CPU.ReadMemory(Registers.PC++);
            },
            () => {
                cb_sub_op = (byte)buffer; 
            },

            //M3
            () => { }, () => { }, 
            () => {
                buffer = CPU.ReadMemory(Registers.HL); 
            },
            () => { 
                int x = (cb_sub_op >> 6) & 0x03;
                if (x == 1) { // BIT, early exit
                    ExecuteCbMemoryOperation(cb_sub_op, (byte)buffer);
                    CPU.FinishOperation(); 
                }},

            //M4
            () => { }, () => { }, () => { },
            () => {
                byte result = ExecuteCbMemoryOperation(cb_sub_op, (byte)buffer);
        
                if (IsCbBitInstruction(cb_sub_op)) {
                    CPU.FinishOperation();
                    return;
                }

                CPU.WriteMemory(Registers.HL, result);
                CPU.FinishOperation();
            }
        ];
        
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