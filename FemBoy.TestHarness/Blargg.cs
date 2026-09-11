using System.Text;

namespace FemBoy.TestHarness;

public class BlarggTestROM : ITestROM {
    public string rom_folder = "";
    
    public string rom_name { get; set; } = "";
    public string filename { get; set; } = "";
    public PassState pass_state { get; set; }= PassState.UNFINISHED;
}

public static class Blargg {
    private static GameBoy gb;
    public static List<BlarggTestROM> test_roms = new List<BlarggTestROM>();

    private static bool test_finished = false;

    private static char[] pass = [
         'P', 'a', 's', 's', 'e', 'd' 
    ];
    private static char[] fail = [
         'F', 'a', 'i', 'l', 'e', 'd' 
    ];

    private static char[] serial_buffer = new char[6];
    
    static void Setup(BlarggTestROM rom) {
        gb = new GameBoy();
        
        gb.WriteMonitor += (address, value) => {
            if (address == 0xA000) {
                if (value != 0x80) {
                    test_finished = true;
                    if (value == 0x00) rom.pass_state = PassState.PASS;
                    else rom.pass_state = PassState.FAIL;
                }
            }

            if (address == SerialRegisterAddresses.SB) {
                for (int i = 0; i < serial_buffer.Length-1; i++) serial_buffer[i] = serial_buffer[i + 1];
                serial_buffer[5] = (char)value;

                int pass_matches = 0;
                int fail_matches = 0;
                
                for (int i = 0; i < serial_buffer.Length; i++) {
                    if (pass[i] == serial_buffer[i]) pass_matches++;
                    if (fail[i] == serial_buffer[i]) fail_matches++;
                }

                if (pass_matches == 6) rom.pass_state = PassState.PASS;
                else if (fail_matches == 6) rom.pass_state = PassState.FAIL;
            }
        };

        gb.LoadROM(rom.filename);
    }

    public static void RunTests() {
        string base_directory = "blargg/";
        var rom_search_path = Path.Combine(AppContext.BaseDirectory, base_directory);

        if (Directory.Exists(rom_search_path)) {
            var roms = Directory.GetFiles(base_directory, "*.gb", SearchOption.AllDirectories);

            foreach (var rp in roms.Order()) {
                FileInfo fi = new FileInfo(rp);

                string rname = rp.Remove(0, base_directory.Length);
                rname = rname.Remove(rname.Length - 3, 3);

                BlarggTestROM mtr = new BlarggTestROM();

                mtr.filename = fi.FullName;

                mtr.rom_folder = fi.Directory.Name;
                
                if (rname.Contains(Path.VolumeSeparatorChar)) {
                    mtr.rom_name = rname.Remove(0, rname.IndexOf(Path.VolumeSeparatorChar) + 1);
                } else
                    mtr.rom_name = rname;

                test_roms.Add(mtr);
            }
            
            test_roms = test_roms.OrderBy(a => a.rom_folder).ToList();
            string last_rom_folder = "";
            
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"[Blargg]");


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
                int cycle = 0;
                test_finished = false;
                
                while (test_rom.pass_state == PassState.UNFINISHED) {
                    gb.Tick();
                    if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start_time > 20000) test_rom.pass_state = PassState.DNF;
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
        }
    }
}