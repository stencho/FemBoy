namespace FemBoy;

public class OAMSearch {
    private GameBoy gameboy;
    private PPU PPU => gameboy.PPU;
    
    public List<Sprite> visible_sprites = new();

    private int index = 0;
    private int LY = 0;
    
    public string OAM_List = "";
    
    public OAMSearch(GameBoy gameboy) {
        this.gameboy = gameboy;
    }
    
    public void Start(int LY) {
        OAM_List = "";
        foreach (Sprite s in visible_sprites) {
            OAM_List += $" [{s.index}] [Pos] {s.X}x{s.Y}\n";
        } 
        
        visible_sprites.Clear();
        index = 0;
        this.LY = LY;
    }

    public void Reset() {
        visible_sprites.Clear();
        index = 0;
        LY = 0;
    }
    
    public void Tick() {
        //if (visible_sprites.Count == 10) return;
        if ((PPU.dot % 2) == 0) return;
        
        ushort address = (ushort)(0xFE00 + index * 4);
        int y = gameboy.RAM.Read(address) - 16;
        
        if (LY >= y && LY < y + PPU.SpriteHeight && visible_sprites.Count < 10) {
            visible_sprites.Add(new Sprite(gameboy, address, index));
        }

        index++;
    }
}