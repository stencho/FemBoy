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
    
    public OpcodeInfo(byte op) {
        opcode = op;
        name = Opcodes.opcode_list[op];
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
    public static Dictionary<byte, string> opcode_list = new(256) {
        // ----- 0x0x -----
        {0x00, "NOP"}, 
        {0x01, "LD BC,u8"}, 
        {0x02, "LD (BC),A"}, 
        {0x03, "INC BC"}, 
        {0x04, "INC B"}, 
        {0x05, "DEC B"}, 
        {0x06, "LD B,u8"},
        {0x07, "RLCA"},
        {0x08, "LD u16,SP"},
        {0x09, "ADD HL,BC"},
        {0x0A, "LD A,(BC)"},
        {0x0B, "DEC BC"},
        {0x0C, "INC C"},
        {0x0D, "DEC C"},
        {0x0E, "LD C,u8"},
        {0x0F, "RRCA"},
        
        // ----- 0x1x -----
        {0x10, "STOP"},
        {0x11, "LD DE,u16"},
        {0x12, "LD (DE),A"},
        {0x13, "INC DE"},
        {0x14, "INC D"},
        {0x15, "DEC D"},
        {0x16, "LD D,u8"},
        {0x17, "RLA"},
        {0x18, "JR i8"},
        {0x19, "ADD HL,DE"},
        {0x1A, "LD A,(DE)"},
        {0x1B, "DEC DE"},
        {0x1C, "INC E"},
        {0x1D, "DEC E"},
        {0x1E, "LD E,u8"},
        {0x1F, "RRA"},
        
        // ----- 0x2x -----
        {0x20, "JR NZ,i8"},
        {0x21, "LD HL,u16"},
        {0x22, "LD (HL+),A"},
        {0x23, "INC HL"},
        {0x24, "INC H"},
        {0x25, "DEC H"},
        {0x26, "LD H,u8"},
        {0x27, "DAA"},
        {0x28, "JR Z,i8"},
        {0x29, "ADD HL,HL"},
        {0x2A, "LD A,(HL+)"},
        {0x2B, "DEC HL"},
        {0x2C, "INC L"},
        {0x2D, "DEC L"},
        {0x2E, "LD L,u8"},
        {0x2F, "CPL"},
        
        // ----- 0x3x -----
        {0x30, "JR NC,i8"},
        {0x31, "LD SP,u16"},
        {0x32, "LD (HL-),A"},
        {0x33, "INC SP"},
        {0x34, "INC (HL)"},
        {0x35, "DEC (HL)"},
        {0x36, "LD (HL),u8"},
        {0x37, "SCF"},
        {0x38, "JR C,i8"},
        {0x39, "ADD HL,SP"},
        {0x3A, "LD A,(HL-)"},
        {0x3B, "DEC SP"},
        {0x3C, "INC A"},
        {0x3D, "DEC A"},
        {0x3E, "LD A,u8"},
        {0x3F, "CCF"},
        
        // ----- 0x4x -----
        {0x40, "LD B,B"},
        {0x41, "LD B,C"},
        {0x42, "LD B,D"},
        {0x43, "LD B,E"},
        {0x44, "LD B,H"},
        {0x45, "LD B,L"},
        {0x46, "LD B,(HL)"},
        {0x47, "LD B,A"},
        {0x48, "LD C,B"},
        {0x49, "LD C,C"},
        {0x4A, "LD C,D"},
        {0x4B, "LD C,E"},
        {0x4C, "LD C,H"},
        {0x4D, "LD C,L"},
        {0x4E, "LD C,(HL)"},
        {0x4F, "LD C,A"},
        
        // ----- 0x5x -----
        {0x50, "LD D,B"},
        {0x51, "LD D,C"},
        {0x52, "LD D,D"},
        {0x53, "LD D,E"},
        {0x54, "LD D,H"},
        {0x55, "LD D,L"},
        {0x56, "LD D,(HL)"},
        {0x57, "LD D,A"},
        {0x58, "LD E,B"},
        {0x59, "LD E,C"},
        {0x5A, "LD E,D"},
        {0x5B, "LD E,E"},
        {0x5C, "LD E,H"},
        {0x5D, "LD E,L"},
        {0x5E, "LD E,(HL)"},
        {0x5F, "LD E,A"},
        
        // ----- 0x6x -----
        {0x60, "LD H,B"},
        {0x61, "LD H,C"},
        {0x62, "LD H,D"},
        {0x63, "LD H,E"},
        {0x64, "LD H,H"},
        {0x65, "LD H,L"},
        {0x66, "LD H,(HL)"},
        {0x67, "LD H,A"},
        {0x68, "LD L,B"},
        {0x69, "LD L,C"},
        {0x6A, "LD L,D"},
        {0x6B, "LD L,E"},
        {0x6C, "LD L,H"},
        {0x6D, "LD L,L"},
        {0x6E, "LD L,(HL)"},
        {0x6F, "LD L,A"},
        
        // ----- 0x7x -----
        {0x70, "LD (HL),B"},
        {0x71, "LD (HL),C"},
        {0x72, "LD (HL),D"},
        {0x73, "LD (HL),E"},
        {0x74, "LD (HL),H"},
        {0x75, "LD (HL),L"},
        {0x76, "HALT"},
        {0x77, "LD (HL),A"},
        {0x78, "LD A,B"},
        {0x79, "LD A,C"},
        {0x7A, "LD A,D"},
        {0x7B, "LD A,E"},
        {0x7C, "LD A,H"},
        {0x7D, "LD A,L"},
        {0x7E, "LD A,(HL)"},
        {0x7F, "LD A,A"},
        
        // ----- 0x8x -----
        {0x80, "ADD A,B"},
        {0x81, "ADD A,C"},
        {0x82, "ADD A,D"},
        {0x83, "ADD A,E"},
        {0x84, "ADD A,H"},
        {0x85, "ADD A,L"},
        {0x86, "ADD A,(HL)"},
        {0x87, "ADD A,A"},
        {0x88, "ADC A,B"},
        {0x89, "ADC A,C"},
        {0x8A, "ADC A,D"},
        {0x8B, "ADC A,E"},
        {0x8C, "ADC A,H"},
        {0x8D, "ADC A,L"},
        {0x8E, "ADC A,(HL)"},
        {0x8F, "ADC A,A"},
        
        // ----- 0x9x -----
        {0x90, "SUB A,B"},
        {0x91, "SUB A,C"},
        {0x92, "SUB A,D"},
        {0x93, "SUB A,E"},
        {0x94, "SUB A,H"},
        {0x95, "SUB A,L"},
        {0x96, "SUB A,(HL)"},
        {0x97, "SUB A,A"},
        {0x98, "SBC A,B"},
        {0x99, "SBC A,C"},
        {0x9A, "SBC A,D"},
        {0x9B, "SBC A,E"},
        {0x9C, "SBC A,H"},
        {0x9D, "SBC A,L"},
        {0x9E, "SBC A,(HL)"},
        {0x9F, "SBC A,A"},
        
        // ----- 0xAx -----
        {0xA0, "AND A,B"},
        {0xA1, "AND A,C"},
        {0xA2, "AND A,D"},
        {0xA3, "AND A,E"},
        {0xA4, "AND A,H"},
        {0xA5, "AND A,L"},
        {0xA6, "AND A,(HL)"},
        {0xA7, "AND A,A"},
        {0xA8, "XOR A,B"},
        {0xA9, "XOR A,C"},
        {0xAA, "XOR A,D"},
        {0xAB, "XOR A,E"},
        {0xAC, "XOR A,H"},
        {0xAD, "XOR A,L"},
        {0xAE, "XOR A,(HL)"},
        {0xAF, "XOR A,A"},
        
        // ----- 0xBx -----
        {0xB0, "OR A,B"},
        {0xB1, "OR A,C"},
        {0xB2, "OR A,D"},
        {0xB3, "OR A,E"},
        {0xB4, "OR A,H"},
        {0xB5, "OR A,L"},
        {0xB6, "OR A,(HL)"},
        {0xB7, "OR A,A"},
        {0xB8, "CP A,B"},
        {0xB9, "CP A,C"},
        {0xBA, "CP A,D"},
        {0xBB, "CP A,E"},
        {0xBC, "CP A,H"},
        {0xBD, "CP A,L"},
        {0xBE, "CP A,(HL)"},
        {0xBF, "CP A,A"},
        
        // ----- 0xCx -----
        {0xC0, "RET NZ"},
        {0xC1, "POP BC"},
        {0xC2, "JP NZ,u16"},
        {0xC3, "JP u16"},
        {0xC4, "CALL NZ,u16"},
        {0xC5, "PUSH BC"},
        {0xC6, "ADD A,u8"},
        {0xC7, "RST 00h"},
        {0xC8, "RET Z"},
        {0xC9, "RET"},
        {0xCA, "JP Z,u16"},
        {0xCB, "PREFIX CB"},
        {0xCC, "CALL Z,u16"},
        {0xCD, "CALL u16"},
        {0xCE, "ADC A,u8"},
        {0xCF, "RST 08h"},
        
        // ----- 0xDx -----
        {0xD0, "RET NC"},
        {0xD1, "POP DE"},
        {0xD2, "JP NC,u16"},
        {0xD3, "INVALID"},
        {0xD4, "CALL NC,u16"},
        {0xD5, "PUSH DE"},
        {0xD6, "SUB A,u8"},
        {0xD7, "RST 10h"},
        {0xD8, "RET C"},
        {0xD9, "RETI"},
        {0xDA, "JP C,u16"},
        {0xDB, "INVALID"},
        {0xDC, "CALL C,u16"},
        {0xDD, "INVALID"},
        {0xDE, "SBC A,u8"},
        {0xDF, "RST 18h"},
        
        // ----- 0xEx -----
        {0xE0, "LD ($FF00+u8),A"},
        {0xE1, "POP HL"},
        {0xE2, "LD ($FF00+C),A"},
        {0xE3, "INVALID"},
        {0xE4, "INVALID"},
        {0xE5, "PUSH HL"},
        {0xE6, "AND A,u8"},
        {0xE7, "RST 20h"},
        {0xE8, "ADD SP,i8"},
        {0xE9, "JP HL"},
        {0xEA, "LD (u16),A"},
        {0xEB, "INVALID"},
        {0xEC, "INVALID"},
        {0xED, "INVALID"},
        {0xEE, "XOR A,u8"},
        {0xEF, "RST 28h"},
        
        // ----- 0xFx -----
        {0xF0, "LD A,($FF00+u8)"},
        {0xF1, "POP AF"},
        {0xF2, "LD A,($FF00+C)"},
        {0xF3, "DI"},
        {0xF4, "INVALID"},
        {0xF5, "PUSH AF"},
        {0xF6, "OR A,u8"},
        {0xF7, "RST 30h"},
        {0xF8, "LD HL,SP+i8"},
        {0xF9, "LD SP,HL"},
        {0xFA, "LD A,(u16)"},
        {0xFB, "EI"},
        {0xFC, "INVALID"},
        {0xFD, "INVALID"},
        {0xFE, "CP A,u8"},
        {0xFF, "RST 38h"},
    };
}