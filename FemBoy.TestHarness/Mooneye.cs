using System.Threading.Tasks.Sources;

namespace FemBoy.TestHarness;

public static class Mooneye {
    private static GameBoy gb;
    
    enum PassState { UNFINISHED, PASS, FAIL, DNF }
    
    class MooneyeTestROM {
        public string rom_folder = "";
        public string rom_name = "";
        public string filename = "";
        
        public PassState pass_state = PassState.UNFINISHED;

        public bool WaitingForSerialWrites = false;
        public int WriteCount = 0;
    }

    private static readonly byte[] pass_writes = [
        0x03, 0x05, 0x08, 0x0D, 0x15, 0x22
    ];
    private static readonly byte[] fail_writes = [
        0x42, 0x42, 0x42, 0x42, 0x42, 0x42
    ];
    
    static void Setup(MooneyeTestROM rom) {
        gb = new GameBoy();
        
        gb.CPU.ChangedOpcode += (op) => {
            if (op == 0x40) {
                rom.WaitingForSerialWrites = true;
            }
        };
        
        gb.WriteMonitor += (address, value) => {
            if (address == SerialRegisterAddresses.SB) {
                if (value == pass_writes[rom.WriteCount]) {
                    rom.WriteCount++;
                    if (rom.WriteCount >= 5) rom.pass_state = PassState.PASS;
                } else if (value == 0x42) rom.pass_state = PassState.FAIL;
            }
        };

        gb.LoadROM(rom.filename);
        
    }

    public static void RunTests() {
        string base_directory = "mooneye-test-suite/acceptance/";
        var rom_search_path = Path.Combine(AppContext.BaseDirectory, base_directory);
        
        List<MooneyeTestROM> test_roms = new List<MooneyeTestROM>();
        
        if (Directory.Exists(rom_search_path)) {
            var roms = Directory.GetFiles(base_directory, "*.gb", SearchOption.AllDirectories); 
            
            foreach (var rp in roms.Order()) {
                FileInfo fi = new FileInfo(rp);
                
                string rname = rp.Remove(0, base_directory.Length);
                rname = rname.Remove(rname.Length - 3, 3);
                
                MooneyeTestROM mtr = new MooneyeTestROM();

                mtr.filename = fi.FullName;
                
                mtr.rom_folder = fi.Directory.Name;
                if (mtr.rom_folder == "acceptance") mtr.rom_folder = "";

                if (rname.Contains(Path.VolumeSeparatorChar)) {
                    mtr.rom_name = rname.Remove(0, rname.IndexOf(Path.VolumeSeparatorChar) + 1);
                } else
                    mtr.rom_name = rname;
                
                if (   !mtr.rom_name.EndsWith("-S")
                    && !mtr.rom_name.EndsWith("-mgb") 
                    && !mtr.rom_name.EndsWith("-dmg0") 
                    && !mtr.rom_name.EndsWith("-sgb") 
                    && !mtr.rom_name.EndsWith("-sgb2")) 
                    test_roms.Add(mtr);
            }

            test_roms = test_roms.OrderBy(a => a.rom_folder).ToList();
            string last_rom_folder = "";
            
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"[Mooneye]");
            
            for (var index = 0; index < test_roms.Count; index++) {
                var test_rom = test_roms[index];
                if (last_rom_folder != test_rom.rom_folder) {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine();
                    Console.WriteLine($"[{test_rom.rom_folder}]");
                    last_rom_folder = test_rom.rom_folder;
                }

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

            int pass_count = test_roms.Count(a => a.pass_state == PassState.PASS);
            int dnf_count = test_roms.Count(a => a.pass_state == PassState.DNF);
            int fail_count = test_roms.Count(a => a.pass_state == PassState.FAIL);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine();
            Console.WriteLine($"[Result]");
            
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
            Console.WriteLine($"{test_roms.Count, -5}");
            
            
            
        } else {
            Console.WriteLine($"");
        }
    }
}