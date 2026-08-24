using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RatGBLib;

namespace RatGB;

class GameboyWithBuffer {
    private GameBoy gameboy;
    
    private Texture2D texture;
    private Color[] frame_buffer = new Color[160 * 144];
    
    private Input input = new Input();
    
    private int cycles = 0;
    
    public GameboyWithBuffer(GraphicsDevice gd) {
        gameboy = new GameBoy();
        texture = new Texture2D(gd, 160, 144);
    }

    public void LoadROM(string filename) {
        gameboy.LoadROM(filename);
    }
    
    public void Update() {
        input.Update(gameboy.joypad);
        
        while (cycles < GameBoy.CYCLES_PER_FRAME) {
            cycles += gameboy.Execute();
        }
        
        cycles = 0;
    }

    public void Draw(SpriteBatch sb, Vector2 position) {
        for (int c = 0; c < 160 * 144; c++) {
            byte b = gameboy.PPU.frame_buffer[c];
            switch (b) {
                case 0:
                    frame_buffer[c] = new Color(255, 255, 255);
                    break;
                case 1: 
                    frame_buffer[c] = new Color(255, 0, 0);
                    break;
                case 2: 
                    frame_buffer[c] = new Color(0, 255, 0);
                    break;
                case 3: 
                    frame_buffer[c] = new Color(0, 0, 255);
                    break;
            }
        }
        
        texture.SetData(frame_buffer);
        
        sb.Draw(texture, new Rectangle((int)position.X, (int)position.Y, 160, 144), Color.White);
    }
}