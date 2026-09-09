using System;
using Microsoft.Xna.Framework;
using Raven.Engine;
using Raven.Graphics.Drawing2D;
using Raven.UI;

namespace FemBoy.UI;

public class VerticalMenuItem {
    private string text = "";
    public string Text => text;

    public Action? Pressed;

    public virtual int item_height { get; set; } = 30;

    public virtual void pressed_left() {}
    public virtual void pressed_right() {}
    
    public VerticalMenuItem(string text, Action on_pressed) {
        this.text = text;
        Pressed = on_pressed;
    }

    public virtual void draw(VerticalMenu parent, bool selected, Vector2i top_left, Vector2i bottom_right) {
        var middle = top_left + (new Vector2(parent.menu_width, item_height + parent.item_gap) / 2f);
            
        if (selected) {
            Draw2D.fill_rect(top_left, top_left + new Vector2i(parent.menu_width, item_height + parent.item_gap), UIColors.Foreground);
            Draw2D.text_centered(parent.font, Text, middle, UIColors.Background); 
        } else {
            Draw2D.text_centered(parent.font, Text, middle, UIColors.Foreground);
        }
    }
}

public class VerticalMenuSlider : VerticalMenuItem {
    public override int item_height { get; set; } = 60;

    public int Value => (int)(Minimum + ((Maximum - Minimum) * value_f));
    private float value_f = 0f;
    public int Minimum { get; set; } = 0; 
    public int Maximum { get; set; } = 100;

    public int Step { get; set; } = 1;

    public int Range => Maximum - Minimum;

    public Action<float>? ValueChanged;
    
    public VerticalMenuSlider(string text, int min, int max, int value, int step, Action<float> value_changed = null) : base(text, null) {
        Minimum = min;
        Maximum = max;
        Step = step;
        
        if (value < min) value_f = 0f;
        else if (value > max) value_f = 1f;
        else {
            float position = (value - Minimum) / (float)Range;
            value_f = position;
        }

        ValueChanged = value_changed;
    }

    public override void draw(VerticalMenu parent, bool selected, Vector2i top_left, Vector2i bottom_right) {
        var height_half = item_height / 2f;
        var middle = top_left + (new Vector2(parent.menu_width, height_half + parent.item_gap) / 2f);
        var middle_of_bottom = top_left + (new Vector2(parent.menu_width / 2f, height_half + (height_half / 2f)));
            
        Color fg = !selected ? UIColors.Foreground : UIColors.Background;
        Color bg = selected ? UIColors.Foreground : UIColors.Background;
        
        if (selected) Draw2D.fill_rect(top_left, top_left + new Vector2i(parent.menu_width, item_height + parent.item_gap), UIColors.Foreground);
        Draw2D.text_centered(parent.font, Text, middle, fg); 
        
        Draw2D.line_rounded_ends(
            middle_of_bottom - (Vector2i.UnitX * (parent.menu_width / 3)),  
            middle_of_bottom + (Vector2i.UnitX * (parent.menu_width / 3)), 
            fg, 16f 
            );

        float max_dot_pos = (parent.menu_width / 3) * 2f;
        
        Draw2D.fill_circle(middle_of_bottom - (Vector2i.UnitX * (parent.menu_width / 3)) + (Vector2i.UnitX * (max_dot_pos * value_f)), 5f, bg);
    }

    public override void pressed_left() {
        float s = Step / (float)Range;
        value_f -= s;
        if (value_f < 0f) value_f = 0f;
        
        ValueChanged?.Invoke(value_f);
    }

    public override void pressed_right() {
        float s = Step / (float)Range;
        value_f += s;
        if (value_f > 1f) value_f = 1f;
        ValueChanged?.Invoke(value_f);
    }
}

public class VerticalMenu : IMenuPage {
    public string font = "04b11";
    
    VerticalMenuItem[] menu_items;
    
    public int menu_width = 300;
    public int item_gap = 4;
    public int selected_menu_item = 0;

    public string opened_by { get; set; } = "";
    public Vector2i size { get; set; }

    public MenuSystem parent_system { get; set; }
    
    int menu_height = 1;
    
    public VerticalMenu(params VerticalMenuItem[] menu_items) {
        this.menu_items = menu_items;
        
        foreach (var menu_item in menu_items) {
            menu_height += menu_item.item_height + item_gap;
        }
    }
    
    public void render() {
        int current_top = 0;
        for (var i = 0; i < menu_items.Length; i++) {
            var menu_item = menu_items[i];
                
            var top_left = parent_system.position + new Vector2(0,  current_top);
            
            menu_item.draw(this, i == selected_menu_item, top_left, top_left + new Vector2(menu_width, menu_item.item_height + item_gap));
            
            current_top += menu_item.item_height + item_gap;
        }
    }

    public void move_selection_up() {
        selected_menu_item--;
        if (selected_menu_item < 0) selected_menu_item = menu_items.Length - 1;
    }

    public void move_selection_down() {
        selected_menu_item++;
        if (selected_menu_item > menu_items.Length - 1) selected_menu_item = 0;
    }

    public void move_selection_left() {
        menu_items[selected_menu_item].pressed_left();
    }

    public void move_selection_right() {
        menu_items[selected_menu_item].pressed_right();
    }

    public void confirm_selection() {
        menu_items[selected_menu_item].Pressed?.Invoke();
    }

    public void cancel_selection() {
        if (opened_by == "") parent_system.close_menu(); 
        else parent_system.go_up_submenu();
    }

    public void page_opened() {
        size = new Vector2i(menu_width, menu_height);
    }

    public void page_closed() { }

    public void reset_cursor() {
        selected_menu_item = 0;
    }
}
