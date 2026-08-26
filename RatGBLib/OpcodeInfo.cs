using System.Collections.Generic;
namespace RatGBLib;

public class OpcodeInfo {
    public string name;
    
    public ushort PC;
    
    public ushort SP_before;
    public ushort SP_after;
    
    public byte opcode;
    
    public byte? operand_one;
    public byte? operand_two;
    
    public byte[]? stack_before;
    public byte[]? stack_after;

    public int stack_size = 8;

    public uint cycles = 0;
    public uint intended_cycles = 0;
    public OpcodeInfo(byte op) {
        opcode = op;
        name = Opcodes.opcode_list[op].op_name;
        intended_cycles = Opcodes.opcode_list[op].max_m_cycles * 4;
    }

    public void store_stack(GameBoy gb, ushort SP, ref byte[] stack) {
        stack = new byte[stack_size];
        for (int i = 0; i < stack_size; i++) {
            stack[i] = gb.ReadByte((ushort)(SP + i));
            if (SP + i == 0xFFFF) break;
        }
    }
}

public static class Opcodes {
    public static Dictionary<byte, (uint max_m_cycles, string op_name)> opcode_list = new(256) {
        // ----- 0x0x -----
        {0x00, (1, "NOP")}, 
        {0x01, (3, "LD BC,u8")}, 
        {0x02, (2, "LD (BC),A")}, 
        {0x03, (2, "INC BC")}, 
        {0x04, (1, "INC B")}, 
        {0x05, (1, "DEC B")}, 
        {0x06, (2, "LD B,u8")},
        {0x07, (1, "RLCA")},
        {0x08, (5, "LD u16,SP")},
        {0x09, (2, "ADD HL,BC")},
        {0x0A, (2, "LD A,(BC)")},
        {0x0B, (2, "DEC BC")},
        {0x0C, (1, "INC C")},
        {0x0D, (1, "DEC C")},
        {0x0E, (2, "LD C,u8")},
        {0x0F, (1, "RRCA")},
        
        // ----- 0x1x -----
        {0x10, (1, "STOP")},
        {0x11, (3, "LD DE,u16")},
        {0x12, (2, "LD (DE),A")},
        {0x13, (2, "INC DE")},
        {0x14, (2, "INC D")},
        {0x15, (1, "DEC D")},
        {0x16, (2, "LD D,u8")},
        {0x17, (1, "RLA")},
        {0x18, (3, "JR i8")},
        {0x19, (2, "ADD HL,DE")},
        {0x1A, (2, "LD A,(DE)")},
        {0x1B, (2, "DEC DE")},
        {0x1C, (1, "INC E")},
        {0x1D, (1, "DEC E")},
        {0x1E, (2, "LD E,u8")},
        {0x1F, (1, "RRA")},
        
        // ----- 0x2x -----
        {0x20, (3, "JR NZ,i8")},
        {0x21, (3, "LD HL,u16")},
        {0x22, (2, "LD (HL+),A")},
        {0x23, (2, "INC HL")},
        {0x24, (1, "INC H")},
        {0x25, (1, "DEC H")},
        {0x26, (2, "LD H,u8")},
        {0x27, (1, "DAA")},
        {0x28, (3, "JR Z,i8")},
        {0x29, (2, "ADD HL,HL")},
        {0x2A, (2, "LD A,(HL+)")},
        {0x2B, (2, "DEC HL")},
        {0x2C, (1, "INC L")},
        {0x2D, (1, "DEC L")},
        {0x2E, (2, "LD L,u8")},
        {0x2F, (1, "CPL")},
        
        // ----- 0x3x -----
        {0x30, (3, "JR NC,i8")},
        {0x31, (3, "LD SP,u16")},
        {0x32, (2, "LD (HL-),A")},
        {0x33, (2, "INC SP")},
        {0x34, (3, "INC (HL)")},
        {0x35, (3, "DEC (HL)")},
        {0x36, (3, "LD (HL),u8")},
        {0x37, (1, "SCF")},
        {0x38, (3, "JR C,i8")},
        {0x39, (2, "ADD HL,SP")},
        {0x3A, (2, "LD A,(HL-)")},
        {0x3B, (2, "DEC SP")},
        {0x3C, (1, "INC A")},
        {0x3D, (1, "DEC A")},
        {0x3E, (2, "LD A,u8")},
        {0x3F, (1, "CCF")},
        
        // ----- 0x4x -----
        {0x40, (1, "LD B,B")},
        {0x41, (1, "LD B,C")},
        {0x42, (1, "LD B,D")},
        {0x43, (1, "LD B,E")},
        {0x44, (1, "LD B,H")},
        {0x45, (1, "LD B,L")},
        {0x46, (2, "LD B,(HL)")},
        {0x47, (1, "LD B,A")},
        {0x48, (1, "LD C,B")},
        {0x49, (1, "LD C,C")},
        {0x4A, (1, "LD C,D")},
        {0x4B, (1, "LD C,E")},
        {0x4C, (1, "LD C,H")},
        {0x4D, (1, "LD C,L")},
        {0x4E, (2, "LD C,(HL)")},
        {0x4F, (1, "LD C,A")},
        
        // ----- 0x5x -----
        {0x50, (1, "LD D,B")},
        {0x51, (1, "LD D,C")},
        {0x52, (1, "LD D,D")},
        {0x53, (1, "LD D,E")},
        {0x54, (1, "LD D,H")},
        {0x55, (1, "LD D,L")},
        {0x56, (2, "LD D,(HL)")},
        {0x57, (1, "LD D,A")},
        {0x58, (1, "LD E,B")},
        {0x59, (1, "LD E,C")},
        {0x5A, (1, "LD E,D")},
        {0x5B, (1, "LD E,E")},
        {0x5C, (1, "LD E,H")},
        {0x5D, (1, "LD E,L")},
        {0x5E, (2, "LD E,(HL)")},
        {0x5F, (1, "LD E,A")},
        
        // ----- 0x6x -----
        {0x60, (1, "LD H,B")},
        {0x61, (1, "LD H,C")},
        {0x62, (1, "LD H,D")},
        {0x63, (1, "LD H,E")},
        {0x64, (1, "LD H,H")},
        {0x65, (1, "LD H,L")},
        {0x66, (2, "LD H,(HL)")},
        {0x67, (1, "LD H,A")},
        {0x68, (1, "LD L,B")},
        {0x69, (1, "LD L,C")},
        {0x6A, (1, "LD L,D")},
        {0x6B, (1, "LD L,E")},
        {0x6C, (1, "LD L,H")},
        {0x6D, (1, "LD L,L")},
        {0x6E, (2, "LD L,(HL)")},
        {0x6F, (1, "LD L,A")},
        
        // ----- 0x7x -----
        {0x70, (2, "LD (HL),B")},
        {0x71, (2, "LD (HL),C")},
        {0x72, (2, "LD (HL),D")},
        {0x73, (2, "LD (HL),E")},
        {0x74, (2, "LD (HL),H")},
        {0x75, (2, "LD (HL),L")},
        {0x76, (1, "HALT")},
        {0x77, (2, "LD (HL),A")},
        {0x78, (1, "LD A,B")},
        {0x79, (1, "LD A,C")},
        {0x7A, (1, "LD A,D")},
        {0x7B, (1, "LD A,E")},
        {0x7C, (1, "LD A,H")},
        {0x7D, (1, "LD A,L")},
        {0x7E, (2, "LD A,(HL)")},
        {0x7F, (1, "LD A,A")},
        
        // ----- 0x8x -----
        {0x80, (1, "ADD A,B")},
        {0x81, (1, "ADD A,C")},
        {0x82, (1, "ADD A,D")},
        {0x83, (1, "ADD A,E")},
        {0x84, (1, "ADD A,H")},
        {0x85, (1, "ADD A,L")},
        {0x86, (2, "ADD A,(HL)")},
        {0x87, (1, "ADD A,A")},
        {0x88, (1, "ADC A,B")},
        {0x89, (1, "ADC A,C")},
        {0x8A, (1, "ADC A,D")},
        {0x8B, (1, "ADC A,E")},
        {0x8C, (1, "ADC A,H")},
        {0x8D, (1, "ADC A,L")},
        {0x8E, (2, "ADC A,(HL)")},
        {0x8F, (1, "ADC A,A")},
        
        // ----- 0x9x -----
        {0x90, (1, "SUB A,B")},
        {0x91, (1, "SUB A,C")},
        {0x92, (1, "SUB A,D")},
        {0x93, (1, "SUB A,E")},
        {0x94, (1, "SUB A,H")},
        {0x95, (1, "SUB A,L")},
        {0x96, (2, "SUB A,(HL)")},
        {0x97, (1, "SUB A,A")},
        {0x98, (1, "SBC A,B")},
        {0x99, (1, "SBC A,C")},
        {0x9A, (1, "SBC A,D")},
        {0x9B, (1, "SBC A,E")},
        {0x9C, (1, "SBC A,H")},
        {0x9D, (1, "SBC A,L")},
        {0x9E, (2, "SBC A,(HL)")},
        {0x9F, (1, "SBC A,A")},
        
        // ----- 0xAx -----
        {0xA0, (1, "AND A,B")},
        {0xA1, (1, "AND A,C")},
        {0xA2, (1, "AND A,D")},
        {0xA3, (1, "AND A,E")},
        {0xA4, (1, "AND A,H")},
        {0xA5, (1, "AND A,L")},
        {0xA6, (2, "AND A,(HL)")},
        {0xA7, (1, "AND A,A")},
        {0xA8, (1, "XOR A,B")},
        {0xA9, (1, "XOR A,C")},
        {0xAA, (1, "XOR A,D")},
        {0xAB, (1, "XOR A,E")},
        {0xAC, (1, "XOR A,H")},
        {0xAD, (1, "XOR A,L")},
        {0xAE, (2, "XOR A,(HL)")},
        {0xAF, (1, "XOR A,A")},
        
        // ----- 0xBx -----
        {0xB0, (1, "OR A,B")},
        {0xB1, (1, "OR A,C")},
        {0xB2, (1, "OR A,D")},
        {0xB3, (1, "OR A,E")},
        {0xB4, (1, "OR A,H")},
        {0xB5, (1, "OR A,L")},
        {0xB6, (2, "OR A,(HL)")},
        {0xB7, (1, "OR A,A")},
        {0xB8, (1, "CP A,B")},
        {0xB9, (1, "CP A,C")},
        {0xBA, (1, "CP A,D")},
        {0xBB, (1, "CP A,E")},
        {0xBC, (1, "CP A,H")},
        {0xBD, (1, "CP A,L")},
        {0xBE, (2, "CP A,(HL)")},
        {0xBF, (1, "CP A,A")},
        
        // ----- 0xCx -----
        {0xC0, (5, "RET NZ")},
        {0xC1, (3, "POP BC")},
        {0xC2, (4, "JP NZ,u16")},
        {0xC3, (4, "JP u16")},
        {0xC4, (6, "CALL NZ,u16")},
        {0xC5, (4, "PUSH BC")},
        {0xC6, (2, "ADD A,u8")},
        {0xC7, (4, "RST 00h")},
        {0xC8, (5, "RET Z")},
        {0xC9, (4, "RET")},
        {0xCA, (4, "JP Z,u16")},
        {0xCB, (4, "PREFIX CB")},
        {0xCC, (6, "CALL Z,u16")},
        {0xCD, (6, "CALL u16")},
        {0xCE, (2, "ADC A,u8")},
        {0xCF, (4, "RST 08h")},
        
        // ----- 0xDx -----
        {0xD0, (5, "RET NC")},
        {0xD1, (3, "POP DE")},
        {0xD2, (4, "JP NC,u16")},
        {0xD3, (0, "INVALID")},
        {0xD4, (6, "CALL NC,u16")},
        {0xD5, (4, "PUSH DE")},
        {0xD6, (2, "SUB A,u8")},
        {0xD7, (4, "RST 10h")},
        {0xD8, (5, "RET C")},
        {0xD9, (4, "RETI")},
        {0xDA, (4, "JP C,u16")},
        {0xDB, (0, "INVALID")},
        {0xDC, (6, "CALL C,u16")},
        {0xDD, (0, "INVALID")},
        {0xDE, (2, "SBC A,u8")},
        {0xDF, (4, "RST 18h")},
        
        // ----- 0xEx -----
        {0xE0, (3, "LD ($FF00+u8),A")},
        {0xE1, (3, "POP HL")},
        {0xE2, (2, "LD ($FF00+C),A")},
        {0xE3, (0, "INVALID")},
        {0xE4, (0, "INVALID")},
        {0xE5, (4, "PUSH HL")},
        {0xE6, (2, "AND A,u8")},
        {0xE7, (4, "RST 20h")},
        {0xE8, (4, "ADD SP,i8")},
        {0xE9, (1, "JP HL")},
        {0xEA, (4, "LD (u16),A")},
        {0xEB, (0, "INVALID")},
        {0xEC, (0, "INVALID")},
        {0xED, (0, "INVALID")},
        {0xEE, (2, "XOR A,u8")},
        {0xEF, (4, "RST 28h")},
        
        // ----- 0xFx -----
        {0xF0, (3, "LD A,($FF00+u8)")},
        {0xF1, (3, "POP AF")},
        {0xF2, (2, "LD A,($FF00+C)")},
        {0xF3, (1, "DI")},
        {0xF4, (0, "INVALID")},
        {0xF5, (4, "PUSH AF")},
        {0xF6, (2, "OR A,u8")},
        {0xF7, (4, "RST 30h")},
        {0xF8, (3, "LD HL,SP+i8")},
        {0xF9, (2, "LD SP,HL")},
        {0xFA, (4, "LD A,(u16)")},
        {0xFB, (1, "EI")},
        {0xFC, (0, "INVALID")},
        {0xFD, (0, "INVALID")},
        {0xFE, (2, "CP A,u8")},
        {0xFF, (4, "RST 38h")},
    };
}