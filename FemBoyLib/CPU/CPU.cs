using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FemBoy;

public static class InterruptRegisterAddresses {
    public const ushort IF = 0xFF0F;
    public const ushort IE = 0xFFFF;
}

public enum InterruptMask : byte {
    VBlank = 0x01,
    LCD = 0x02,
    Timer = 0x04,
    Serial = 0x08,
    Joypad = 0x10
}

public class CPU {
    internal bool _stopped = false;
    internal bool _halted =  false;

    private bool _halt_bug = false;
    
    public bool Stopped => _stopped;
    public bool Halted =>  _halted;

    public CPURegisters Registers;
    private CPUOperations Operations;
    
    public bool interrupt_master_enable = false;
    internal bool _ime_enable_requested = false;
    
    public bool InterruptRequested(InterruptMask interrupt) => (Registers.IE & Registers.IF & (byte)interrupt) != 0;
    public bool InterruptPending => (Registers.IE & Registers.IF & 0x1F) != 0;
    
    internal byte t_cycle = 0;
    public uint ops = 0;
    private byte current_opcode = 0;
    
    private bool executing_opcode = false;

    public bool wants_pause = false;
    
    private GameBoy gameboy;

    private InterruptMask current_interrupt;
    
    public CPU(GameBoy gameboy) {
        this.gameboy = gameboy;
        Registers = new CPURegisters(gameboy);
        Operations = new CPUOperations(gameboy);
        
        Operations.InterruptServicePipeline = [
            () => { }, () => { },
            () => {
                if      (InterruptRequested(InterruptMask.VBlank)) current_interrupt = InterruptMask.VBlank;
                else if (InterruptRequested(InterruptMask.LCD)) current_interrupt = InterruptMask.LCD;
                else if (InterruptRequested(InterruptMask.Timer)) current_interrupt = InterruptMask.Timer;
                else if (InterruptRequested(InterruptMask.Serial)) current_interrupt = InterruptMask.Serial;
                else if (InterruptRequested(InterruptMask.Joypad)) current_interrupt = InterruptMask.Joypad;
                else {
                    Registers.PC = 0x0000;
                    interrupt_master_enable = false;
                    return;
                }
                
                interrupt_master_enable = false;
                Registers.IF &= (byte)~(byte)current_interrupt;
            },
            () => { }, 
            
            () => { }, () => { }, () => { }, () => { }, 
            
            () => { }, () => { }, () => { },
            () => { 
                Registers.SP--;
                WriteMemory(Registers.SP, (byte)(Registers.PC >> 8)); 
            },
            
            () => { }, () => { }, () => { },
            () => { 
                Registers.SP--;
                WriteMemory(Registers.SP, (byte)(Registers.PC & 0xFF)); 
            },

            
            () => { }, () => { }, () => { },
            () => { 
                Registers.PC = current_interrupt switch {
                    InterruptMask.VBlank => 0x0040,
                    InterruptMask.LCD    => 0x0048,
                    InterruptMask.Timer  => 0x0050,
                    InterruptMask.Serial => 0x0058,
                    InterruptMask.Joypad => 0x0060,
                    _                    => 0x0000
                };
                
                FinishOperation();
            }
        ];
    }

    internal byte ReadMemory(ushort address) {
        // can only access HRAM during DMA transfer
        if (gameboy.DMA.Active && (address < 0xFF80 || address > 0xFFFE)) return 0xFF; 
        
        // cannot access VRAM during PPU mode 3
        if (address >= 0x8000 && address <= 0x9FFF && gameboy.PPU.Mode == (PPUMode)3) return 0xFF;
        
        // cannot access OAM during PPU mode 2 or 3
        if (gameboy.PPU.LCDEnabled && address >= 0xFE00 && address <= 0xFE9F) {
            if (gameboy.PPU.Mode == PPUMode.OAM_SEARCH_2 || gameboy.PPU.Mode == PPUMode.LCD_TRANSFER_3) return 0xFF;
        }

        return gameboy.ReadMemory(address);
    }

