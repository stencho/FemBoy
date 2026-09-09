using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework.Input;
using Raven.Engine;
using Raven.Engine.Controls;
using Raven.Graphics.Drawing2D;
using Raven.Graphics.InterpolatedTypes;
using Raven.UI;
using Raven.UI.Forms;

namespace FemBoy.UI;

public interface IMenuPage {
    public string opened_by { get; set; } 
    
    public MenuSystem parent_system { get; set; }
    
    public Vector2i size { get; set; }
    
    public void render();

    public void move_selection_down();
    public void move_selection_up();
    public void move_selection_left();
    public void move_selection_right();

    public void confirm_selection();
    public void cancel_selection();

    public void page_opened();
    public void page_closed();

    public void reset_cursor(); 
}

public class MenuSystem : UIPanel {
    public BindWatcher menu_binds;
    BindList menu_bind_list = [
        ("menu_up", [Keys.Up, XInputDigital.DPadUp, XInputAnalog.LeftStickUp]),
        ("menu_down", [Keys.Down, XInputDigital.DPadDown, XInputAnalog.LeftStickDown]),
        ("menu_left", [Keys.Left, XInputDigital.DPadLeft, XInputAnalog.LeftStickLeft]),
        ("menu_right", [Keys.Right, XInputDigital.DPadRight, XInputAnalog.LeftStickRight]),
        
        ("menu_select", [Keys.Enter, Keys.Space, XInputDigital.A]),
        ("menu_back", [Keys.Back, Keys.Escape, XInputDigital.B]),
        ("menu_extra", [Keys.LeftControl, Keys.RightControl, XInputDigital.X]),
    ];
    
    private Dictionary<string, IMenuPage> menu_pages = new Dictionary<string, IMenuPage>();
    private string current_menu_page_name = "";
    private IMenuPage current_menu_page => menu_pages[current_menu_page_name];

    string base_menu_name = "";
    private int draw_disable = 0;
    
    private Lerper resize_lerp = new Lerper(0f, 1f, 150f);

    private Vector2i old_size;
    private Vector2i new_size;
    private Vector2i size_diff => new_size - old_size;
    
    public MenuSystem(string base_menu_name) : base(Vector2i.One, Vector2i.One) {
        disable_client_area();
        
        menu_binds = new BindWatcher(menu_bind_list);
        menu_binds.cares_about_UI_focus = BindWatcher.UIFocusConsideration.NeedsFocus;
        menu_binds.requires_focus_on_specific_form = this;
        
        this.base_menu_name = base_menu_name;
        
        State.UI.add_panel_dialog(this, true, true);
    }

    public void add_menu_page(string name, IMenuPage page) {
        menu_pages.Add(name, page);
        menu_pages[name].parent_system = this;
    }
    
    public void show_menu() {
        resize_lerp.Reset(1f);
        
        current_menu_page_name = base_menu_name;
        current_menu_page.page_opened();
        size = current_menu_page.size;
        old_size = size;
        new_size = size;
        show();
    }

    public void open_submenu(string name) {
        resize_lerp.Reset(0f);
        
        string old_name = current_menu_page_name;
        old_size = size;
        
        current_menu_page_name = name;
        current_menu_page.opened_by = old_name;
        current_menu_page.page_opened();
        
        new_size = current_menu_page.size;
        
        menu_pages[old_name].page_closed();
    }

    public void go_up_submenu() {
        current_menu_page.page_closed();
        old_size = current_menu_page.size;
        
        current_menu_page_name = current_menu_page.opened_by;
        current_menu_page.page_opened();
        new_size = current_menu_page.size;
        
        resize_lerp.Reset(0f);
    }
    
    public void close_menu() {
        hide();
        current_menu_page.page_closed();
        current_menu_page_name = "";

        foreach (IMenuPage page in menu_pages.Values) {
            page.reset_cursor();
        }
    }

    public override void draw() {
        if (!visible) return;
        
        resize_lerp.Lerp();
        size = old_size + (size_diff * resize_lerp.Value);
        
        start_of_draw_action?.Invoke();
        
        Draw2D.fill_rect(position, position + size, UIColors.Background);
        Draw2D.rect(position, position + size, UIColors.Foreground, 1f);
        
        if (resize_lerp.Value >= 1f) {
            current_menu_page.render();
        }
    }
    
    public override void render_internal() { }

    public override void update() {
        base.update();

        menu_binds.Update();
        
        if (!visible) return;
            
        if (menu_binds.just_pressed("menu_up")) current_menu_page.move_selection_up();
        if (menu_binds.just_pressed("menu_down")) current_menu_page.move_selection_down();
        if (menu_binds.just_pressed("menu_left")) current_menu_page.move_selection_left();
        if (menu_binds.just_pressed("menu_right")) current_menu_page.move_selection_right();
        
        if (menu_binds.just_pressed("menu_select")) current_menu_page.confirm_selection();
        if (menu_binds.just_pressed("menu_back")) current_menu_page.cancel_selection();
        
        if (menu_binds.held_repeat("menu_up") ) current_menu_page.move_selection_up();
        if (menu_binds.held_repeat("menu_down") ) current_menu_page.move_selection_down();
        if (menu_binds.held_repeat("menu_left") ) current_menu_page.move_selection_left();
        if (menu_binds.held_repeat("menu_right") ) current_menu_page.move_selection_right();
    }
}