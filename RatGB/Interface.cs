using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RatGBLib;
using Raven.Engine;
using Raven.Engine.Controls;
using Raven.Graphics;
using Raven.Graphics.Drawing2D;
using Raven.Graphics.Geometry2D;
using Raven.UI;
using Raven.UI.Forms;
using Raven.UI.Forms.Layout;
using TextCopy;

using static RatGB.RatGBGame;

namespace RatGB;

public static class Interface {
    static MenuStrip menu_strip;
    
    static UIPanel tool_panel;
    
    public static UIWindow memory_window;
    private static Texture2D memory_texture;
    private static Color[] memory_colors = new Color[256 * 256];

    public static FullResolutionRenderTarget render_target;
    
    private static DrawShapesToSurface shape_drawing;
    
    public static Action<DrawShapesToSurface> Draw2DOverCanvas;
    static void draw_over_canvas_layer() => Draw2DOverCanvas.Invoke(shape_drawing);
        
    public static Action<DrawShapesToSurface> Draw2DOnTop;
    static void draw_on_top_layer() => Draw2DOnTop?.Invoke(shape_drawing);
    
    private static Vector2i cursor_shadow_offset = Vector2i.One * 3 + Vector2i.Down;

    static string PrintStack(byte[] bytes) {
        if (bytes == null) return "";
        string output = "[";
        for (int i = 0; i < bytes.Length; i++) {
            output += $"{bytes[i]:X2}";
            if (i < bytes.Length - 1) output += " ";
        }
        output += "]";
        return output;
    }

    static bool StartsWithAny(string str, params string[] sw) {
        foreach (string s in sw) {
            if (str.StartsWith(s)) return true;
        }
        return false;
    }
    
    public static string PrintOPInfo() {
        if (gb.gameboy == null || gb.gameboy.cartridge == null || gb.gameboy.CPU == null) return "";
        if (!gb.gameboy.CPU.track_opcodes) return "";
        
        string output = "[Trace]\n";
        foreach (var op in gb.gameboy.CPU.LastNOpcodes) {
            string operand_one = op.operand_one != null ? $"{op.operand_one:X2}" : "  ";
            string operand_two = op.operand_two != null ? $"{op.operand_two:X2}" : "  ";
            if (op.cycles > op.intended_cycles) Console.WriteLine($"TOO MANY CYCLES {op.name} {op.cycles} > {op.intended_cycles}");
            output += $"{op.cycles,2}:{op.intended_cycles,2}{(op.cycles > op.intended_cycles ? "TOO MANY CYCLES!!!!!" : "")} {op.PC:X4} -> [{op.opcode:X2} {operand_one} {operand_two}] :: {op.name}\n";
            if (op.name.StartsWith("CALL") || op.name.StartsWith("RET") || op.name.StartsWith("PUSH") || op.name.StartsWith("POP")) {
                output += $"SP CHANGE {op.SP_before:X4} -> {op.SP_after:X4} \n";
                
                if (op.stack_after == null) 
                    output += $"STACK CHANGE {PrintStack(op.stack_before)}\n";
                else 
                    output += $"STACK CHANGE {PrintStack(op.stack_before)} -> {PrintStack(op.stack_after)}\n";
            }
        }
        return output;
    }

    public static string PrintRegisters() {
        if (gb.gameboy == null || gb.gameboy.cartridge == null || gb.gameboy.CPU == null) return "";
        string output =
            $"[Registers]\n" +
            $"[PC] {gb.gameboy.CPU.REGISTERS.PC:X4} [SP] {gb.gameboy.CPU.REGISTERS.SP:X4}\n" +
            $"[IE] {gb.gameboy.CPU.REGISTERS.IE:X2}   [IF] {gb.gameboy.CPU.REGISTERS.IF:X2}\n" +
            $"[A] {gb.gameboy.CPU.REGISTERS.A:X2}    [F] {gb.gameboy.CPU.REGISTERS.F:X2}\n" +
            $"[B] {gb.gameboy.CPU.REGISTERS.B:X2}    [C] {gb.gameboy.CPU.REGISTERS.C:X2}\n" +
            $"[D] {gb.gameboy.CPU.REGISTERS.D:X2}    [E] {gb.gameboy.CPU.REGISTERS.E:X2}\n" +
            $"[H] {gb.gameboy.CPU.REGISTERS.H:X2}    [L] {gb.gameboy.CPU.REGISTERS.L:X2}\n" +
            $"{(gb.ExecutionPaused ? "[PAUSED]\n" : "\n")}";
        return output;
    }

