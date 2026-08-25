using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.Utilities;
using RatGB;
using RatGBLib;
using Raven.Console;
using Raven.Engine;
using Raven.Engine.Controls;
using Raven.Engine.Scene3D;
using Raven.Graphics.Drawing2D;
using Raven.UI;
using WaterTrans.GlyphLoader;

namespace RatGB;

public class RatGBGame : Game {
    private GraphicsDeviceManager _graphics;
    private bool windows = true;
    
    public static FullResolutionRenderTarget output_render_target;
    public FullResolutionRenderTarget game_render_target;

    //public static GameBoy gameboy = new GameBoy();
    private static GameboyEmulator gameboy;
    public static GameboyEmulator gb => gameboy;
    private Texture2D memory_texture;
    private Color[] memory_colors = new Color[256 * 256];
    
    internal static (string bind, object[] bind_data)[]
        bind_list = [
            ("toggle_memory_window", [Keys.F1]),
        ];

    internal static (string bind, object[] bind_data)[]
        emu_bind_list = [
            ("copy_debug_info", [Keys.Insert]),
            ("reload_rom", [Keys.R]),
        ];
    public static BindWatcher ui_binds;
    public static BindWatcher emulator_binds;
    
    public RatGBGame() {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        
        IsFixedTimeStep = true;
        
        windows = OperatingSystem.IsWindows();
    }

    ~RatGBGame() {
        while (SaveGame.CurrentlySaving) ;
    }
    
    protected override void Initialize() {
        State.Initialize(this, Content, _graphics, Window);
        base.Initialize();
        
        ui_binds = new BindWatcher(bind_list);
        ui_binds.cares_about_UI_focus = false;

        emulator_binds = new BindWatcher(emu_bind_list);
        emulator_binds.cares_about_UI_focus = true;
    }

    protected override void LoadContent() {
        State.Load(Content);

        ConsoleInputRunner.using_list = ConsoleInputRunner.using_list + "using RatGBLib;\nusing RatGB;\nusing static RatGB.RatGBGame;"; 
        
        gameboy = new GameboyEmulator();
        
        Interface.Load();
        
        memory_texture = new Texture2D(GraphicsDevice, 256, 256, false, SurfaceFormat.Color);
        
        output_render_target = new FullResolutionRenderTarget();
        game_render_target = new FullResolutionRenderTarget();
        
        //gb.LoadROM("gbmicrotest/000-write_to_x8000.gb");
        //gb.LoadROM("bully.gb");
        //gb.LoadROM("alley.gb");
        
        //gb.LoadROM("tetris.gb");
        //gb.LoadROM("metroid2.gb");
        //gb.LoadROM("kirby.gb");
        //gb.LoadROM("pkmnred.gb");
        gb.LoadROM("zelda.gb");
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
        
        //gb.LoadROM("mooneye-test-suite/acceptance/bits/unused_hwio-GS.gb");
        
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
        
        update_thread = new Clock.UpdateThread("Update", UpdatethreadMethod);
        State.LoadFinished(update_thread);
        
        update_thread.tick_rate = gvars.get_float("g_tick_rate");
        gvars.add_change_action("g_tick_rate", () => { update_thread.tick_rate = gvars.get_float("g_tick_rate"); });        
        
        Interface.memory_window.internal_draw_action = () => {
            for (int i = 0; i < 0x10000; i++) {
                byte value = gameboy.gameboy.ReadByte((ushort)i);
            
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

            float ar = Interface.memory_window.client_area_aspect_ratio;
            if (Interface.memory_window.client_size.X >= Interface.memory_window.client_size.Y) {
                Draw2D.image(memory_texture, 
                    new Vector2i((Interface.memory_window.client_size.X / 2f) - (Interface.memory_window.client_size.X / ar) / 2f, 0),
                    new Vector2i(Interface.memory_window.client_size.X / ar, Interface.memory_window.client_size.Y));
            } else {
                Draw2D.image(memory_texture, 
                    new Vector2i(0, (Interface.memory_window.client_size.X / 2f) - (Interface.memory_window.client_size.X * ar) / 2f),
                    new Vector2i(Interface.memory_window.client_size.X, Interface.memory_window.client_size.Y * ar));
            }
        };

    }
    public static Clock.UpdateThread update_thread;
    
    void UpdatethreadMethod() {
        gameboy.Update();
    }
    
    private int t_cycles = 0;

    protected override void Update(GameTime gameTime) {
        base.Update(gameTime);
    }
    
    protected override void Draw(GameTime gameTime) {
        gameboy.Render();
        
        State.UpdateGraphics(gameTime);
        ui_binds.Update();
        emulator_binds.Update();
        
        if (ui_binds.just_pressed("toggle_memory_window")) {
            State.UI.toggle_window(Interface.memory_window);
        }
        
        if (emulator_binds.just_pressed("copy_debug_info")) {
            Interface.DebugInfoToClipboard();
        }
        
        if (emulator_binds.just_pressed("reload_rom")) {
            gb.ReloadROM();
        }
        
        if (State.engine_binds.double_tapped("exit")) {
            Exit();
        }
        
        State.Render();
        
        // draw canvas and interface to their respective full resolution render targets
        State.graphics_device.SetRenderTarget(game_render_target.rt2D);
        State.graphics_device.Clear(Color.Transparent);
        
        //draw game here
        Draw2D.fill_rect_dither(Vector2i.Zero, State.resolution, 
            UIColors.MiddleGrey.multiply_color(0.9f),
            UIColors.MiddleGrey,
            16
        );

        var ar = (float)State.resolution.X / (float)State.resolution.Y;
        if (State.resolution.X >= State.resolution.Y) {
            Draw2D.image(gameboy.texture, 
                new Vector2i((State.resolution.X / 2f) - ((State.resolution.X / ar) / 2f), 0),
                new Vector2i(State.resolution.X / ar, State.resolution.Y)
            );
        } else {
            Draw2D.image(gameboy.texture, 
                new Vector2i(0, (State.resolution.Y / 2f) - ((State.resolution.Y * ar) / 2f)),
                new Vector2i(State.resolution.X, State.resolution.Y * ar)
            );
        }

        
        Interface.Render();
        
        // compose layers
        State.graphics_device.SetRenderTarget(output_render_target.rt2D);
        
        Draw2D.image(game_render_target.rt2D, Vector2i.Zero, State.resolution);
        Draw2D.image(Interface.render_target.rt2D, Vector2i.Zero, State.resolution);
        
        // draw output to screen
        State.graphics_device.SetRenderTarget(null);
        Draw2D.image(output_render_target.rt2D, Vector2i.Zero, State.resolution);
        
        //update framerate counter
        Clock.FrameRateUpdate(gameTime.ElapsedGameTime.TotalMilliseconds);
        
        base.Draw(gameTime);
    }
    
    protected override void UnloadContent() {
        State.Destroy();
        base.UnloadContent();
    }
}