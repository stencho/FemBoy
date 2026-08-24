using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RatGBLib;

namespace RatGB;

public class Game1 : Game {
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private GameBoy gb = new GameBoy();

    private Texture2D memory_texture;
    private Texture2D frame_texture;
    
    private Color[] memory_colors = new Color[256 * 256];
    private Color[] frame_buffer = new Color[160 * 144];
    
    private Input input = new Input();
    
    public Game1() {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 512;
        _graphics.ApplyChanges();
        
        IsFixedTimeStep = true;
        
        Console.SetOut(TextWriter.Null);
        
        //gb.LoadROM("gbmicrotest/000-write_to_x8000.gb");
        //gb.LoadROM("bully.gb");
        //gb.LoadROM("alley.gb");
        
        //gb.LoadROM("tetris.gb");
        gb.LoadROM("kirby.gb");
        //gb.LoadROM("zelda.gb");
        //gb.LoadROM("buttontest.gb");
        //gb.LoadROM("int.gb");
        
        
        //gb.LoadROM("mooneye-test-suite/acceptance/timer/rapid_toggle.gb");
        //gb.LoadROM("mooneye-test-suite/emulator-only/mbc1/ram_64kb.gb");
        
        //gb.LoadROM("mooneye-test-suite/acceptance/timer/tma_write_reloading.gb");
        //gb.LoadROM("mooneye-test-suite/acceptance/timer/tima_write_reloading.gb");
        //gb.LoadROM("mooneye-test-suite/acceptance/timer/div_write.gb");
        //gb.LoadROM("mooneye-test-suite/acceptance/timer/tima_reload.gb");
        
        //gb.LoadROM("mooneye-test-suite/acceptance/push_timing.gb");
        //gb.LoadROM("mooneye-test-suite/acceptance/pop_timing.gb");
        //gb.LoadROM("mooneye-test-suite/acceptance/oam_dma/sources-GS.gb");
        
        //gb.LoadROM("blargg/instr_timing/instr_timing.gb");
        //gb.LoadROM("blargg/halt_bug.gb");
        
        //gb.LoadROM("blargg/cpu_instrs/cpu_instrs.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/01-special.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/02-interrupts.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/03-op sp,hl.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/04-op r,imm.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/05-op rp.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/06-ld r,r.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/07-jr,jp,call,ret,rst.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/08-misc instrs.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/09-op r,r.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/10-bit ops.gb");
        //gb.LoadROM("blargg/cpu_instrs/individual/11-op a,(hl).gb");
        
        //gb.LoadROM("hb.gb");
    }

    protected override void Initialize() {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        memory_texture = new Texture2D(GraphicsDevice, 256, 256, false, SurfaceFormat.Color);
        frame_texture = new Texture2D(GraphicsDevice, 160, 144, false, SurfaceFormat.Color);
        
        // TODO: use this.Content to load your game content here
        
    }

    private int cycles = 0;
    
    protected override void Update(GameTime gameTime) {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        
        input.Update(gb.joypad);
        
        while (cycles < GameBoy.CYCLES_PER_FRAME) {
            cycles += gb.Execute();
        }
        
        cycles = 0;
        
        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        
        for (int c = 0; c < 160 * 144; c++) {
            byte b = gb.PPU.frame_buffer[c];
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
        
        for (int i = 0; i < 0x10000; i++) {
            byte value = gb.ReadByte((ushort)i);
            
            if (i <= 0x3FFF) {
                memory_colors[i] = new Color(value, 0, 0);    
            } else if (i <= 0x7FFF) {
                memory_colors[i] = new Color(0, value, 0);    
            } else if (i <= 0x9FFF) {
                memory_colors[i] = new Color(0, 0, value);

            } else if (i == 0xFF00) {
                memory_colors[i] = new Color(value, 0, value);

            } else memory_colors[i] = new Color(value, value, value);
        }
        
        memory_texture.SetData(memory_colors);
        frame_texture.SetData(frame_buffer);
        
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);
        _spriteBatch.Draw(memory_texture, new Rectangle(0, 0, 512, 512), Color.White);
        _spriteBatch.Draw(frame_texture, new Rectangle(512, 0, 160 * 3, 144 * 3), Color.White);
        
        
        
        _spriteBatch.End();
        // TODO: Add your drawing code here

        base.Draw(gameTime);
    }
}