    public static string PrintCartridgeInfo() {
        if (gb.gameboy == null || gb.gameboy.cartridge == null) return "";
        string output = "[Cartridge]\n";
        output += $"[CRC32]    {gb.gameboy.cartridge.ROMCRC32:X8}\n";
        output += $"[Type]     0x{gb.gameboy.cartridge.cartridge_type:X2}\n";
        output += $"[ROM code] 0x{gb.gameboy.cartridge.ROM_size_code:X2}\n";
        output += $"[RAM code] 0x{gb.gameboy.cartridge.RAM_size_code:X2}\n";
        
        output += $"[Mapper]   {gb.gameboy.cartridge.Mapper.Name}\n";
        
        if (gb.gameboy.cartridge.HasRAM) 
        output += $"[RAM]      yes ({gb.gameboy.cartridge.GetRAMSize() / 1024}k)\n";
        else 
        output += $"[RAM]      no\n";

        output += $"[Battery]  {(gb.gameboy.cartridge.HasBattery ? "yes" : "no")}\n";
        output += $"[RTC]      {(gb.gameboy.cartridge.HasRTC ? "yes" : "no")}\n";
        
        return output;
    }
    
    public static void DebugInfoToClipboard() {
        string copy = PrintRegisters() + "\n" + PrintCartridgeInfo() + "\n" +  PrintOPInfo() + "\n";
        ClipboardService.SetText(copy);
    }
    
    public static void Load() {
        UIGraphics.Load();

        render_target = new FullResolutionRenderTarget();
        
        memory_texture = new Texture2D(State.graphics_device, 256, 256, false, SurfaceFormat.Color);
        
        shape_drawing = new DrawShapesToSurface(() => State.resolution);
        
        State.resolution_changed += () => {
            State.UI.change_render_target(render_target.rt2D);
        };
        
        //debug text
        Draw2DOverCanvas += (DrawShapesToSurface draw_shapes) => {
            Draw2D.text_shadow(
                Clock.frame_rate + "/" + Clock.tick_rate + " [Frames/Ticks] Per Sec\n\n" + 
                PrintRegisters() + "\n" + 
                PrintCartridgeInfo() + "\n" + 
                PrintOPInfo() + "\n" + 
                $"\n{(gb.Crashed ? " !CRASHED!" : "")} \n", 
                
                (Vector2i.One * 5), Color.White);
        };

        // draw mouse cursor
        Draw2DOnTop += (DrawShapesToSurface draw_shapes) => {
            UIGraphics.cursor.render_position = MouseWatcher.Position - Vector2.UnitY;
            draw_shapes.draw_shape_single_color(UIGraphics.cursor, cursor_shadow_offset, UIColors.Shadow, Color.Transparent, 0, sdf_pattern.DITHER, 1);
            draw_shapes.draw_shape(UIGraphics.cursor);
        };
        
        State.UI = new UIWindowManager(render_target.rt2D);
        
        //menu_strip = new MenuStrip();

        //menu_strip.menu_buttons.Add(new ButtonFlat("File"));                
        //menu_strip.menu_buttons.Add(new ButtonFlat("Edit"));       
            
        memory_window = new UIWindow(new Vector2i(50, 50), new Vector2i(512,512));
        memory_window.change_name("MEMORY");
        memory_window.change_text("MEMORY");
        
        memory_window.allow_resize = true;
        
        //State.UI.add_window(menu_strip);
        State.UI.add_window(memory_window);
        
        memory_window.hide();
    }

    public static void Render() {
        State.graphics_device.SetRenderTarget(render_target.rt2D);
        State.graphics_device.Clear(Color.Transparent);
        
        draw_over_canvas_layer();
        UIWindowManager.Manager.render_UIs_to_their_buffers();
        draw_on_top_layer();
    }
}