    internal void WriteMemory(ushort address, byte value) {
        // Same lockouts as above
        if (gameboy.DMA.Active && (address < 0xFF80 || address > 0xFFFE)) return;

        switch (gameboy.PPU.Mode) {
            case PPUMode.OAM_SEARCH_2:
                if (address is >= 0xFE00 and <= 0xFE9F) return; //OAM
                break;
            case PPUMode.LCD_TRANSFER_3: {
                if (address is >= 0x8000 and <= 0x9FFF) return; //VRAM
                if (address is >= 0xFE00 and <= 0xFE9F) return; //OAM
                break;
            }
        }
        
        gameboy.WriteMemory(address, value);
    }
    
    public void RequestInterrupt(InterruptMask interrupt) {
        Registers.IF |= (byte)interrupt;
    }
    

    public ConcurrentQueue<OpcodeInfo> LastNOpcodes = new();
    private int track_n_opcodes = 50;
    public bool track_opcodes = true;
    private uint last_op_total_cycles = 0;
    private uint cycles_since_last_op = 0;

    private OpcodeInfo current_op;

    
    private StreamWriter? trace;

    public void StartTrace(string path)
    {
        trace?.Dispose();
        trace = new StreamWriter(path, false);
        trace.AutoFlush = false;
    }

    public void StopTrace()
    {
        trace?.Flush();
        trace?.Dispose();
        trace = null;
    }
    
    public void Tick() {
        if (_halted) {
            if (!InterruptPending) return;
            _halted = false;
        }

        if (t_cycle % 4 == 0 && !executing_opcode && interrupt_master_enable && InterruptPending) {
            t_cycle = 0;
            Operations.current_operation = Operations.InterruptServicePipeline;
            executing_opcode = true;
        }
        
        if (!executing_opcode) {
            OpcodeFetch();
            return;
        }
        
        ExecuteInstruction();
    }

    void OpcodeFetch() {
        switch (t_cycle) {
            case 0: break; // Stabilize address lines
            case 1: break; // Open read gates
            case 2:        // Sample opcode
                current_opcode = ReadMemory(Registers.PC);
                if (_halt_bug) _halt_bug = false;
                else Registers.PC++;
                
                break; 
            case 3:        // Execute instruction
                if (_ime_enable_requested) {
                    _ime_enable_requested = false;
                    interrupt_master_enable = true;
                }

                DecodeAndBuildExecutionPipeline(current_opcode); //Mansell decoding function
                return;
        }

        t_cycle++;
    }

    void ExecuteInstruction() {
        if (Operations.current_operation != null && t_cycle < Operations.current_operation.Length) {
            Operations.current_operation[t_cycle++]();
            return;
        }

        Debugger.Break();
    }

