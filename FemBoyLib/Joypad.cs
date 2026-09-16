using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FemBoy;

public enum JoypadButtons {
    Up, Down, Left, Right,
    A, B, Start, Select
}

public class Joypad {
    public const ushort RegisterAddress = 0xFF00;

    public byte JOYP = 0x00;
    
    private GameBoy gameboy;
    private MemoryBus MemoryBus => gameboy.memory_bus;

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

    public void HandleBusRW() {
        if (MemoryBus.BusState == RWState.Write) {
            select_dpad = ((MemoryBus.Data & 0x10) == 0);
            select_buttons = ((MemoryBus.Data & 0x20) == 0);
        }

        if (MemoryBus.BusState == RWState.Read) {
            byte result = 0xCF;

            if (select_dpad) {
                result &= 0xEF;

                if (button_states[JoypadButtons.Right]) result &= 0xFE;
                if (button_states[JoypadButtons.Left]) result &= 0xFD;
                if (button_states[JoypadButtons.Up]) result &= 0xFB;
                if (button_states[JoypadButtons.Down]) result &= 0xF7;
            }

            if (select_buttons) {
                result &= 0xDF;

                if (button_states[JoypadButtons.A]) result &= 0xFE;
                if (button_states[JoypadButtons.B]) result &= 0xFD;
                if (button_states[JoypadButtons.Select]) result &= 0xFB;
                if (button_states[JoypadButtons.Start]) result &= 0xF7;
            }
            JOYP = result;
            MemoryBus.Data = JOYP;
        }
    }
        
    public void Press(JoypadButtons button) {
        if (button_states[button]) return;
        button_states[button] = true;
        
        if (gameboy.CPU.Stopped) gameboy.CPU._stopped = false; 
        gameboy.CPU.RequestInterrupt(InterruptMask.Joypad);
    }
    
    public void Release(JoypadButtons button) => button_states[button] = false;
}