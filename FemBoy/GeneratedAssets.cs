using Raven;
using Raven.Engine;
using Raven.Graphics.Geometry2D;
using Raven.UI;

namespace FemBoy;

public static class UIGraphics {
    public static SDFShape cursor;
    
    //public static SDFShape cursor_work;
    //public static SDFShape cursor_disable;
    
    public static void Load() {
        Vector2i pointer_tip = (Vector2i.One * 5) + (Vector2i.Right * 5);
        
        cursor = new SDFShape(
            pointer_tip,
            pointer_tip + (Vector2i.One * 15),
            pointer_tip + (Vector2i.Down * 15) + (Vector2i.Right * 6), 
            pointer_tip + (Vector2i.Down * 21) 
        );
            
        cursor.render_anchor = render_anchor.first_point;
        cursor.inner_color = UIColors.Foreground;
        cursor.inner_border_color =  UIColors.Foreground.multiply_color(.25f);
        cursor.inner_border_width = 1;    
    }
}