    public void FinishOperation() {
        Operations.current_operation = null;
        executing_opcode = false;

        current_op.cycles = (uint)(t_cycle+4);
        
        ops++;
        t_cycle = 0;
        
        Operations.hl_mutation = CPUOperations.HLMutation.None;
    }

    
    // Use Scott Mansell's opcode decoding method
    void DecodeAndBuildExecutionPipeline(byte opcode) {
        Operations.buffer = 0;
        Operations.pointer = 0;
        
        
        executing_opcode = true;
        t_cycle = 0;
        
        int x = (opcode >> 6) & 0x03;
        int y = (opcode >> 3) & 0x07;
        int z = (opcode & 0x07);

        int p = y >> 1;
        int q = y % 2;
        
        current_op = new OpcodeInfo(current_opcode);
        current_op.PC = (ushort)(Registers.PC - 1);
        current_op.SP_before = Registers.SP;
                
        if (track_opcodes) {
            LastNOpcodes.Enqueue(current_op);
            if (LastNOpcodes.Count > track_n_opcodes) LastNOpcodes.TryDequeue(out _);
        }
        
        
        if (x == 0) {
            if (z == 0) { // Relative jumps, assorted ops
                if (y == 0)  // NOP
                    FinishOperation();
                else if (y == 1) // LD u16, SP 
                    Operations.current_operation = Operations.LDU16SP;
                else if (y == 2) {
                    // STOP
                    _stopped = true;
                    FinishOperation();
                } else if (y == 3) // JR u8
                    Operations.current_operation = Operations.JR;
                else if (y is >= 4 and <= 7) // JR cc[y-4],d
                    JR_cc(y-4);
                
            } else if (z == 1) { // 16-bit load immediate/add
                if (q == 0) { // LD rp[p],u16
                    Operations.target_register = RP_table(p);
                    Operations.current_operation = Operations.LDU16;
                }
                else if (q == 1) { // ADD HL, rp[p]
                    Operations.target_register = TargetRegister.HL;
                    Operations.source_register = RP_table(p);
                    Operations.current_operation = Operations.AddU16RegReg;
                }
                
            } else if (z == 2) { // Indirect loading
                if (q == 0) { // LD rp[p], A
                    Operations.source_register = TargetRegister.A;
                    Operations.pointer = Registers.Getters[RP_table_indirect_load(p)]();
                    if (p == 2)      Operations.hl_mutation = CPUOperations.HLMutation.Inc;
                    else if (p == 3) Operations.hl_mutation = CPUOperations.HLMutation.Dec;
                    else             Operations.hl_mutation = CPUOperations.HLMutation.None;
                    Operations.current_operation = Operations.LDMemFromReg;
                } 
                else if (q == 1) { // LD A, rp[p]
                    Operations.target_register = TargetRegister.A;
                    Operations.pointer = Registers.Getters[RP_table_indirect_load(p)]();
                    if (p == 2)      Operations.hl_mutation = CPUOperations.HLMutation.Inc;
                    else if (p == 3) Operations.hl_mutation = CPUOperations.HLMutation.Dec;
                    else             Operations.hl_mutation = CPUOperations.HLMutation.None;
                    Operations.current_operation = Operations.LDRegFromMem;
                }
                
            } else if (z == 3) { // 16-bit INC/DEC
                Operations.target_register = RP_table(p);
                
                if (q == 0) { // INC rp[p], u16
                    Operations.current_operation = Operations.IncU16Reg;
                } else if (q == 1) { // INC rp[p], u16
                    Operations.current_operation = Operations.DecU16Reg;
                }
                
            } else if (z == 4) { // 8 bit INC
                if (y == 6) {
                    Operations.pointer = Registers.HL;
                    Operations.current_operation = Operations.IncHLMem;
                } else {
                    Operations.IncrementU8(R_table(y));
                    FinishOperation();
                }
                
            } else if (z == 5) { // 8 bit DEC
                if (y == 6) {
                    Operations.pointer = Registers.HL;
                    Operations.current_operation = Operations.DecHLMem;
                } else {
                    Operations.DecrementU8(R_table(y));
                    FinishOperation();
                }
                
            } else if (z == 6) { // 8-bit load immediate
                if (y == 6) {
                    Operations.pointer = Registers.HL;
                    Operations.current_operation = Operations.LDHLImmU8;
                } else {
                    Operations.target_register = R_table(y);
                    Operations.current_operation = Operations.LDRegImmU8;
                }

            } else if (z == 7) { // Assorted accumulator/flag ops
                byte a = Registers.A;
                
                switch (y) {
                    case 0: // RLCA 
                        int bit7_rlca = (a >> 7) & 1;
                        Registers.A = (byte)((a << 1) | bit7_rlca);
                        Registers.SetFlag(CPUFlagMask.Zero, false); 
                        Registers.SetFlag(CPUFlagMask.Negative, false);
                        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
                        Registers.SetFlag(CPUFlagMask.Carry, bit7_rlca == 1);
                        break;

                    case 1: // RRCA
                        int bit0_rrca = a & 1;
                        Registers.A = (byte)((a >> 1) | (bit0_rrca << 7));
                        Registers.SetFlag(CPUFlagMask.Zero, false); 
                        Registers.SetFlag(CPUFlagMask.Negative, false);
                        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
                        Registers.SetFlag(CPUFlagMask.Carry, bit0_rrca == 1);
                        break;

                    case 2: // RLA
                        int old_c_rla = Registers.GetFlag(CPUFlagMask.Carry) ? 1 : 0;
                        int new_c_rla = (a >> 7) & 1;
                        Registers.A = (byte)((a << 1) | old_c_rla);
                        Registers.SetFlag(CPUFlagMask.Zero, false);
                        Registers.SetFlag(CPUFlagMask.Negative, false);
                        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
                        Registers.SetFlag(CPUFlagMask.Carry, new_c_rla == 1);
                        break;

                    case 3: // RRA
                        int old_c_rra = Registers.GetFlag(CPUFlagMask.Carry) ? 1 : 0;
                        int new_c_rra = a & 1;
                        Registers.A = (byte)((a >> 1) | (old_c_rra << 7));
                        Registers.SetFlag(CPUFlagMask.Zero, false);
                        Registers.SetFlag(CPUFlagMask.Negative, false);
                        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
                        Registers.SetFlag(CPUFlagMask.Carry, new_c_rra == 1);
                        break;

                    case 4: DAA(); break; // DAA

                    case 5: // CPL
                        Registers.A = (byte)(~Registers.A);
                        Registers.SetFlag(CPUFlagMask.Negative, true);
                        Registers.SetFlag(CPUFlagMask.HalfCarry, true);
                        break;

                    case 6: // SCF
                        Registers.SetFlag(CPUFlagMask.Negative, false);
                        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
                        Registers.SetFlag(CPUFlagMask.Carry, true);
                        break;

                    case 7: // CCF 
                        bool current_carry = Registers.GetFlag(CPUFlagMask.Carry);
                        Registers.SetFlag(CPUFlagMask.Negative, false);
                        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
                        Registers.SetFlag(CPUFlagMask.Carry, !current_carry);
                        break;
                }

                FinishOperation();
            }
            
        } else if (x == 1) {
            if (z == 6 && y == 6) {
                if (!interrupt_master_enable) {
                    if (InterruptPending) {
                        _halted = false;
                        _halt_bug = true;
                    } else {
                        _halted = true;
                    }
                } else {
                    _halted = true;
                }
                
                FinishOperation();
                
            } else {
                if (z == 6) { // LD r, (HL)
                    Operations.target_register = R_table(y);
                    Operations.pointer = Registers.HL;
                    Operations.current_operation = Operations.LDRegFromMem;
                } 
                else if (y == 6) { // LD (HL), r
                    Operations.source_register = R_table(z);
                    Operations.pointer = Registers.HL;
                    Operations.current_operation = Operations.LDMemFromReg;
                } 
                else { // LD r, r
                    int source = R_table(z);
                    int target = R_table(y);
        
                    byte val = (byte)Registers.Getters[source]();
                    Registers.Setters[target](val);
        
                    FinishOperation();
                }
            }
            
        } else if (x == 2) {
            if (z == 6) {
                Operations.pointer = Registers.HL;
                Operations.current_operation = y switch {
                    0 => Operations.AddMem, 1 => Operations.AdcMem,
                    2 => Operations.SubMem, 3 => Operations.SbcMem,
                    4 => Operations.AndMem, 5 => Operations.XorMem,
                    6 => Operations.OrMem,  7 => Operations.CpMem,
                    _ => null
                };
            } else {
                byte a = Registers.A;
                byte b = (byte)Registers.Getters[R_table(z)]();
        
                switch (y) {
                    case 0: Registers.A = Operations.Add(a, b); break;
                    case 1: Registers.A = Operations.AddWithCarry(a, b); break;
                    case 2: Registers.A = Operations.Subtract(a, b); break;
                    case 3: Registers.A = Operations.SubtractWithCarry(a, b); break;
                    case 4: Registers.A = Operations.And(a, b); break;
                    case 5: Registers.A = Operations.Xor(a, b); break;
                    case 6: Registers.A = Operations.Or(a, b);  break;
                    case 7: Operations.Compare(a, b); break;
                }
        
                FinishOperation();
            }
            
        } else if (x == 3) {
            if (z == 0) {
                if (y <= 3) RET_cc(y); // RET cc[y]
                else if (y == 4) // LD ($FF00 + u8), A
                    Operations.current_operation = Operations.LDHAToImm;
                else if (y == 5) // ADD SP,u8
                    Operations.current_operation = Operations.AddSPImm8;
                else if (y == 6) // LD A, ($FF00 + u8)
                    Operations.current_operation = Operations.LDHImmToA;
                else if (y == 7) // LD HL, SP+u8
                    Operations.current_operation = Operations.LDHSPImm8;
                
            } else if (z == 1) {
                if (q == 0) { // POP rp2[p]
                    Operations.target_register = RP2_table(p);
                    Operations.current_operation = Operations.PopReg16;
                    
                } else {
                    if (p == 0) { // RET
                        Operations.current_operation = Operations.RetUnconditional;

                    } else if (p == 1) { // RETI
                        Operations.current_operation = Operations.RetI;
                        
                    } else if (p == 2) { // JP HL
                        Registers.PC = Registers.HL;
                        FinishOperation();
                        
                    } else if (p == 3) {
                        Operations.current_operation = Operations.LDSPHL;
                    }
                }
                
            } else if (z == 2) { //Conditional jump
                if (y <= 3) JP_cc(y); // JP cc[y],u16
                else if (y == 4) { // LD ($FF00 + C), A
                    Operations.hl_mutation = CPUOperations.HLMutation.None;
                    Operations.source_register = TargetRegister.A;
                    Operations.pointer = (ushort)(0xFF00 + Registers.C);
                    Operations.current_operation = Operations.LDMemFromReg;
                    
                } else if (y == 5) { // LD (nn), A
                    Operations.current_operation = Operations.LDAnn;

                } else if (y == 6) { // LD A, ($FF00 + C)
                    Operations.hl_mutation = CPUOperations.HLMutation.None;
                    Operations.target_register = TargetRegister.A;
                    Operations.pointer = (ushort)(0xFF00 + Registers.C);
                    Operations.current_operation = Operations.LDRegFromMem;
                    
                } else if (y == 7) { // LD A, (nn)
                    Operations.current_operation = Operations.LDnnA;
                } else {
                    throw new Exception($"INVALID OPCODE: {opcode:X2} @ {Registers.PC:X4}");
                }
                
            } else if (z == 3) { // Assorted ops
                if (y == 0) // JP nn
                    Operations.current_operation = Operations.JPTaken;
                
                else if (y == 1) { // CB PREFIX
                    int sub_z = gameboy.ReadMemory(Registers.PC) & 0x07;

                    if (sub_z == 6) {
                        Operations.current_operation = Operations.CBMemory;
                    } else {
                        Operations.current_operation = Operations.CBRegister;
                    }
                }
                else if (y == 6) { // DI
                    interrupt_master_enable = false;
                    _ime_enable_requested = false;
                    FinishOperation();
                    
                }else if (y == 7) { // EI
                    _ime_enable_requested = true;
                    FinishOperation();
                } else {
                    throw new Exception($"INVALID OPCODE: {opcode:X2} @ {Registers.PC:X4}");
                }
                
            } else if (z == 4) { // conditional CALL
                if (y <= 3) {
                    bool condition_met = cc_table(y);
                    if (condition_met) Operations.current_operation = Operations.CALLTaken; 
                    else Operations.current_operation = Operations.JPFailed;
                    
                } else {
                    throw new Exception($"INVALID OPCODE: {opcode:X2} @ {Registers.PC:X4}");
                }
                
            } else if (z == 5) { //PUSH and various ops
                if (q == 0) {
                    Operations.source_register = RP2_table(p);
                    Operations.current_operation = Operations.PushReg16;
                    
                } else if (q == 1) {
                    if (p == 0) Operations.current_operation = Operations.CALLTaken;
                    else throw new Exception($"INVALID OPCODE: {opcode:X2} @ {Registers.PC:X4}");
                }
                
            } else if (z == 6) { // immediate ALU ops
                Operations.alu_op = y;
                Operations.current_operation = Operations.ALUImm;

            } else if (z == 7) {
                Operations.pointer = (ushort)(y * 8);
                Operations.current_operation = Operations.RSTPipeline;
                
            }
        }
    }

