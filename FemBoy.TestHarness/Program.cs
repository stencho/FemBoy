namespace FemBoy.TestHarness;

public enum PassState { UNFINISHED, PASS, FAIL, DNF }

public interface ITestROM {
    public string rom_name { get; set; }
    public string filename { get; set; }

    public PassState pass_state { get; set; }
}

class Program {
    static void Main(string[] args) {
        bool run_mooneye = false;
        bool run_gbmicro = false;
        bool run_blargg  = false;

        foreach (string a in args) {
            if (a == "--mooneye" || a == "-m") {
                run_mooneye = true;
            } else if (a == "--gbmicro" || a == "-g") {
                run_gbmicro = true;
            } else if (a == "--blargg" || a == "-b") {
                run_blargg = true;
            }
        }

        if (!run_mooneye && !run_gbmicro && !run_blargg) {
            run_mooneye = true;
            run_gbmicro = true;
            run_blargg = true;
        }
        
        if (run_mooneye) Mooneye.RunTests();
        if (run_gbmicro) GBMicroTest.RunTests();
        if (run_blargg)  Blargg.RunTests();
        
        /*
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine();
        Console.WriteLine($"[Diff]");
        */
        
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine();
        Console.WriteLine($"[Results]");
        
        if (run_mooneye) PrintResults(Mooneye.test_roms, "Mooneye");
        if (run_gbmicro) PrintResults(GBMicroTest.test_roms, "GBMicroTest");
        if (run_blargg)  PrintResults(Blargg.test_roms, "Blargg");
    }
    
    
    public static void PrintResults(IEnumerable<ITestROM> test_roms, string suite) {
        int pass_count = test_roms.Count(a => a.pass_state == PassState.PASS);
        int dnf_count = test_roms.Count(a => a.pass_state == PassState.DNF);
        int fail_count = test_roms.Count(a => a.pass_state == PassState.FAIL);
        
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"[{suite}]");
            
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.Write("PASS ");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write("DNF  ");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Write("FAIL ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("TOTAL");
            
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.Write($"{pass_count, -5}");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write($"{dnf_count, -5}");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Write($"{fail_count, -5}");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"{test_roms.Count(), -5}");
    }
}