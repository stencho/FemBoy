using FemBoy;
using NUnit.Framework;
using Timer = FemBoy.Timer;

[TestFixture]
public class TimerTests {
    [Test]
    public void Timer_Mode01_ShouldIncrementTimaOnExactFallingEdge() {
        var gb = new GameBoy(); 
        gb.LoadROM(new byte[1024*4]);
        
        gb.WriteMemory(0xFF07, 0x05); 
        gb.Timer.DebugSetDivider(0); 
        gb.WriteMemory(0xFF05, 0x00);
        
        int t = 0;
        for (int i = 0; i < 8; i++) {
            t++;
            Console.WriteLine($"TICK {t}");
            gb.Tick(); 
            Assert.That(gb.ReadMemory(0xFF05), Is.EqualTo((byte)0x00)); 
        }

        Assert.That(gb.ReadMemory(0xFF05), Is.EqualTo((byte)0x00)); 

        for (int i = 0; i < 7; i++) {
            t++;
            Console.WriteLine($"TICK {t}");
            var old = gb.Timer.GetTimerSignal();
            gb.Tick();
            
            if (old && !gb.Timer.GetTimerSignal())
                Console.WriteLine("FALLING EDGE");
        }

        t++;
        Console.WriteLine($"TICK {t}");
        gb.Tick();
        Assert.That(gb.ReadMemory(0xFF05), Is.EqualTo((byte)0x01)); 
    }

}