    internal int R_table(int y) {
        if (y == 0) return TargetRegister.B;
        if (y == 1) return TargetRegister.C;
        if (y == 2) return TargetRegister.D;
        if (y == 3) return TargetRegister.E;
        if (y == 4) return TargetRegister.H;
        if (y == 5) return TargetRegister.L;
        
        if (y == 7) return TargetRegister.A;
        return 0;
    }
    
    int RP_table(int p) {
        if (p == 0) return TargetRegister.BC;
        if (p == 1) return TargetRegister.DE;
        if (p == 2) return TargetRegister.HL;
        if (p == 3) return TargetRegister.SP;
        return 0;
    }
    int RP_table_indirect_load(int p) {
        if (p == 0) return TargetRegister.BC;
        if (p == 1) return TargetRegister.DE;
        if (p == 2) return TargetRegister.HL;
        if (p == 3) return TargetRegister.HL;
        return 0;
    }
    
    int RP2_table(int p) {
        if (p == 0) return TargetRegister.BC;
        if (p == 1) return TargetRegister.DE;
        if (p == 2) return TargetRegister.HL;
        if (p == 3) return TargetRegister.AF;
        return 0;
    }

    bool cc_table(int cc_table_id) {
        bool result = cc_table_id switch {
            0 => !Registers.GetFlag(CPUFlagMask.Zero),
            1 => Registers.GetFlag(CPUFlagMask.Zero),
            2 => !Registers.GetFlag(CPUFlagMask.Carry),
            3 => Registers.GetFlag(CPUFlagMask.Carry)
        };
        return result;
    }
    
