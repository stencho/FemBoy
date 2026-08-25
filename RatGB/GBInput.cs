using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RatGBLib;
using Raven.Engine.Controls;

namespace RatGB;

public class GBInput {
    internal static (string bind, object[] bind_data)[]
        bind_list = [
            ("up", [Keys.Up]),
            ("down", [Keys.Down]),
            ("left", [Keys.Left]),
            ("right", [Keys.Right]),
            ("a", [Keys.X]),
            ("b", [Keys.Z]),
            ("start", [Keys.Enter]),
            ("select", [Keys.OemPipe]),
        ];

    private BindWatcher binds;
    private GameBoy gameboy;
    public GBInput(GameBoy gameboy) {
        this.gameboy = gameboy;
        
        binds = new BindWatcher(bind_list);
        binds.cares_about_UI_focus = true;
    }
    
    public void Update(Joypad joypad) {
        binds.Update();
        
        if (binds.just_pressed("up")) gameboy.joypad.Press(JoypadButtons.Up);
        if (binds.just_released("up")) gameboy.joypad.Release(JoypadButtons.Up);
        
        if (binds.just_pressed("down")) gameboy.joypad.Press(JoypadButtons.Down);
        if (binds.just_released("down")) gameboy.joypad.Release(JoypadButtons.Down);
        
        if (binds.just_pressed("left")) gameboy.joypad.Press(JoypadButtons.Left);
        if (binds.just_released("left")) gameboy.joypad.Release(JoypadButtons.Left);
        
        if (binds.just_pressed("right")) gameboy.joypad.Press(JoypadButtons.Right);
        if (binds.just_released("right")) gameboy.joypad.Release(JoypadButtons.Right);
        
        if (binds.just_pressed("a")) gameboy.joypad.Press(JoypadButtons.A);
        if (binds.just_released("a")) gameboy.joypad.Release(JoypadButtons.A);
        
        if (binds.just_pressed("b")) gameboy.joypad.Press(JoypadButtons.B);
        if (binds.just_released("b")) gameboy.joypad.Release(JoypadButtons.B);
        
        if (binds.just_pressed("start")) gameboy.joypad.Press(JoypadButtons.Start);
        if (binds.just_released("start")) gameboy.joypad.Release(JoypadButtons.Start);
        
        if (binds.just_pressed("select")) gameboy.joypad.Press(JoypadButtons.Select);
        if (binds.just_released("select")) gameboy.joypad.Release(JoypadButtons.Select);
    }
}