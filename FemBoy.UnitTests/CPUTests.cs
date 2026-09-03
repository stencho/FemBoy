using FemBoy;
using Microsoft.Maui.Animations;
using NUnit.Framework;

namespace Microsoft.Extensions.Hosting;
public class CPUTests {
    GameBoy gb = new GameBoy();
    private CPU CPU => gb.CPU;
    
    [SetUp]
    public void Setup() {
        gb = new GameBoy();
    }
    
    [TestCase((byte)0x00, (byte)0x01)]
    [TestCase((byte)0x01, (byte)0x02)]
    [TestCase((byte)0x0F, (byte)0x10)]
    [TestCase((byte)0x7F, (byte)0x80)]
    [TestCase((byte)0xFF, (byte)0x00)]
    public void INC_B(byte init, byte expected) {
        gb.LoadROM(0x04);

        CPU.Registers.B = init;
        
        while (CPU.ops < 1) CPU.Tick();
        
        Assert.That(CPU.Registers.B, Is.EqualTo(expected));
    }
    
    [TestCase((ushort)0x0000, (ushort)0x0001)]
    [TestCase((ushort)0x0001, (ushort)0x0002)]
    [TestCase((ushort)0x000F, (ushort)0x0010)]
    [TestCase((ushort)0x00FF, (ushort)0x0100)]
    [TestCase((ushort)0x0FFF, (ushort)0x1000)]
    [TestCase((ushort)0x7FFF, (ushort)0x8000)]
    [TestCase((ushort)0x80FF, (ushort)0x8100)]
    [TestCase((ushort)0xFFFF, (ushort)0x0000)]
    public void INC_BC(ushort init, ushort expected) {
        gb.LoadROM(0x03);

        CPU.Registers.BC = init;
        CPU.Registers.F = 0xF0;
        
        while (CPU.ops < 1) CPU.Tick();
        
        Assert.That(CPU.Registers.BC, Is.EqualTo(expected));
        Assert.That(CPU.Registers.F, Is.EqualTo((byte)0xF0));
    }
    
    [TestCase((ushort)0x0000, (ushort)0x0001)]
    [TestCase((ushort)0x0001, (ushort)0x0002)]
    [TestCase((ushort)0x000F, (ushort)0x0010)]
    [TestCase((ushort)0x00FF, (ushort)0x0100)]
    [TestCase((ushort)0x0FFF, (ushort)0x1000)]
    [TestCase((ushort)0x7FFF, (ushort)0x8000)]
    [TestCase((ushort)0x80FF, (ushort)0x8100)]
    [TestCase((ushort)0xFFFF, (ushort)0x0000)]
    public void INC_DE(ushort init, ushort expected) {
        gb.LoadROM(0x13);

        CPU.Registers.DE = init;
        CPU.Registers.F = 0xF0;
        
        while (CPU.ops < 1) CPU.Tick();
        
        Assert.That(CPU.Registers.DE, Is.EqualTo(expected));
        Assert.That(CPU.Registers.F, Is.EqualTo((byte)0xF0));
    }
    
    [TestCase((ushort)0x0000, (ushort)0x0001)]
    [TestCase((ushort)0x0001, (ushort)0x0002)]
    [TestCase((ushort)0x000F, (ushort)0x0010)]
    [TestCase((ushort)0x00FF, (ushort)0x0100)]
    [TestCase((ushort)0x0FFF, (ushort)0x1000)]
    [TestCase((ushort)0x7FFF, (ushort)0x8000)]
    [TestCase((ushort)0x80FF, (ushort)0x8100)]
    [TestCase((ushort)0xFFFF, (ushort)0x0000)]
    public void INC_HL(ushort init, ushort expected) {
        gb.LoadROM(0x23);

        CPU.Registers.HL = init;
        CPU.Registers.F = 0xF0;
        
        while (CPU.ops < 1) CPU.Tick();
        
        Assert.That(CPU.Registers.HL, Is.EqualTo(expected));
        Assert.That(CPU.Registers.F, Is.EqualTo((byte)0xF0));
    }
    
    [TestCase((ushort)0x0001, (ushort)0x0000)]
    [TestCase((ushort)0x0002, (ushort)0x0001)]
    [TestCase((ushort)0x0010, (ushort)0x000F)]
    [TestCase((ushort)0x0100, (ushort)0x00FF)]
    [TestCase((ushort)0x1000, (ushort)0x0FFF)]
    [TestCase((ushort)0x8000, (ushort)0x7FFF)]
    [TestCase((ushort)0x8100, (ushort)0x80FF)]
    [TestCase((ushort)0x0000, (ushort)0xFFFF)]
    public void DEC_BC(ushort init, ushort expected) {
        gb.LoadROM(0x0B);

        CPU.Registers.BC = init;
        CPU.Registers.F = 0xF0;

        while (CPU.ops < 1) CPU.Tick();
        

        Assert.That(CPU.Registers.BC, Is.EqualTo(expected));
        Assert.That(CPU.Registers.F, Is.EqualTo(0xF0));
    }
    