    void JR_cc(int cc_table_id) {
        if (cc_table(cc_table_id)) Operations.current_operation = Operations.JRTaken;
        else Operations.current_operation = Operations.JRFailed;
    }
    
    void JP_cc(int cc_table_id) {
        if (cc_table(cc_table_id)) Operations.current_operation = Operations.JPTaken;
        else Operations.current_operation = Operations.JPFailed;
    }

    void RET_cc(int cc_table_id) {
        if (cc_table(cc_table_id)) Operations.current_operation = Operations.RETTaken;
        else Operations.current_operation = Operations.RETFailed;
    }
    
    void DAA() {
        bool previous_op_subtract = Registers.GetFlag(CPUFlagMask.Negative);
        bool half_carry =           Registers.GetFlag(CPUFlagMask.HalfCarry);
        bool carry =                Registers.GetFlag(CPUFlagMask.Carry);

        byte A = Registers.A;
                
        if (!previous_op_subtract) {
            if (carry || A > 0x99) {
                A += 0x60;
                Registers.SetFlag(CPUFlagMask.Carry, true);
            }
                    
            if (half_carry || (A & 0x0F) > 9) A += 0x06;
                    
        } else {
            if (half_carry) A -= 0x06;
            if (carry) A -= 0x60;
        }
                
        Registers.SetFlag(CPUFlagMask.Zero, A == 0);
        Registers.SetFlag(CPUFlagMask.HalfCarry, false);
                
        Registers.A = A;
    }
}