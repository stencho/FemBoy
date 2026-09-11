namespace FemBoy.TestHarness;

public class GBMicroTestROM : ITestROM {
    public string rom_name { get; set; } = "";
    public string filename { get; set; } = "";
    public PassState pass_state { get; set; } = PassState.UNFINISHED;
}

public static class GBMicroTest {
    private static GameBoy gb;
    public static List<GBMicroTestROM> test_roms = new List<GBMicroTestROM>();
    
    static void Setup(GBMicroTestROM rom) {
        gb = new GameBoy();
        
        gb.WriteMonitor += (address, value) => {
            if (address == 0xFF82) {
                if (value == 0x01) rom.pass_state = PassState.PASS;
                else rom.pass_state = PassState.FAIL;
            }
        };

        gb.LoadROM(rom.filename);
    }
    
    public static void RunTests() {
        string base_directory = "gbmicrotest/";
        var rom_search_path = Path.Combine(AppContext.BaseDirectory, base_directory);

        if (Directory.Exists(rom_search_path)) {
            var roms = Directory.GetFiles(base_directory, "*.gb", SearchOption.AllDirectories);

            foreach (var rp in roms.Order()) {
                FileInfo fi = new FileInfo(rp);

                string rname = rp.Remove(0, base_directory.Length);
                rname = rname.Remove(rname.Length - 3, 3);

                GBMicroTestROM mtr = new GBMicroTestROM();

                mtr.filename = fi.FullName;

                if (rname.Contains(Path.VolumeSeparatorChar)) {
                    mtr.rom_name = rname.Remove(0, rname.IndexOf(Path.VolumeSeparatorChar) + 1);
                } else
                    mtr.rom_name = rname;

                test_roms.Add(mtr);
            }
        }

        test_roms = test_roms.OrderBy(a => a.rom_name).ToList();
        string last_rom_folder = "";
            
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"[GBMicroTest]");
        
        for (var index = 0; index < test_roms.Count; index++) {
            var test_rom = test_roms[index];
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(test_rom.rom_name + "...");

            Setup(test_roms[index]);

            var start_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
            while (test_rom.pass_state == PassState.UNFINISHED) {
                gb.Tick();
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start_time > 1000) test_rom.pass_state = PassState.DNF;
            }

            Console.CursorLeft -= 3;

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" :: ");

            if (test_rom.pass_state == PassState.PASS) {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("PASS\n");
                    
            } else if (test_rom.pass_state == PassState.DNF) {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("DNF\n");
                    
            } else if (test_rom.pass_state == PassState.FAIL) {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("FAIL\n");
            } 
                
            Console.ForegroundColor = ConsoleColor.Gray;
        }
        
        Console.WriteLine();
    }

}