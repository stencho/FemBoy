using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RatGBLib;

namespace RatGB;

public enum InputType {
    Keyboard, Gamepad
}

public class DigitalButton {
    public InputType type;
    
    public JoypadButtons mapped_to;
    
    public bool pressed;
    public bool was_pressed;

    public Buttons button;
    
    public Keys key;
    
    public DigitalButton(Buttons button, JoypadButtons mapped_to) {
        type = InputType.Gamepad;
        this.button = button;
        this.mapped_to = mapped_to;
    }
    
    public DigitalButton(Keys key, JoypadButtons mapped_to) {
        type = InputType.Keyboard;
        this.key = key;
        this.mapped_to = mapped_to;
    }
    
    public void Update(Input input, Joypad joypad) {
        was_pressed = pressed;
        
        if (type == InputType.Keyboard) {
            pressed = input.keyboard_state.IsKeyDown(key);
        } else if (type == InputType.Gamepad) {
            pressed = input.gamepad_state.IsButtonDown(button);
        }

        if (pressed && !was_pressed) {
            joypad.Press(mapped_to);
        }
        if (!pressed && was_pressed) {
            joypad.Release(mapped_to);
        }
    }
}

public class Input {
    public KeyboardState keyboard_state;
    public  GamePadState gamepad_state;

    private DigitalButton[] button_map = {
        new(Keys.Up, JoypadButtons.Up),
        new(Keys.Down, JoypadButtons.Down),
        new(Keys.Left, JoypadButtons.Left),
        new(Keys.Right, JoypadButtons.Right),
        
        new(Keys.X, JoypadButtons.A),
        new(Keys.Z, JoypadButtons.B),
        new(Keys.Enter, JoypadButtons.Start),
        new(Keys.OemPipe, JoypadButtons.Select)
    };
    
    public void Update(Joypad joypad) {
        keyboard_state = Keyboard.GetState();
        gamepad_state = GamePad.GetState(PlayerIndex.One);

        foreach (DigitalButton button in button_map) {
            button.Update(this, joypad);
        }
    }
}