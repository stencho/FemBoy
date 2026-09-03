using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FemBoy;

public enum JoypadButtons {
    Up, Down, Left, Right,
    A, B, Start, Select
}

public class Joypad {
    public const ushort RegisterAddress = 0xFF00;
    
    private GameBoy gameboy;

    public Joypad(GameBoy gameboy) => this.gameboy = gameboy;
    
    public bool select_dpad = false;
    public bool select_buttons = false;

    public Dictionary<JoypadButtons, bool> button_states = new() {
        {JoypadButtons.Up,     false},
        {JoypadButtons.Down,   false},
        {JoypadButtons.Left,   false},
        {JoypadButtons.Right,  false},
        {JoypadButtons.A,      false},
        {JoypadButtons.B,      false},
        {JoypadButtons.Start,  false},
        {JoypadButtons.Select, false}
    };

    public byte ReadState() {
        byte result = 0xCF;
        
        if (select_dpad) {
            result &= 0xEF;
            
            if (button_states[JoypadButtons.Right])  result &= 0xFE;
            if (button_states[JoypadButtons.Left])   result &= 0xFD;
            if (button_states[JoypadButtons.Up])     result &= 0xFB;
            if (button_states[JoypadButtons.Down])   result &= 0xF7;
        } 
        if (select_buttons) {
            result &= 0xDF;
            
            if (button_states[JoypadButtons.A])      result &= 0xFE;
            if (button_states[JoypadButtons.B])      result &= 0xFD;
            if (button_states[JoypadButtons.Select]) result &= 0xFB;
            if (button_states[JoypadButtons.Start])  result &= 0xF7;
        } 
        
        return result;
    }
    
    public void Press(JoypadButtons button) {
        if (button_states[button]) return;
        button_states[button] = true;
        
        if (gameboy.CPU.Stopped) gameboy.CPU._stopped = false; 
        gameboy.CPU.RequestInterrupt(InterruptMask.Joypad);
    }

    public void Release(JoypadButtons button) => button_states[button] = false;
}