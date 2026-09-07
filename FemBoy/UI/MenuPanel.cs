using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Raven.Engine;
using Raven.Engine.Controls;
using Raven.Graphics.Drawing2D;
using Raven.Graphics.InterpolatedTypes;
using Raven.UI;
using Raven.UI.Forms;

namespace FemBoy.UI;

public class UIMenuPanel : UIPanel {
    MenuPanelItem[] menu_items;
    public int item_height = 30;
    public int menu_width = 300;

    public int header_gap = 50;
    public int footer_gap = 0;
    public int item_gap = 10;

    private string font = "04b11";

    public Action<Vector2i>? DrawHeader;

    public static BindWatcher menu_binds;

    public int selected_menu_item = 0;

    protected UIMenuPanel parent_menu;

    public static bool any_menus_visible = false;
    
    protected static BindList
        menu_bind_list = [
            ("menu_up", [Keys.Up, XInputDigital.DPadUp]),
            ("menu_down", [Keys.Down, XInputDigital.DPadDown]),
            ("menu_left", [Keys.Left, XInputDigital.DPadLeft]),
            ("menu_right", [Keys.Right, XInputDigital.DPadRight]),
            ("menu_select", [Keys.Enter, Keys.Space, XInputDigital.A]),
            ("menu_back", [Keys.Back, Keys.Escape, XInputDigital.B]),
            ("menu_extra", [Keys.LeftControl, Keys.RightControl, XInputDigital.X]),
        ];
    
    public UIMenuPanel(UIMenuPanel parent_menu, params MenuPanelItem[] items) : base(Vector2i.One, Vector2i.One) {
        menu_items = items;

        if (menu_binds == null) {
            menu_binds = new BindWatcher(menu_bind_list);
            menu_binds.cares_about_UI_focus = BindWatcher.UIFocusConsideration.NeedsFocus;
            if (parent_menu == null) menu_binds.requires_focus_on_specific_form = this;
        }

        this.parent_menu = parent_menu;
        focus_lerp = new Lerper(1, 1, 1);
        size = new Vector2i(menu_width, header_gap + (items.Length * (item_height + item_gap)) + footer_gap);
        State.UI.add_panel_dialog(this, true, true);
    }

    public override void render_internal() {
        base.render_internal();
        
        if (header_gap > 0) DrawHeader?.Invoke(new Vector2i(menu_width, header_gap));
        if (header_gap > 0) Draw2D.line(new Vector2i(0, header_gap), new Vector2i(menu_width, header_gap), color_window_focus, 1f);
            
        for (var i = 0; i < menu_items.Length; i++) {
            var menu_item = menu_items[i];
                
            var top_left = new Vector2(0, header_gap + (i * (item_height + item_gap)));
            var middle = top_left + (new Vector2(menu_width, item_height + item_gap) / 2f);
            
            var text_size = Draw2D.measure_string_i(font, menu_item.Text);

            if (selected_menu_item == i) {
                Draw2D.fill_rect(
                    top_left,
                    top_left + new Vector2i(menu_width, item_height + item_gap), 
                    color_window_focus);
                Draw2D.text(font, menu_item.Text, middle - (text_size / 2f), color_background); 
            } else {
                Draw2D.text(font, menu_item.Text, middle - (text_size / 2f), color_window_focus);
            }
        }
    }

    public override void show() {
        base.show();
        menu_binds.requires_focus_on_specific_form = this;
    }
    
    public override void update() {
        base.update();

        if (menu_binds.requires_focus_on_specific_form != this) return;
        menu_binds.Update();
        
        if (!visible) return;
        any_menus_visible = true;
            
        if (menu_binds.just_pressed("menu_up")) {
            selected_menu_item--;
            if (selected_menu_item < 0) selected_menu_item = menu_items.Length - 1;
        }
            
        if (menu_binds.just_pressed("menu_down")) {
            selected_menu_item++;
            if (selected_menu_item > menu_items.Length - 1) selected_menu_item = 0;
        }
            
        if (menu_binds.just_pressed("menu_back")) {
            up_menu();
        }
            
        if (menu_binds.just_pressed("menu_select")) {
            menu_items[selected_menu_item].Pressed?.Invoke();
        }
    }
    
    public void up_menu() {
        if (parent_menu == null) {
            any_menus_visible = false;
            selected_menu_item = 0;
            hide();
            
        } else {
            hide();
            parent_menu.show();
        }
    }
    
    public void show_submenu(UIMenuPanel menu) {
        hide();
        menu.show();
    }
}
