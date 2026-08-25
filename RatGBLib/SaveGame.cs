namespace RatGBLib;

public static class SaveGame {
    private static string save_path = "saves";

    public static string GetSaveFileName(uint CRC) => Path.Combine(save_path, CRC.ToString("X8") + ".sav");

    public static bool CurrentlySaving = false;
    
    public static void Save(uint CRC, byte[] RAM) {
        Interlocked.Exchange(ref CurrentlySaving, true);
        string save_file = GetSaveFileName(CRC);

        if (!Directory.Exists(save_path)) {
            Directory.CreateDirectory(save_path);
        }
        
        try {
            File.WriteAllBytes(save_file, RAM);
            Console.WriteLine($"Saved game to {save_file}");
        } catch (IOException ex) {
            Console.WriteLine($"Failed to write save: {ex.Message}");
        }
        
        Interlocked.Exchange(ref CurrentlySaving, false);
    }

    public static byte[] Load(uint CRC, int ram_size) {
        string save_file = GetSaveFileName(CRC); 
        
        if (File.Exists(save_file)) {
            byte[] save = File.ReadAllBytes(save_file);
            
            if (save.Length != ram_size) 
                Console.WriteLine($"Save file \"{save_file}\" corrupted, incorrect size");
            else return save;
        }
        
        return new byte[ram_size];
    }
}