    [TestCase(0x00, (ushort)0x0102)]
    [TestCase(0x01, (ushort)0x0103)]
    [TestCase(0x02, (ushort)0x0104)]
    [TestCase(0x7F, (ushort)0x0181)]
    [TestCase(0xFE, (ushort)0x0100)]
    [TestCase(0xFD, (ushort)0x00FF)]
    [TestCase(0x80, (ushort)0x0082)]
    public void JR(byte offset, ushort expected) {
        gb.LoadROM(0x18, offset);

        CPU.Registers.PC = 0x0100;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(expected));
    }
    
    [TestCase(false, (ushort)0x0104)] // Z=0 -> taken
    [TestCase(true,  (ushort)0x0102)] // Z=1 -> not taken
    public void JR_NZ(bool zero, ushort expected) {
        gb.LoadROM(0x20, 0x02);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, zero);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(expected));
    }
    
    [TestCase(true,  (ushort)0x0104)]
    [TestCase(false, (ushort)0x0102)]
    public void JR_Z(bool zero, ushort expected) {
        gb.LoadROM(0x28, 0x02);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, zero);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(expected));
    }
    
    [TestCase(false, (ushort)0x0104)] // C=0 -> taken
    [TestCase(true,  (ushort)0x0102)] // C=1 -> not taken
    public void JR_NC(bool carry, ushort expected) {
        gb.LoadROM(0x30, 0x02);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, carry);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(expected));
    }
    
    [TestCase(true,  (ushort)0x0104)]
    [TestCase(false, (ushort)0x0102)]
    public void JR_C(bool carry, ushort expected) {
        gb.LoadROM(0x38, 0x02);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, carry);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(expected));
    }
    
    [TestCase((ushort)0x8000, 0x42, (ushort)0x8001)]
    [TestCase((ushort)0x80FF, 0x37, (ushort)0x8100)]
    [TestCase((ushort)0xFFFF, 0xA5, (ushort)0x0000)]
    public void LD_HL_Inc_A(ushort hl, byte value, ushort expectedHL) {
        gb.LoadROM(0x22);

        CPU.Registers.HL = hl;
        CPU.Registers.A = value;

        while (CPU.ops < 1) CPU.Tick();
        

        Assert.That(gb.ReadMemory(hl), Is.EqualTo(value));
        Assert.That(CPU.Registers.HL, Is.EqualTo(expectedHL));
    }
    
    [TestCase((ushort)0x8000, 0x42, (ushort)0x8001)]
    [TestCase((ushort)0x80FF, 0x37, (ushort)0x8100)]
    [TestCase((ushort)0xFFFF, 0xA5, (ushort)0x0000)]
    public void LD_A_HL_Inc(ushort hl, byte value, ushort expectedHL) {
        gb.LoadROM(0x2A);

        CPU.Registers.HL = hl;
        gb.WriteMemory(hl, value);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(value));
        Assert.That(CPU.Registers.HL, Is.EqualTo(expectedHL));
    }
    
    [TestCase((ushort)0xC000, 0x00)]
    [TestCase((ushort)0xC123, 0x42)]
    [TestCase((ushort)0xD000, 0x7F)]
    [TestCase((ushort)0xFFFF, 0xA5)]
    public void LD_nn_A(ushort address, byte value) {
        gb.LoadROM(
            0xEA,
            (byte)(address & 0xFF),
            (byte)(address >> 8)
        );

        CPU.Registers.A = value;

        while (CPU.ops < 1)
            CPU.Tick();
        
        Assert.That(gb.ReadMemory(address), Is.EqualTo(value));
    }
    
    [TestCase((ushort)0xC000, 0x00)]
    [TestCase((ushort)0xC123, 0x42)]
    [TestCase((ushort)0xD000, 0x7F)]
    [TestCase((ushort)0xFFFF, 0xA5)]
    public void LD_A_nn(ushort address, byte value) {
        gb.LoadROM(
            0xFA,
            (byte)(address & 0xFF),
            (byte)(address >> 8)
        );

        gb.WriteMemory(address, value);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(value));
    }
    
    [TestCase((ushort)0x1234, (ushort)0xD000)]
    [TestCase((ushort)0x0000, (ushort)0xD000)]
    [TestCase((ushort)0xFFFF, (ushort)0xD000)]
    [TestCase((ushort)0xA55A, (ushort)0xD001)]
    public void PUSH_BC(ushort value, ushort initialSP) {
        gb.LoadROM(0xC5);

        CPU.Registers.BC = value;
        CPU.Registers.SP = initialSP;

        CPU.ops = 0;
        
        while (CPU.ops < 1) CPU.Tick();
        
        Assert.That(CPU.Registers.SP, Is.EqualTo((ushort)(initialSP - 2)));

        Console.Write($"BC = {CPU.Registers.BC:X4} ");
        Console.Write($"SP = {CPU.Registers.SP:X4} ");
        Console.Write($"CFFF = {gb.RAM.Read(0xCFFF):X2} ");
        Console.WriteLine($"CFFE = {gb.RAM.Read(0xCFFE):X2}");
        
        Assert.That(
            gb.RAM.Read((ushort)(initialSP - 1)),
            Is.EqualTo((byte)(value >> 8))
        );

        Assert.That(
            gb.RAM.Read((ushort)(initialSP - 2)),
            Is.EqualTo((byte)(value & 0xFF))
        );
    }
    
    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0x0100)]
    [TestCase((ushort)0xFFFF)]
    [TestCase((ushort)0xA55A)]
    public void RET(ushort returnAddress) {
        gb.LoadROM(0xC9);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;

        gb.RAM.Write(0xD000, (byte)(returnAddress & 0xFF));
        gb.RAM.Write(0xD001, (byte)(returnAddress >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(returnAddress));
        Assert.That(CPU.Registers.SP, Is.EqualTo((ushort)0xD002));
    }
    
    [TestCase(false, (ushort)0x1234)] // Z=0 -> taken
    [TestCase(true,  (ushort)0x0100)] // Z=1 -> not taken
    public void RET_NZ(bool zero, ushort returnAddress) {
        gb.LoadROM(0xC0);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, zero);

        gb.RAM.Write(0xD000, (byte)(returnAddress & 0xFF));
        gb.RAM.Write(0xD001, (byte)(returnAddress >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(zero ? (ushort)0x0101 : returnAddress));
        Assert.That(CPU.Registers.SP, Is.EqualTo(zero ? (ushort)0xD000 : (ushort)0xD002));
    }
    
    [TestCase(false, (ushort)0x1234)] // Z=0 -> not taken
    [TestCase(true,  (ushort)0x0100)] // Z=1 -> taken
    public void RET_Z(bool zero, ushort returnAddress) {
        gb.LoadROM(0xC8);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, zero);

        gb.RAM.Write(0xD000, (byte)(returnAddress & 0xFF));
        gb.RAM.Write(0xD001, (byte)(returnAddress >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            zero ? returnAddress : (ushort)0x0101
        ));
        Assert.That(CPU.Registers.SP, Is.EqualTo(
            zero ? (ushort)0xD002 : (ushort)0xD000
        ));
    }

    [TestCase(false, (ushort)0x1234)] // C=0 -> not taken
    [TestCase(true,  (ushort)0x0100)] // C=1 -> taken
    public void RET_C(bool carry, ushort returnAddress) {
        gb.LoadROM(0xD8);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, carry);

        gb.RAM.Write(0xD000, (byte)(returnAddress & 0xFF));
        gb.RAM.Write(0xD001, (byte)(returnAddress >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            carry ? returnAddress : (ushort)0x0101
        ));
        Assert.That(CPU.Registers.SP, Is.EqualTo(
            carry ? (ushort)0xD002 : (ushort)0xD000
        ));
    }

    [TestCase(false, (ushort)0x1234)] // C=0 -> taken
    [TestCase(true,  (ushort)0x0100)] // C=1 -> not taken
    public void RET_NC(bool carry, ushort returnAddress) {
        gb.LoadROM(0xD0);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, carry);

        gb.RAM.Write(0xD000, (byte)(returnAddress & 0xFF));
        gb.RAM.Write(0xD001, (byte)(returnAddress >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            !carry ? returnAddress : (ushort)0x0101
        ));
        Assert.That(CPU.Registers.SP, Is.EqualTo(
            !carry ? (ushort)0xD002 : (ushort)0xD000
        ));
    }
    
    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0x0000)]
    [TestCase((ushort)0xFFFF)]
    [TestCase((ushort)0xA55A)]
    public void POP_BC(ushort value) {
        gb.LoadROM(0xC1);

        CPU.Registers.BC = 0;
        CPU.Registers.SP = 0xD000;

        gb.RAM.Write(0xD000, (byte)(value & 0xFF));
        gb.RAM.Write(0xD001, (byte)(value >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.BC, Is.EqualTo(value));
        Assert.That(CPU.Registers.SP, Is.EqualTo((ushort)0xD002));
    }

    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0x0000)]
    [TestCase((ushort)0xFFFF)]
    [TestCase((ushort)0xA55A)]
    public void POP_DE(ushort value) {
        gb.LoadROM(0xD1);

        CPU.Registers.DE = 0;
        CPU.Registers.SP = 0xD000;

        gb.RAM.Write(0xD000, (byte)(value & 0xFF));
        gb.RAM.Write(0xD001, (byte)(value >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.DE, Is.EqualTo(value));
        Assert.That(CPU.Registers.SP, Is.EqualTo((ushort)0xD002));
    }

    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0x0000)]
    [TestCase((ushort)0xFFFF)]
    [TestCase((ushort)0xA55A)]
    public void POP_HL(ushort value) {
        gb.LoadROM(0xE1);

        CPU.Registers.HL = 0;
        CPU.Registers.SP = 0xD000;

        gb.RAM.Write(0xD000, (byte)(value & 0xFF));
        gb.RAM.Write(0xD001, (byte)(value >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.HL, Is.EqualTo(value));
        Assert.That(CPU.Registers.SP, Is.EqualTo((ushort)0xD002));
    }

    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0x0000)]
    [TestCase((ushort)0xFFFF)]
    [TestCase((ushort)0xA55A)]
    public void POP_AF(ushort value) {
        gb.LoadROM(0xF1);

        CPU.Registers.AF = 0;
        CPU.Registers.SP = 0xD000;

        gb.RAM.Write(0xD000, (byte)(value & 0xFF));
        gb.RAM.Write(0xD001, (byte)(value >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo((byte)(value >> 8)));
        Assert.That(CPU.Registers.F, Is.EqualTo((byte)(value & 0xF0)));
        Assert.That(CPU.Registers.SP, Is.EqualTo((ushort)0xD002));
    }
    
    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0x0100)]
    [TestCase((ushort)0xFFFF)]
    [TestCase((ushort)0xA55A)]
    public void CALL(ushort target) {
        gb.LoadROM(0xCD, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(target));
        Assert.That(CPU.Registers.SP, Is.EqualTo((ushort)0xCFFE));

        Assert.That(
            gb.RAM.Read(0xCFFF),
            Is.EqualTo((byte)0x01)
        );

        Assert.That(
            gb.RAM.Read(0xCFFE),
            Is.EqualTo((byte)0x03)
        );
    }
    
    [TestCase(false, (ushort)0x1234)] // Z=0 -> taken
    [TestCase(true,  (ushort)0x1234)] // Z=1 -> not taken
    public void CALL_NZ(bool zero, ushort target) {
        gb.LoadROM(0xC4, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, zero);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            zero ? (ushort)0x0103 : target
        ));

        Assert.That(CPU.Registers.SP, Is.EqualTo(
            zero ? (ushort)0xD000 : (ushort)0xCFFE
        ));

        if (!zero) {
            Assert.That(gb.RAM.Read(0xCFFF), Is.EqualTo((byte)0x01));
            Assert.That(gb.RAM.Read(0xCFFE), Is.EqualTo((byte)0x03));
        }
    }
    
    [TestCase(false, (ushort)0x1234)] // Z=0 -> not taken
    [TestCase(true,  (ushort)0x1234)] // Z=1 -> taken
    public void CALL_Z(bool zero, ushort target) {
        gb.LoadROM(0xCC, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, zero);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            zero ? target : (ushort)0x0103
        ));

        Assert.That(CPU.Registers.SP, Is.EqualTo(
            zero ? (ushort)0xCFFE : (ushort)0xD000
        ));
    }

    [TestCase(false, (ushort)0x1234)] // C=0 -> not taken
    [TestCase(true,  (ushort)0x1234)] // C=1 -> taken
    public void CALL_C(bool carry, ushort target) {
        gb.LoadROM(0xDC, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, carry);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            carry ? target : (ushort)0x0103
        ));

        Assert.That(CPU.Registers.SP, Is.EqualTo(
            carry ? (ushort)0xCFFE : (ushort)0xD000
        ));
    }

    [TestCase(false, (ushort)0x1234)] // C=0 -> taken
    [TestCase(true,  (ushort)0x1234)] // C=1 -> not taken
    public void CALL_NC(bool carry, ushort target) {
        gb.LoadROM(0xD4, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, carry);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            !carry ? target : (ushort)0x0103
        ));

        Assert.That(CPU.Registers.SP, Is.EqualTo(
            !carry ? (ushort)0xCFFE : (ushort)0xD000
        ));
    }
    
    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0x0100)]
    [TestCase((ushort)0xFFFF)]
    [TestCase((ushort)0xA55A)]
    public void JP(ushort target) {
        gb.LoadROM(0xC3, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(target));
    }[TestCase(false, (ushort)0x1234)] // Z=0 -> taken
    [TestCase(true,  (ushort)0x1234)] // Z=1 -> not taken
    public void JP_NZ(bool zero, ushort target) {
        gb.LoadROM(0xC2, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, zero);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            zero ? (ushort)0x0103 : target
        ));
    }

    [TestCase(false, (ushort)0x1234)] // Z=0 -> not taken
    [TestCase(true,  (ushort)0x1234)] // Z=1 -> taken
    public void JP_Z(bool zero, ushort target) {
        gb.LoadROM(0xCA, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, zero);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            zero ? target : (ushort)0x0103
        ));
    }

    [TestCase(false, (ushort)0x1234)] // C=0 -> taken
    [TestCase(true,  (ushort)0x1234)] // C=1 -> not taken
    public void JP_NC(bool carry, ushort target) {
        gb.LoadROM(0xD2, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, carry);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            !carry ? target : (ushort)0x0103
        ));
    }

    [TestCase(false, (ushort)0x1234)] // C=0 -> not taken
    [TestCase(true,  (ushort)0x1234)] // C=1 -> taken
    public void JP_C(bool carry, ushort target) {
        gb.LoadROM(0xDA, (byte)(target & 0xFF), (byte)(target >> 8));

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, carry);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(
            carry ? target : (ushort)0x0103
        ));
    }
    
    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0x0000)]
    [TestCase((ushort)0xFFFF)]
    [TestCase((ushort)0xA55A)]
    public void LD_SP_nn(ushort value) {
        gb.LoadROM(0x31, (byte)(value & 0xFF), (byte)(value >> 8));

        CPU.Registers.SP = 0;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.SP, Is.EqualTo(value));
    }
    
    [TestCase((ushort)0x1234, (ushort)0xD000)]
    [TestCase((ushort)0x0000, (ushort)0xD100)]
    [TestCase((ushort)0xFFFF, (ushort)0xD200)]
    [TestCase((ushort)0xA55A, (ushort)0xD300)]
    public void LD_nn_SP(ushort value, ushort address) {
        gb.LoadROM(
            0x08,
            (byte)(address & 0xFF),
            (byte)(address >> 8)
        );

        CPU.Registers.SP = value;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(
            gb.RAM.Read(address),
            Is.EqualTo((byte)(value & 0xFF))
        );

        Assert.That(
            gb.RAM.Read((ushort)(address + 1)),
            Is.EqualTo((byte)(value >> 8))
        );
    }
    
    [TestCase((ushort)0x0000, (ushort)0x0000, false, false, false)]
    [TestCase((ushort)0x0001, (ushort)0x0001, false, false, false)]
    [TestCase((ushort)0x0FFF, (ushort)0x0001, false, true,  false)]
    [TestCase((ushort)0xFFFF, (ushort)0x0001, false, true,  true)]
    [TestCase((ushort)0xFFFF, (ushort)0xFFFF, false, true,  true)]
    [TestCase((ushort)0x1234, (ushort)0x5678, false, false, false)]
    public void ADD_HL_BC(
        ushort hl,
        ushort bc,
        bool expectedN,
        bool expectedH,
        bool expectedC) {
    
        gb.LoadROM(0x09);

        CPU.Registers.HL = hl;
        CPU.Registers.BC = bc;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, true);
        CPU.Registers.SetFlag(CPUFlagMask.Negative, true);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.HL, Is.EqualTo((ushort)(hl + bc)));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Zero), Is.True);
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Negative), Is.EqualTo(expectedN));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.HalfCarry), Is.EqualTo(expectedH));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.EqualTo(expectedC));
    }
    
    [TestCase((ushort)0x0000, (ushort)0x0000)]
    [TestCase((ushort)0x0FFF, (ushort)0x0001)]
    [TestCase((ushort)0xFFFF, (ushort)0x0001)]
    [TestCase((ushort)0x1234, (ushort)0x5678)]
    public void ADD_HL_DE(ushort hl, ushort de) {
        gb.LoadROM(0x19);

        CPU.Registers.HL = hl;
        CPU.Registers.DE = de;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.HL, Is.EqualTo((ushort)(hl + de)));
    }
    
    [TestCase((ushort)0x0000)]
    [TestCase((ushort)0x0001)]
    [TestCase((ushort)0x0FFF)]
    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0xFFFF)]
    public void ADD_HL_HL(ushort hl) {
        gb.LoadROM(0x29);

        CPU.Registers.HL = hl;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.HL, Is.EqualTo((ushort)(hl + hl)));
    }

    [TestCase((ushort)0x0000, (ushort)0x0000)]
    [TestCase((ushort)0x0FFF, (ushort)0x0001)]
    [TestCase((ushort)0xFFFF, (ushort)0x0001)]
    [TestCase((ushort)0x1234, (ushort)0x5678)]
    public void ADD_HL_SP(ushort hl, ushort sp) {
        gb.LoadROM(0x39);

        CPU.Registers.HL = hl;
        CPU.Registers.SP = sp;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.HL, Is.EqualTo((ushort)(hl + sp)));
    }
    
    [TestCase((byte)0x00, (byte)0x00, (byte)0x00)]
    [TestCase((byte)0x01, (byte)0x01, (byte)0x02)]
    [TestCase((byte)0x7F, (byte)0x01, (byte)0x80)]
    [TestCase((byte)0xFF, (byte)0x01, (byte)0x00)]
    [TestCase((byte)0x55, (byte)0xAA, (byte)0xFF)]
    public void ADD_A_B(byte a, byte b, byte expected) {
        gb.LoadROM(0x80);

        CPU.Registers.A = a;
        CPU.Registers.B = b;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(expected));
    }
    
    [TestCase(0x00, 0x00, 0x01)]
    [TestCase(0x01, 0x01, 0x03)]
    [TestCase(0x7F, 0x01, 0x81)]
    [TestCase(0xFF, 0x01, 0x01)]
    public void ADC_A_B(byte a, byte b, byte expected) {
        gb.LoadROM(0x88);

        CPU.Registers.A = a;
        CPU.Registers.B = b;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, true);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(expected));
    }

    [TestCase(0x00, 0x00, 0x00)]
    [TestCase(0x01, 0x01, 0x00)]
    [TestCase(0x80, 0x01, 0x7F)]
    [TestCase(0x00, 0x01, 0xFF)]
    public void SUB_A_B(byte a, byte b, byte expected) {
        gb.LoadROM(0x90);

        CPU.Registers.A = a;
        CPU.Registers.B = b;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(expected));
    }

    [TestCase(0x00, 0x00, 0xFF)]
    [TestCase(0x01, 0x00, 0x00)]
    [TestCase(0x80, 0x01, 0x7E)]
    [TestCase(0x00, 0x01, 0xFE)]
    public void SBC_A_B(byte a, byte b, byte expected) {
        gb.LoadROM(0x98);

        CPU.Registers.A = a;
        CPU.Registers.B = b;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, true);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(expected));
    }
    
    [TestCase(0x00, 0x00, 0x00)]
    [TestCase(0xFF, 0x0F, 0x0F)]
    [TestCase(0x55, 0xAA, 0x00)]
    [TestCase(0xF0, 0x0F, 0x00)]
    public void AND_A_B(byte a, byte b, byte expected) {
        gb.LoadROM(0xA0);

        CPU.Registers.A = a;
        CPU.Registers.B = b;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(expected));
    }

    [TestCase(0x00, 0x00, 0x00)]
    [TestCase(0xFF, 0x0F, 0xF0)]
    [TestCase(0x55, 0xAA, 0xFF)]
    [TestCase(0xF0, 0x0F, 0xFF)]
    public void XOR_A_B(byte a, byte b, byte expected) {
        gb.LoadROM(0xA8);

        CPU.Registers.A = a;
        CPU.Registers.B = b;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(expected));
    }

    [TestCase(0x00, 0x00, 0x00)]
    [TestCase(0xFF, 0x0F, 0xFF)]
    [TestCase(0x55, 0xAA, 0xFF)]
    [TestCase(0xF0, 0x0F, 0xFF)]
    public void OR_A_B(byte a, byte b, byte expected) {
        gb.LoadROM(0xB0);

        CPU.Registers.A = a;
        CPU.Registers.B = b;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(expected));
    }
    
    [TestCase(0x00, 0x00)]
    [TestCase(0x01, 0x00)]
    [TestCase(0x00, 0x01)]
    [TestCase(0x55, 0x55)]
    [TestCase(0xFF, 0x01)]
    public void CP_A_B(byte a, byte b) {
        gb.LoadROM(0xB8);

        CPU.Registers.A = a;
        CPU.Registers.B = b;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(a));
    }
    
    [TestCase(0xC7, (ushort)0x0000)]
    [TestCase(0xCF, (ushort)0x0008)]
    [TestCase(0xD7, (ushort)0x0010)]
    [TestCase(0xDF, (ushort)0x0018)]
    [TestCase(0xE7, (ushort)0x0020)]
    [TestCase(0xEF, (ushort)0x0028)]
    [TestCase(0xF7, (ushort)0x0030)]
    [TestCase(0xFF, (ushort)0x0038)]
    public void RST(byte opcode, ushort target) {
        gb.LoadROM(opcode);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(target));
        Assert.That(CPU.Registers.SP, Is.EqualTo((ushort)0xCFFE));
        Assert.That(gb.RAM.Read(0xCFFF), Is.EqualTo((byte)0x01));
        Assert.That(gb.RAM.Read(0xCFFE), Is.EqualTo((byte)0x01));
    }
    
    [TestCase((ushort)0x1234)]
    [TestCase((ushort)0x0100)]
    [TestCase((ushort)0xFFFF)]
    [TestCase((ushort)0xA55A)]
    public void RETI(ushort returnAddress) {
        gb.LoadROM(0xD9);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SP = 0xD000;
        CPU.interrupt_master_enable = false;

        gb.RAM.Write(0xD000, (byte)(returnAddress & 0xFF));
        gb.RAM.Write(0xD001, (byte)(returnAddress >> 8));

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(returnAddress));
        Assert.That(CPU.Registers.SP, Is.EqualTo((ushort)0xD002));
        Assert.That(CPU.interrupt_master_enable, Is.True);
    }
    
    [TestCase((byte)0x80, (byte)0x12)]
    [TestCase((byte)0x81, (byte)0x34)]
    [TestCase((byte)0x90, (byte)0x56)]
    [TestCase((byte)0xFE, (byte)0x78)]
    public void LDH_n_A(byte offset, byte value) {
        gb.LoadROM(0xE0, offset);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.A = value;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(
            gb.RAM.Read((ushort)(0xFF00 + offset)),
            Is.EqualTo(value)
        );

        Assert.That(CPU.Registers.PC, Is.EqualTo((ushort)0x0102));
    }
    
    [TestCase((byte)0x80, (byte)0x12)]
    [TestCase((byte)0x81, (byte)0x34)]
    [TestCase((byte)0x90, (byte)0x56)]
    [TestCase((byte)0xFE, (byte)0x78)]
    public void LDH_A_n(byte offset, byte value) {
        gb.LoadROM(0xF0, offset);

        gb.RAM.Write((ushort)(0xFF00 + offset), value);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(value));
    }
    
    [TestCase((byte)0x80, (byte)0x12)]
    [TestCase((byte)0x81, (byte)0x34)]
    [TestCase((byte)0x90, (byte)0x56)]
    [TestCase((byte)0xFE, (byte)0x78)]
    public void LDH_C_A(byte c, byte value) {
        gb.LoadROM(0xE2);

        CPU.Registers.C = c;
        CPU.Registers.A = value;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(
            gb.RAM.Read((ushort)(0xFF00 + c)),
            Is.EqualTo(value)
        );
    }
    
    [TestCase((byte)0x80, (byte)0x12)]
    [TestCase((byte)0x81, (byte)0x34)]
    [TestCase((byte)0x90, (byte)0x56)]
    [TestCase((byte)0xFE, (byte)0x78)]
    public void LDH_A_C(byte c, byte value) {
        gb.LoadROM(0xF2);

        CPU.Registers.C = c;
        gb.RAM.Write((ushort)(0xFF00 + c), value);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(value));
    }
    
    [TestCase((byte)0x80, (byte)0x12)]
    [TestCase((byte)0x81, (byte)0x34)]
    [TestCase((byte)0x90, (byte)0x56)]
    [TestCase((byte)0xFE, (byte)0x78)]
    public void LDH_n_A_WritesCorrectAddress(byte offset, byte value) {
        gb.LoadROM(0xE0, offset);

        CPU.Registers.A = value;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(
            gb.RAM.Read((ushort)(0xFF00 + offset)),
            Is.EqualTo(value)
        );
    }
    
    [TestCase(true,  (ushort)0x0102)] // Z=1 -> not taken
    [TestCase(false, (ushort)0x00FE)] // Z=0 -> taken by -4
    public void JR_NZ_ConsumesOperand(bool zero, ushort expected) {
        gb.LoadROM(0x20, 0xFC);

        CPU.Registers.PC = 0x0100;
        CPU.Registers.SetFlag(CPUFlagMask.Zero, zero);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.PC, Is.EqualTo(expected));
    }
    [Test]
    public void CB_RLC_B() {
        gb.LoadROM(0xCB, 0x00);
        CPU.Registers.B = 0x80;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x01));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.True);
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Zero), Is.False);
    }

    [Test]
    public void CB_RRC_B() {
        gb.LoadROM(0xCB, 0x08);
        CPU.Registers.B = 0x01;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x80));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.True);
    }

    [Test]
    public void CB_RL_B() {
        gb.LoadROM(0xCB, 0x10);
        CPU.Registers.B = 0x80;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, true);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x01));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.True);
    }

    [Test]
    public void CB_RR_B() {
        gb.LoadROM(0xCB, 0x18);
        CPU.Registers.B = 0x01;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, true);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x80));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.True);
    }

    [Test]
    public void CB_SLA_B() {
        gb.LoadROM(0xCB, 0x20);
        CPU.Registers.B = 0x81;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x02));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.True);
    }

    [Test]
    public void CB_SRA_B() {
        gb.LoadROM(0xCB, 0x28);
        CPU.Registers.B = 0x81;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0xC0));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.True);
    }

    [Test]
    public void CB_SRL_B() {
        gb.LoadROM(0xCB, 0x38);
        CPU.Registers.B = 0x81;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x40));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.True);
    }

    [Test]
    public void CB_SWAP_B() {
        gb.LoadROM(0xCB, 0x30);
        CPU.Registers.B = 0xF0;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x0F));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Zero), Is.False);
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Negative), Is.False);
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.HalfCarry), Is.False);
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.False);
    }
    
    [Test]
    public void CB_BIT_Bit0_Set() {
        gb.LoadROM(0xCB, 0x40); // BIT 0,B

        CPU.Registers.B = 0x01;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x01));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Zero), Is.False);
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Negative), Is.False);
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.HalfCarry), Is.True);
    }

    [Test]
    public void CB_BIT_Bit0_Clear() {
        gb.LoadROM(0xCB, 0x40);

        CPU.Registers.B = 0x00;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x00));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Zero), Is.True);
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Negative), Is.False);
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.HalfCarry), Is.True);
    }
    [Test]
    public void CB_RES_Bit0_B() {
        gb.LoadROM(0xCB, 0x80); // RES 0,B

        CPU.Registers.B = 0xFF;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0xFE));
    }

    [Test]
    public void CB_SET_Bit0_B() {
        gb.LoadROM(0xCB, 0xC0); // SET 0,B

        CPU.Registers.B = 0x00;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.B, Is.EqualTo(0x01));
    }

    [Test]
    public void CB_SET_Bit7_B() {
        gb.LoadROM(0xCB, 0xFF); // SET 7,A

        CPU.Registers.A = 0x00;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(0x80));
    }
    
    [Test]
    public void CB_RLC_HL() {
        gb.LoadROM(0xCB, 0x06); // RLC (HL)

        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0x80);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(gb.RAM.Read(0xC000), Is.EqualTo(0x01));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Carry), Is.True);
    }

    [Test]
    public void CB_BIT_HL() {
        gb.LoadROM(0xCB, 0x46); // BIT 0,(HL)

        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0x00);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(gb.RAM.Read(0xC000), Is.EqualTo(0x00));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Zero), Is.True);
    }

    [Test]
    public void CB_RES_HL() {
        gb.LoadROM(0xCB, 0x86); // RES 0,(HL)

        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0xFF);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(gb.RAM.Read(0xC000), Is.EqualTo(0xFE));
    }

    [Test]
    public void CB_SET_HL() {
        gb.LoadROM(0xCB, 0xC6); // SET 0,(HL)

        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0x00);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(gb.RAM.Read(0xC000), Is.EqualTo(0x01));
    }
    
    [Test]
    public void ADD_A_HL() {
        gb.LoadROM(0x86); // ADD A,(HL)

        CPU.Registers.A = 0x12;
        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0x34);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(0x46));
    }

    [Test]
    public void ADC_A_HL() {
        gb.LoadROM(0x8E); // ADC A,(HL)

        CPU.Registers.A = 0x12;
        CPU.Registers.HL = 0xC000;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, true);
        gb.RAM.Write(0xC000, 0x34);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(0x47));
    }

    [Test]
    public void SUB_A_HL() {
        gb.LoadROM(0x96); // SUB (HL)

        CPU.Registers.A = 0x46;
        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0x12);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(0x34));
    }

    [Test]
    public void SBC_A_HL() {
        gb.LoadROM(0x9E); // SBC A,(HL)

        CPU.Registers.A = 0x46;
        CPU.Registers.HL = 0xC000;
        CPU.Registers.SetFlag(CPUFlagMask.Carry, true);
        gb.RAM.Write(0xC000, 0x12);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(0x33));
    }

    [Test]
    public void AND_A_HL() {
        gb.LoadROM(0xA6); // AND (HL)

        CPU.Registers.A = 0xF0;
        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0x3C);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(0x30));
    }

    [Test]
    public void XOR_A_HL() {
        gb.LoadROM(0xAE); // XOR (HL)

        CPU.Registers.A = 0xF0;
        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0x3C);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(0xCC));
    }

    [Test]
    public void OR_A_HL() {
        gb.LoadROM(0xB6); // OR (HL)

        CPU.Registers.A = 0xF0;
        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0x3C);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(0xFC));
    }

    [Test]
    public void CP_A_HL() {
        gb.LoadROM(0xBE); // CP (HL)

        CPU.Registers.A = 0x3C;
        CPU.Registers.HL = 0xC000;
        gb.RAM.Write(0xC000, 0x3C);

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.Registers.A, Is.EqualTo(0x3C));
        Assert.That(CPU.Registers.GetFlag(CPUFlagMask.Zero), Is.True);
        Assert.That(CPU.Registers.A, Is.EqualTo(0x3C));
    }

    [Test]
    public void EI() {
        gb.LoadROM(0xFB, 0x00, 0x00); // CP (HL)

        CPU.interrupt_master_enable = false;
        
        while (CPU.ops < 1) CPU.Tick();

        Assert.That(CPU.interrupt_master_enable, Is.False);

        while (CPU.ops < 2) CPU.Tick();

        Assert.That(CPU.interrupt_master_enable, Is.True);
    }
    
    [Test]
    public void EI_DelaysInterrupt() {
        gb.LoadROM(0xFB, 0x00, 0x00); // EI, NOP, NOP

        CPU.interrupt_master_enable = false;
        CPU.Registers.IE = 0x01;
        CPU.Registers.IF = 0x01;

        // Execute EI
        while (CPU.ops < 1)
            CPU.Tick();

        Assert.That(CPU.interrupt_master_enable, Is.False);

        // Execute NOP
        while (CPU.ops < 2)
            CPU.Tick();

        Assert.That(CPU.interrupt_master_enable, Is.True);
    }
    
    [TestCase((byte)0x0F, (byte)0x01, true)]
    [TestCase((byte)0x0E, (byte)0x01, false)]
    [TestCase((byte)0xFF, (byte)0x01, true)]
    public void ADD_A_B_HalfCarry(byte a, byte b, bool expected) {
        gb.LoadROM(0x80);

        CPU.Registers.A = a;
        CPU.Registers.B = b;

        while (CPU.ops < 1) CPU.Tick();

        Assert.That(
            CPU.Registers.GetFlag(CPUFlagMask.HalfCarry),
            Is.EqualTo(expected)
        );
    }
}